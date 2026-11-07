using Wrexercise.Infrastructure.Authentication;

namespace Wrexercise.Features.Grupos;

internal static class GruposRoutes
{
    public static void MapGruposRoutes(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/grupos").RequireSessao();

        group.MapGet("/", ListarGrupos.HandleAsync);
        group.MapPost("/", CriarGrupo.HandleAsync);
        group.MapGet("/{id:guid}", ObterGrupo.HandleAsync);
        group.MapGet("/{id:guid}/membros", ListarMembros.HandleAsync);
        group.MapPut("/{id:guid}/membros/{usuarioId:guid}/papel", AlterarPapel.HandleAsync);
        group.MapDelete("/{id:guid}/membros/{usuarioId:guid}", RemoverMembro.HandleAsync);
        group.MapPost("/{id:guid}/sair", SairDoGrupo.HandleAsync);
        group.MapPost("/{id:guid}/transferir", TransferirGrupo.HandleAsync);
    }
}
