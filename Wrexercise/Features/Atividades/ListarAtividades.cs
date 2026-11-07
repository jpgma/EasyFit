using Npgsql;
using NpgsqlTypes;
using Wrexercise.Infrastructure.Authentication;
using Wrexercise.Infrastructure.Json;

namespace Wrexercise.Features.Atividades;

internal static class ListarAtividades
{
    public static async Task<IResult> HandleAsync(
        Guid grupoId,
        int? limite,
        string? cursor,
        string? filtro,
        Guid? autorId,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        if ((cursor is not null && !PaginacaoAtividade.TryDecodeCursor(cursor, out _, out _))
            || !ValidacaoAtividade.TryGetFiltroSql(filtro, out var filtroSql))
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
            filtroSql,
            autorId,
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
        string filtroSql,
        Guid? autorId,
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

        var sql = $"""
            {ConsultasAtividade.SelectResumo}
            WHERE a.grupo_id = $2
            {filtroSql}
            AND ($4::uuid IS NULL OR a.usuario_id = $4)
            {(temCursor ? "AND (a.started_at, a.id) < ($5, $6)" : string.Empty)}
            ORDER BY a.started_at DESC, a.id DESC
            LIMIT $3
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue(usuarioId);
        command.Parameters.AddWithValue(grupoId);
        command.Parameters.AddWithValue(limite + 1);
        command.Parameters.AddWithValue(NpgsqlDbType.Uuid, (object?)autorId ?? DBNull.Value);
        if (temCursor)
        {
            command.Parameters.AddWithValue(cursorStarted);
            command.Parameters.AddWithValue(cursorId);
        }

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
            // O cursor é o último item entregue: a próxima página começa logo depois dele.
            var ultimo = itens[limite - 1];
            proximo = PaginacaoAtividade.EncodeCursor(ultimo.StartedAt, ultimo.Id);
            itens.RemoveAt(limite);
        }

        return new AtividadesPaginadasResponse(itens, proximo);
    }
}
