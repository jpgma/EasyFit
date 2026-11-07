using Npgsql;
using Wrexercise.Infrastructure.Authentication;
using Wrexercise.Infrastructure.Json;

namespace Wrexercise.Features.Atividades;

internal static class ListarRecentes
{
    public static async Task<IResult> HandleAsync(
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
        var temCursor = PaginacaoAtividade.TryDecodeCursor(cursor, out var cursorStarted, out var cursorId);
        var limiteNormalizado = ValidacaoAtividade.NormalizeLimite(limite);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var sql = ConsultasAtividade.SelectResumo + (temCursor ? """

            AND (a.started_at, a.id) < ($2, $3)
            """ : string.Empty) + (temCursor
            ? """

            ORDER BY a.started_at DESC, a.id DESC
            LIMIT $4
            """
            : """

            ORDER BY a.started_at DESC, a.id DESC
            LIMIT $2
            """);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue(usuarioId);
        if (temCursor)
        {
            command.Parameters.AddWithValue(cursorStarted);
            command.Parameters.AddWithValue(cursorId);
        }

        command.Parameters.AddWithValue(limiteNormalizado + 1);

        var pagina = await ListarAtividades.ReadPaginaAsync(command, limiteNormalizado, cancellationToken);
        return Results.Json(pagina, AppJsonContext.Default.AtividadesPaginadasResponse);
    }
}
