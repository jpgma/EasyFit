using Npgsql;
using Wrexercise.Infrastructure.Errors;

namespace Wrexercise.Infrastructure.Authentication;

internal sealed record SessaoAtual(Guid UsuarioId);

internal static class SessaoFilter
{
    private const string ItemKey = "Wrexercise.SessaoAtual";

    /// <summary>Exige uma sessão válida. O pertencimento ao grupo continua sendo verificado por cada endpoint.</summary>
    public static TBuilder RequireSessao<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter(async (invocation, next) =>
        {
            var context = invocation.HttpContext;
            var sessao = await ResolveAsync(context);
            if (sessao is null)
            {
                return NotAuthenticated();
            }

            context.Items[ItemKey] = sessao;
            return await next(invocation);
        });

    /// <summary>Só pode ser chamado em endpoints protegidos por <see cref="RequireSessao{TBuilder}"/>.</summary>
    public static SessaoAtual GetSessaoAtual(this HttpContext context) =>
        context.Items[ItemKey] as SessaoAtual
        ?? throw new InvalidOperationException("O endpoint não exige sessão.");

    public static IResult NotAuthenticated() => ApiProblem.Create(
        StatusCodes.Status401Unauthorized,
        "NAO_AUTENTICADO",
        "Entre na sua conta para continuar.");

    private static async Task<SessaoAtual?> ResolveAsync(HttpContext context)
    {
        var token = SessaoCookie.Read(context);
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        var dataSource = context.RequestServices.GetRequiredService<NpgsqlDataSource>();
        var cancellationToken = context.RequestAborted;

        Guid id;
        Guid usuarioId;
        DateTimeOffset expiresAt;

        await using (var command = dataSource.CreateCommand("""
            SELECT id, usuario_id, expires_at
            FROM sessoes_autenticacao
            WHERE token_hash = $1 AND revoked_at IS NULL AND expires_at > now()
            """))
        {
            command.Parameters.AddWithValue(OpaqueToken.Hash(token));

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            id = reader.GetGuid(0);
            usuarioId = reader.GetGuid(1);
            expiresAt = reader.GetFieldValue<DateTimeOffset>(2);
        }

        var now = DateTimeOffset.UtcNow;
        if (SessaoPolicy.ShouldRenew(expiresAt, now))
        {
            await using var renew = dataSource.CreateCommand(
                "UPDATE sessoes_autenticacao SET expires_at = $1 WHERE id = $2");
            renew.Parameters.AddWithValue(now + SessaoPolicy.Lifetime);
            renew.Parameters.AddWithValue(id);
            await renew.ExecuteNonQueryAsync(cancellationToken);

            SessaoCookie.Write(context, token);
        }

        return new SessaoAtual(usuarioId);
    }
}
