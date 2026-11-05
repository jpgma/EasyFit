using EasyFit.Infrastructure.Authentication;
using EasyFit.Infrastructure.Errors;
using Npgsql;

namespace EasyFit.Features.Convites;

internal static class RevogarConvite
{
    public static async Task<IResult> HandleAsync(
        Guid id,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var denied = await ValidacaoConvite.CheckGerenciarAsync(context, dataSource, id, cancellationToken);
        if (denied is not null)
        {
            return denied;
        }

        return await ExecutarAsync(dataSource, id, cancellationToken)
            ? Results.NoContent()
            : ApiProblem.Create(
                StatusCodes.Status404NotFound,
                "CONVITE_NAO_ENCONTRADO",
                "Este grupo não tem um link de convite válido.");
    }

    /// <returns><c>false</c> se o grupo não tiver um link válido.</returns>
    private static async Task<bool> ExecutarAsync(
        NpgsqlDataSource dataSource,
        Guid grupoId,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("""
            UPDATE convites_grupo
            SET revoked_at = now()
            WHERE grupo_id = $1 AND revoked_at IS NULL AND expires_at > now()
            """);
        command.Parameters.AddWithValue(grupoId);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }
}
