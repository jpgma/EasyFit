using EasyFit.Infrastructure.Authentication;

namespace EasyFit.Features.Reacoes;

internal static class ReacoesRoutes
{
    public static void MapReacoesRoutes(this IEndpointRouteBuilder routes)
    {
        var atividade = routes.MapGroup("/api/atividades/{id:guid}").RequireSessao();
        atividade.MapPut("/reacao", RegistrarReacao.HandleAsync);
        atividade.MapGet("/reacoes", ListarReacoes.HandleAsync);
        atividade.MapPost("/revisao", RevisarAtividade.HandleAsync);
        atividade.MapGet("/comentarios", ComentariosRevisao.ListarAsync);
        atividade.MapPost("/comentarios", ComentariosRevisao.CriarAsync);
    }
}
