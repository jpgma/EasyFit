using EasyFit.Infrastructure.Authentication;
using EasyFit.Infrastructure.Json;
using Npgsql;

namespace EasyFit.Features.Atividades;

internal static class ListarAtividades
{
    public static async Task<IResult> HandleAsync(
        Guid grupoId,
        int? limite,
        string? cursor,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        if (cursor is not null && !PaginacaoAtividade.TryDecodeCursor(cursor, out _, out _))
        {
            return OperacaoAtividade.InvalidData();
        }

        var usuarioId = context.GetSessaoAtual().UsuarioId;
        var pagina = await ExecutarAsync(
            dataSource,
            grupoId,
            usuarioId,
            ValidacaoAtividade.NormalizeLimite(limite),
            cursor,
            cancellationToken);

        return pagina is null
            ? OperacaoAtividade.ToResult(AtividadeOperacaoStatus.GrupoNaoEncontrado)
            : Results.Json(pagina, AppJsonContext.Default.AtividadesPaginadasResponse);
    }

    private static async Task<AtividadesPaginadasResponse?> ExecutarAsync(
        NpgsqlDataSource dataSource,
        Guid grupoId,
        Guid usuarioId,
        int limite,
        string? cursor,
        CancellationToken cancellationToken)
    {
        var temCursor = PaginacaoAtividade.TryDecodeCursor(cursor, out var cursorStarted, out var cursorId);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        await using var membro = new NpgsqlCommand("""
            SELECT 1 FROM membros_grupo
            WHERE grupo_id = $1 AND usuario_id = $2 AND removed_at IS NULL
            """, connection);
        membro.Parameters.AddWithValue(grupoId);
        membro.Parameters.AddWithValue(usuarioId);
        if (await membro.ExecuteScalarAsync(cancellationToken) is null)
        {
            return null;
        }

        var sql = ConsultasAtividade.SelectResumo + """

            WHERE a.grupo_id = $2
            """ + (temCursor ? """

            AND (a.started_at, a.id) < ($3, $4)
            """ : string.Empty) + (temCursor
            ? """

            ORDER BY a.started_at DESC, a.id DESC
            LIMIT $5
            """
            : """

            ORDER BY a.started_at DESC, a.id DESC
            LIMIT $3
            """);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue(usuarioId);
        command.Parameters.AddWithValue(grupoId);
        if (temCursor)
        {
            command.Parameters.AddWithValue(cursorStarted);
            command.Parameters.AddWithValue(cursorId);
        }

        command.Parameters.AddWithValue(limite + 1);

        return await ReadPaginaAsync(command, limite, cancellationToken);
    }

    internal static async Task<AtividadesPaginadasResponse> ReadPaginaAsync(
        NpgsqlCommand command,
        int limite,
        CancellationToken cancellationToken)
    {
        var itens = new List<AtividadeResumoResponse>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                itens.Add(ConsultasAtividade.ReadResumo(reader));
            }
        }

        string? proximo = null;
        if (itens.Count > limite)
        {
            var ultimo = itens[limite];
            proximo = PaginacaoAtividade.EncodeCursor(ultimo.StartedAt, ultimo.Id);
            itens.RemoveAt(limite);
        }

        return new AtividadesPaginadasResponse(itens, proximo);
    }
}
