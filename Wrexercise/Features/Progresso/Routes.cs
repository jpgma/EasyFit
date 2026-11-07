using Wrexercise.Infrastructure.Authentication;

namespace Wrexercise.Features.Progresso;

internal static class ProgressoRoutes
{
    public static void MapProgressoRoutes(this IEndpointRouteBuilder routes)
    {
        var desafio = routes.MapGroup("/api/desafios/{id:guid}").RequireSessao();
        desafio.MapGet("/classificacao", ObterClassificacao.HandleAsync);
        desafio.MapGet("/historico", ObterHistoricoSemanal.HandleAsync);

        routes.MapGroup("/api/grupos")
            .RequireSessao()
            .MapGet("/{grupoId:guid}/resumo", ObterResumoGrupo.HandleAsync);
    }
}
