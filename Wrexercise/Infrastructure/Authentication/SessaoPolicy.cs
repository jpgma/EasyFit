namespace Wrexercise.Infrastructure.Authentication;

internal static class SessaoPolicy
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    private static readonly TimeSpan RenewalInterval = TimeSpan.FromDays(1);

    /// <summary>A sessão desliza no máximo uma vez por dia, para não gerar uma escrita por requisição.</summary>
    public static bool ShouldRenew(DateTimeOffset expiresAt, DateTimeOffset now) =>
        expiresAt - now < Lifetime - RenewalInterval;
}
