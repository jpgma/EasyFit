using EasyFit.Features.Grupos.Contracts;
using EasyFit.Infrastructure.Errors;

namespace EasyFit.Features.Desafios;

internal enum DesafioOperacaoStatus
{
    Concluida,
    GrupoNaoEncontrado,
    DesafioNaoEncontrado,
    AcessoNegado,
    DesafioEncerrado,
    DesafioCancelado,
    DadosInvalidos,
}

internal static class OperacaoDesafio
{
    public static IResult ToResult(DesafioOperacaoStatus status) => status switch
    {
        DesafioOperacaoStatus.Concluida => Results.NoContent(),
        DesafioOperacaoStatus.GrupoNaoEncontrado => GrupoAcesso.GrupoNaoEncontrado(),
        DesafioOperacaoStatus.DesafioNaoEncontrado => DesafioNaoEncontrado(),
        DesafioOperacaoStatus.AcessoNegado => GrupoAcesso.AcessoNegado(),
        DesafioOperacaoStatus.DesafioEncerrado => ApiProblem.Create(
            StatusCodes.Status409Conflict,
            "DESAFIO_ENCERRADO",
            "Este desafio já terminou."),
        DesafioOperacaoStatus.DesafioCancelado => ApiProblem.Create(
            StatusCodes.Status409Conflict,
            "DESAFIO_CANCELADO",
            "Este desafio foi cancelado."),
        DesafioOperacaoStatus.DadosInvalidos => InvalidData(),
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    // A mesma resposta para quem não é membro do grupo e para um desafio inexistente.
    public static IResult DesafioNaoEncontrado() => ApiProblem.Create(
        StatusCodes.Status404NotFound,
        "DESAFIO_NAO_ENCONTRADO",
        "Desafio não encontrado.");

    public static IResult InvalidData() => ApiProblem.Create(
        StatusCodes.Status400BadRequest,
        "DADOS_INVALIDOS",
        "Verifique os dados informados.");
}
