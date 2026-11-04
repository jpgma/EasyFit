using EasyFit.Infrastructure.Authentication;
using Npgsql;

namespace EasyFit.Features.Convites;

internal static class ListarConvites
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

        var convites = await ListPendentesAsync(dataSource, id, cancellationToken);
        return Results.Ok(new ConvitesResponse(convites));
    }

    private static async Task<IReadOnlyList<ConviteResponse>> ListPendentesAsync(
        NpgsqlDataSource dataSource,
        Guid grupoId,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("""
            SELECT c.id, u.nome_exibicao, c.created_at, c.expires_at
            FROM convites_grupo c
            JOIN usuarios u ON u.id = c.criador_id
            WHERE c.grupo_id = $1 AND c.used_at IS NULL AND c.revoked_at IS NULL AND c.expires_at > now()
            ORDER BY c.created_at DESC, c.id
            """);
        command.Parameters.AddWithValue(grupoId);

        var convites = new List<ConviteResponse>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            convites.Add(new ConviteResponse(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetFieldValue<DateTimeOffset>(2),
                reader.GetFieldValue<DateTimeOffset>(3)));
        }

        return convites;
    }
}
