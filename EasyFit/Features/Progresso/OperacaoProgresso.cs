using EasyFit.Features.Grupos.Contracts;
using EasyFit.Infrastructure.Errors;

namespace EasyFit.Features.Progresso;

internal static class OperacaoProgresso
{
    public static IResult DesafioNaoEncontrado() => ApiProblem.Create(
        StatusCodes.Status404NotFound,
        "DESAFIO_NAO_ENCONTRADO",
        "Desafio não encontrado.");

    public static IResult GrupoNaoEncontrado() => GrupoAcesso.GrupoNaoEncontrado();
}
