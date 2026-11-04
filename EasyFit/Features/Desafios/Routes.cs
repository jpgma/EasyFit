using EasyFit.Features.Grupos.Contracts;
using EasyFit.Infrastructure.Authentication;
using EasyFit.Infrastructure.Errors;
using EasyFit.Infrastructure.Json;
using Npgsql;

namespace EasyFit.Features.Desafios;

internal static class DesafiosRoutes
{
    public static void MapDesafiosRoutes(this IEndpointRouteBuilder routes)
    {
        var doGrupo = routes.MapGroup("/api/grupos/{grupoId:guid}/desafios").RequireSessao();
        doGrupo.MapGet("/", ListAsync);
        doGrupo.MapPost("/", CreateAsync);

        var desafio = routes.MapGroup("/api/desafios/{id:guid}").RequireSessao();
        desafio.MapGet("/", GetAsync);
        desafio.MapGet("/progresso", GetProgressoAsync);
        desafio.MapPost("/participar", ParticiparAsync);
        desafio.MapPost("/cancelar", CancelarAsync);
    }

    private static async Task<IResult> ListAsync(
        Guid grupoId,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var usuarioId = context.GetSessaoAtual().UsuarioId;
        if (await GrupoAcesso.GetPapelAsync(dataSource, grupoId, usuarioId, cancellationToken) is null)
        {
            return GrupoAcesso.GrupoNaoEncontrado();
        }

        var desafios = await DesafiosQueries.ListByGrupoAsync(dataSource, grupoId, usuarioId, cancellationToken);
        return Results.Ok(new DesafiosResponse(desafios));
    }

    private static async Task<IResult> CreateAsync(
        Guid grupoId,
        CriarDesafioRequest request,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        if (!DesafiosRules.IsValidNome(request.Nome)
            || !DesafiosRules.IsValidDescricao(request.Descricao)
            || !DesafiosRules.IsValidDuracaoMinima(request.DuracaoMinimaSegundos)
            || !DesafiosRules.IsValidSessoesPorSemana(request.SessoesPorSemana)
            || request.DataInicio is not { } dataInicio
            || request.DataFim is not { } dataFim)
        {
            return InvalidData();
        }

        var usuarioId = context.GetSessaoAtual().UsuarioId;
        var (status, desafioId) = await DesafiosCommands.CreateAsync(
            dataSource,
            grupoId,
            usuarioId,
            new NovoDesafio(
                DesafiosRules.NormalizeNome(request.Nome!),
                DesafiosRules.NormalizeDescricao(request.Descricao),
                dataInicio,
                dataFim,
                request.DuracaoMinimaSegundos!.Value,
                request.SessoesPorSemana!.Value),
            cancellationToken);
        if (status != DesafioOperacaoStatus.Concluida)
        {
            return ToResult(status);
        }

        var desafio = await DesafiosQueries.GetAsync(dataSource, desafioId, usuarioId, cancellationToken);
        return desafio is null
            ? DesafioNaoEncontrado()
            : Results.Json(desafio, AppJsonContext.Default.DesafioResponse, statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var desafio = await DesafiosQueries.GetAsync(
            dataSource,
            id,
            context.GetSessaoAtual().UsuarioId,
            cancellationToken);

        return desafio is null ? DesafioNaoEncontrado() : Results.Ok(desafio);
    }

    private static async Task<IResult> GetProgressoAsync(
        Guid id,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var progresso = await DesafiosQueries.GetProgressoAsync(
            dataSource,
            id,
            context.GetSessaoAtual().UsuarioId,
            cancellationToken);

        return progresso is null ? DesafioNaoEncontrado() : Results.Ok(progresso);
    }

    private static async Task<IResult> ParticiparAsync(
        Guid id,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var status = await DesafiosCommands.ParticiparAsync(
            dataSource,
            id,
            context.GetSessaoAtual().UsuarioId,
            cancellationToken);

        return ToResult(status);
    }

    private static async Task<IResult> CancelarAsync(
        Guid id,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var status = await DesafiosCommands.CancelarAsync(
            dataSource,
            id,
            context.GetSessaoAtual().UsuarioId,
            cancellationToken);

        return ToResult(status);
    }

    private static IResult ToResult(DesafioOperacaoStatus status) => status switch
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
    private static IResult DesafioNaoEncontrado() => ApiProblem.Create(
        StatusCodes.Status404NotFound,
        "DESAFIO_NAO_ENCONTRADO",
        "Desafio não encontrado.");

    private static IResult InvalidData() => ApiProblem.Create(
        StatusCodes.Status400BadRequest,
        "DADOS_INVALIDOS",
        "Verifique os dados informados.");
}
