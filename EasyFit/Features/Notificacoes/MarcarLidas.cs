using EasyFit.Infrastructure.Authentication;
using EasyFit.Infrastructure.Errors;
using Npgsql;

namespace EasyFit.Features.Notificacoes;

internal static class MarcarLidas
{
    public static async Task<IResult> MarcarUmaAsync(
        Guid id,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("""
            UPDATE notificacoes
            SET read_at = COALESCE(read_at, now())
            WHERE id = $1 AND usuario_id = $2
            """);
        command.Parameters.AddWithValue(id);
        command.Parameters.AddWithValue(context.GetSessaoAtual().UsuarioId);

        return await command.ExecuteNonQueryAsync(cancellationToken) == 1
            ? Results.NoContent()
            : ApiProblem.Create(
                StatusCodes.Status404NotFound,
                "NOTIFICACAO_NAO_ENCONTRADA",
                "Notificação não encontrada.");
    }

    public static async Task<IResult> MarcarTodasAsync(
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            "UPDATE notificacoes SET read_at = now() WHERE usuario_id = $1 AND read_at IS NULL");
        command.Parameters.AddWithValue(context.GetSessaoAtual().UsuarioId);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return Results.NoContent();
    }
}
