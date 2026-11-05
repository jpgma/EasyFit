using EasyFit.Infrastructure.Authentication;
using Npgsql;

namespace EasyFit.Features.Convites;

internal static class ObterConvite
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

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return Results.Ok(new ConviteAtualResponse(await GetAtivoAsync(connection, null, id, cancellationToken)));
    }

    public static async Task<ConviteResponse?> GetAtivoAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        Guid grupoId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            SELECT c.token, u.nome_exibicao, c.created_at, c.expires_at
            FROM convites_grupo c
            JOIN usuarios u ON u.id = c.criador_id
            WHERE c.grupo_id = $1 AND c.revoked_at IS NULL AND c.expires_at > now()
            """, connection, transaction);
        command.Parameters.AddWithValue(grupoId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new ConviteResponse(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetFieldValue<DateTimeOffset>(2),
                reader.GetFieldValue<DateTimeOffset>(3))
            : null;
    }
}
