using EasyFit.Infrastructure.Authentication;

namespace EasyFit.Features.Desafios;

internal static class DesafiosRoutes
{
    public static void MapDesafiosRoutes(this IEndpointRouteBuilder routes)
    {
        var doGrupo = routes.MapGroup("/api/grupos/{grupoId:guid}/desafios").RequireSessao();
        doGrupo.MapGet("/", ListarDesafios.HandleAsync);
        doGrupo.MapPost("/", CriarDesafio.HandleAsync);

        var desafio = routes.MapGroup("/api/desafios/{id:guid}").RequireSessao();
        desafio.MapGet("/", ObterDesafio.HandleAsync);
        desafio.MapGet("/progresso", ObterProgresso.HandleAsync);
        desafio.MapPut("/", EditarDesafio.HandleAsync);
        desafio.MapPost("/cancelar", CancelarDesafio.HandleAsync);
    }
}
