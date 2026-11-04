using EasyFit.Features.Grupos.Contracts;
using EasyFit.Infrastructure.Authentication;
using EasyFit.Infrastructure.Json;
using Npgsql;
using NpgsqlTypes;

namespace EasyFit.Features.Atividades;

internal static class CriarAtividade
{
    public static async Task<IResult> HandleAsync(
        Guid grupoId,
        CriarAtividadeRequest request,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        if (!IsValidRequest(request))
        {
            return OperacaoAtividade.InvalidData();
        }

        var usuarioId = context.GetSessaoAtual().UsuarioId;
        var (status, atividadeId) = await ExecutarAsync(
            dataSource,
            grupoId,
            usuarioId,
            request,
            cancellationToken);

        if (status != AtividadeOperacaoStatus.Concluida)
        {
            return OperacaoAtividade.ToResult(status);
        }

        var atividade = await ConsultasAtividade.GetDetalheAsync(dataSource, atividadeId, usuarioId, cancellationToken);
        return atividade is null
            ? OperacaoAtividade.AtividadeNaoEncontrada()
            : Results.Json(atividade, AppJsonContext.Default.AtividadeResponse, statusCode: StatusCodes.Status201Created);
    }

    private static bool IsValidRequest(CriarAtividadeRequest request) =>
        TipoAtividade.IsValid(request.Tipo)
        && TipoComprovacao.IsValid(request.TipoComprovacao)
        && ValidacaoAtividade.IsValidDuracao(request.DuracaoSegundos)
        && ValidacaoAtividade.IsValidDescricao(request.Descricao)
        && ValidacaoAtividade.IsValidStartedAt(request.StartedAt);

    private static async Task<(AtividadeOperacaoStatus Status, Guid Id)> ExecutarAsync(
        NpgsqlDataSource dataSource,
        Guid grupoId,
        Guid usuarioId,
        CriarAtividadeRequest request,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        if (await GrupoAcesso.GetPapelAsync(connection, transaction, grupoId, usuarioId, cancellationToken) is null)
        {
            return (AtividadeOperacaoStatus.GrupoNaoEncontrado, default);
        }

        await using var insert = new NpgsqlCommand("""
            INSERT INTO atividades (
                grupo_id, usuario_id, tipo, duracao_segundos, started_at, descricao, tipo_comprovacao)
            VALUES ($1, $2, $3, $4, $5, $6, $7)
            RETURNING id
            """, connection, transaction);
        insert.Parameters.AddWithValue(grupoId);
        insert.Parameters.AddWithValue(usuarioId);
        insert.Parameters.AddWithValue(request.Tipo!);
        insert.Parameters.AddWithValue(request.DuracaoSegundos!.Value);
        insert.Parameters.AddWithValue(request.StartedAt!.Value);
        insert.Parameters.Add(new NpgsqlParameter
        {
            Value = (object?)ValidacaoAtividade.NormalizeDescricao(request.Descricao) ?? DBNull.Value,
        });
        insert.Parameters.AddWithValue(request.TipoComprovacao!);

        var id = (Guid)(await insert.ExecuteScalarAsync(cancellationToken))!;
        await transaction.CommitAsync(cancellationToken);
        return (AtividadeOperacaoStatus.Concluida, id);
    }
}
