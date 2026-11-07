using Wrexercise.Infrastructure.Authentication;

namespace Wrexercise.Features.Usuarios;

internal static class UsuariosRoutes
{
    public static void MapUsuariosRoutes(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/usuarios/eu/perfil", ObterMeuPerfil.HandleAsync).RequireSessao();
        routes.MapGet("/api/grupos/{id:guid}/membros/{usuarioId:guid}/perfil", ObterPerfilMembro.HandleAsync).RequireSessao();
    }
}
