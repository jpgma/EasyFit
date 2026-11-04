using EasyFit.Infrastructure.Files;
using Microsoft.AspNetCore.Http.Features;

namespace EasyFit.Features.Atividades;

/// <summary>Permite corpo de até 5 MB só nas rotas de upload, mantendo 1 MB no restante da API.</summary>
internal sealed class LimiteUploadFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var feature = context.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (feature is not null && !feature.IsReadOnly)
        {
            feature.MaxRequestBodySize = ValidacaoImagemEvidencia.MaxBytes;
        }

        return await next(context);
    }
}
