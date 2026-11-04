using EasyFit.Infrastructure.Authentication;
using EasyFit.Infrastructure.Errors;
using Npgsql;

namespace EasyFit.Features.Convites;

internal static class RevogarConvite
{
    public static async Task<IResult> HandleAsync(
        Guid id,
        Guid conviteId,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var denied = await ValidacaoConvite.CheckGerenciarAsync(context, dataSource, id, cancellationToken);
        if (denied is not null)
        {
            return denied;
        }

        return await ExecutarAsync(dataSource, id, conviteId, cancellationToken)
            ? Results.NoContent()
            : ApiProblem.Create(
                StatusCodes.Status404NotFound,
                "CONVITE_NAO_ENCONTRADO",
                "Este convite não está mais pendente.");
    }

    /// <returns><c>false</c> se o convite não existir neste grupo ou não estiver mais pendente.</returns>
    private static async Task<bool> ExecutarAsync(
        NpgsqlDataSource dataSource,
        Guid grupoId,
        Guid conviteId,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("""
            UPDATE convites_grupo
            SET revoked_at = now()
            WHERE id = $1 AND grupo_id = $2 AND used_at IS NULL AND revoked_at IS NULL AND expires_at > now()
            """);
        command.Parameters.AddWithValue(conviteId);
        command.Parameters.AddWithValue(grupoId);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }
}
