using EasyFit.Features.Notificacoes.Contracts;
using EasyFit.Infrastructure.Authentication;
using EasyFit.Infrastructure.Database;
using EasyFit.Infrastructure.Errors;
using EasyFit.Infrastructure.Json;
using Npgsql;

namespace EasyFit.Features.Reacoes;

internal static class ComentariosRevisao
{
    public static async Task<IResult> ListarAsync(
        Guid id,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var usuarioId = context.GetSessaoAtual().UsuarioId;
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        await using (var acesso = new NpgsqlCommand("""
            SELECT 1
            FROM atividades a
            JOIN membros_grupo m ON m.grupo_id = a.grupo_id AND m.usuario_id = $2 AND m.removed_at IS NULL
            WHERE a.id = $1
            """, connection))
        {
            acesso.Parameters.AddWithValue(id);
            acesso.Parameters.AddWithValue(usuarioId);
            if (await acesso.ExecuteScalarAsync(cancellationToken) is null)
            {
                return OperacaoReacao.ToResult(ReacaoOperacaoStatus.AtividadeNaoEncontrada);
            }
        }

        var comentarios = new List<ComentarioResponse>();
        await using var lista = new NpgsqlCommand("""
            SELECT c.id, c.usuario_id, u.nome_exibicao, c.texto, c.created_at
            FROM comentarios_revisao c
            JOIN usuarios u ON u.id = c.usuario_id
            WHERE c.atividade_id = $1
            ORDER BY c.created_at, c.id
            """, connection);
        lista.Parameters.AddWithValue(id);
        await using var reader = await lista.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            comentarios.Add(new ComentarioResponse(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetString(2),
                reader.GetString(3),
                TimestampLeitura.FromReader(reader, 4)));
        }

        return Results.Json(new ComentariosResponse(comentarios), AppJsonContext.Default.ComentariosResponse);
    }

    public static async Task<IResult> CriarAsync(
        Guid id,
        ComentarioRequest request,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        if (!ValidacaoRevisao.IsValidTexto(request.Texto))
        {
            return OperacaoReacao.InvalidData();
        }

        var usuarioId = context.GetSessaoAtual().UsuarioId;
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // O bloqueio impede gravar um comentário depois de uma decisão simultânea.
        string situacao;
        await using (var load = new NpgsqlCommand("""
            SELECT a.situacao_verificacao
            FROM atividades a
            JOIN membros_grupo m ON m.grupo_id = a.grupo_id AND m.usuario_id = $2 AND m.removed_at IS NULL
            WHERE a.id = $1
            FOR UPDATE OF a
            """, connection, transaction))
        {
            load.Parameters.AddWithValue(id);
            load.Parameters.AddWithValue(usuarioId);
            if (await load.ExecuteScalarAsync(cancellationToken) is not string atual)
            {
                return OperacaoReacao.ToResult(ReacaoOperacaoStatus.AtividadeNaoEncontrada);
            }

            situacao = atual;
        }

        if (situacao != SituacaoVerificacao.EmRevisao)
        {
            return ApiProblem.Create(
                StatusCodes.Status409Conflict,
                "COMENTARIOS_ENCERRADOS",
                "Só é possível comentar enquanto a atividade está em revisão.");
        }

        var texto = ValidacaoRevisao.NormalizeJustificativa(request.Texto!);
        Guid comentarioId;
        DateTimeOffset createdAt;
        await using (var insert = new NpgsqlCommand("""
            INSERT INTO comentarios_revisao (atividade_id, usuario_id, texto)
            VALUES ($1, $2, $3)
            RETURNING id, created_at
            """, connection, transaction))
        {
            insert.Parameters.AddWithValue(id);
            insert.Parameters.AddWithValue(usuarioId);
            insert.Parameters.AddWithValue(texto);
            await using var reader = await insert.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            comentarioId = reader.GetGuid(0);
            createdAt = TimestampLeitura.FromReader(reader, 1);
        }

        await NotificacoesAtividade.NotificarComentarioAsync(connection, transaction, id, usuarioId, cancellationToken);

        string nome;
        await using (var autor = new NpgsqlCommand(
            "SELECT nome_exibicao FROM usuarios WHERE id = $1",
            connection,
            transaction))
        {
            autor.Parameters.AddWithValue(usuarioId);
            nome = (string)(await autor.ExecuteScalarAsync(cancellationToken))!;
        }

        await transaction.CommitAsync(cancellationToken);
        return Results.Json(
            new ComentarioResponse(comentarioId, usuarioId, nome, texto, createdAt),
            AppJsonContext.Default.ComentarioResponse,
            statusCode: StatusCodes.Status201Created);
    }
}
