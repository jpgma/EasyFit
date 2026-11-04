using EasyFit.Infrastructure.Authentication;

namespace EasyFit.Features.Convites;

internal static class ConvitesRoutes
{
    public static void MapConvitesRoutes(this IEndpointRouteBuilder routes)
    {
        var grupo = routes.MapGroup("/api/grupos/{id:guid}/convites").RequireSessao();
        grupo.MapPost("/", CriarConvite.HandleAsync);
        grupo.MapGet("/", ListarConvites.HandleAsync);
        grupo.MapDelete("/{conviteId:guid}", RevogarConvite.HandleAsync);

        routes.MapPost("/api/grupos/entrar", EntrarNoGrupo.HandleAsync).RequireSessao();

        // Público: quem foi convidado ainda pode não ter conta. O token de 256 bits não pode ser adivinhado.
        routes.MapPost("/api/convites/consultar", ConsultarConvite.HandleAsync);
    }
}
