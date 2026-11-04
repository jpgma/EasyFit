using EasyFit.Infrastructure.Errors;

namespace EasyFit.Infrastructure.Authentication;

/// <summary>Proteção contra CSRF: escritas na API só são aceitas quando vêm da mesma origem.</summary>
internal static class OriginCheck
{
    public static IApplicationBuilder UseOriginCheck(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var request = context.Request;
            if (request.Path.StartsWithSegments("/api")
                && IsWrite(request.Method)
                && !IsSameOrigin(request.Headers.Origin, request.Headers["Sec-Fetch-Site"], request.Host.Value))
            {
                await ApiProblem.WriteAsync(
                    context,
                    StatusCodes.Status403Forbidden,
                    "ORIGEM_INVALIDA",
                    "Requisição recusada.");
                return;
            }

            await next(context);
        });

    public static bool IsSameOrigin(string? origin, string? secFetchSite, string? host)
    {
        if (string.IsNullOrEmpty(origin))
        {
            return string.Equals(secFetchSite, "same-origin", StringComparison.OrdinalIgnoreCase);
        }

        return Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            && string.Equals(uri.Authority, host, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWrite(string method) =>
        HttpMethods.IsPost(method) || HttpMethods.IsPut(method)
        || HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method);
}
