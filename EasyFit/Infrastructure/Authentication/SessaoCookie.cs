namespace EasyFit.Infrastructure.Authentication;

internal static class SessaoCookie
{
    private const string Name = "ef_session";

    public static string? Read(HttpContext context) => context.Request.Cookies[Name];

    public static void Write(HttpContext context, string token) =>
        context.Response.Cookies.Append(Name, token, CreateOptions(context));

    public static void Delete(HttpContext context) =>
        context.Response.Cookies.Delete(Name, CreateOptions(context));

    private static CookieOptions CreateOptions(HttpContext context) => new()
    {
        HttpOnly = true,
        // O desenvolvimento roda em http://; o Caddy garante HTTPS em produção.
        Secure = !context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment(),
        SameSite = SameSiteMode.Lax,
        Path = "/",
        MaxAge = SessaoPolicy.Lifetime,
    };
}
