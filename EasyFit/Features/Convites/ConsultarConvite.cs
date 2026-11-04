using EasyFit.Features.Convites.Contracts;
using EasyFit.Features.Grupos.Contracts;
using EasyFit.Infrastructure.Authentication;
using Npgsql;

namespace EasyFit.Features.Convites;

internal static class ConsultarConvite
{
    public static async Task<IResult> HandleAsync(
        ConviteTokenRequest request,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var convite = await ConsultarAsync(dataSource, request.Token, cancellationToken);
        return convite is null ? ConviteAceite.ConviteInvalido() : Results.Ok(convite);
    }

    private static async Task<ConviteConsultaResponse?> ConsultarAsync(
        NpgsqlDataSource dataSource,
        string? token,
        CancellationToken cancellationToken)
    {
        if (!ConviteAceite.IsPlausibleToken(token))
        {
            return null;
        }

        await using var command = dataSource.CreateCommand("""
            SELECT g.nome, u.nome_exibicao,
                (SELECT count(*)::int FROM membros_grupo t WHERE t.grupo_id = g.id AND t.removed_at IS NULL)
            FROM convites_grupo c
            JOIN grupos g ON g.id = c.grupo_id
            JOIN usuarios u ON u.id = c.criador_id
            WHERE c.token_hash = $1 AND c.used_at IS NULL AND c.revoked_at IS NULL AND c.expires_at > now()
            """);
        command.Parameters.AddWithValue(OpaqueToken.Hash(token!));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new ConviteConsultaResponse(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetInt32(2),
                GrupoEntrada.MaxMembros)
            : null;
    }
}
