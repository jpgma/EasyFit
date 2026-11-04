using Microsoft.AspNetCore.StaticFiles;

namespace EasyFit.Infrastructure.StaticFiles;

/// <summary>
/// Evita que o navegador mantenha um index.html antigo apontando para chunks que já não existem após novo build.
/// </summary>
internal static class CacheControleEstatico
{
    public static void Aplicar(StaticFileResponseContext contexto)
    {
        var nome = contexto.File.Name;
        if (nome.Equals("index.html", StringComparison.OrdinalIgnoreCase))
        {
            contexto.Context.Response.Headers.CacheControl = "no-cache, must-revalidate";
            return;
        }

        if (nome.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            contexto.Context.Response.Headers.CacheControl = "no-cache, must-revalidate";
            return;
        }

        if (contexto.Context.Request.Path.StartsWithSegments("/assets"))
        {
            contexto.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        }
    }
}
