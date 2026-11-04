using EasyFit.Features.Grupos.Contracts;
using EasyFit.Infrastructure.Authentication;
using EasyFit.Infrastructure.Errors;
using EasyFit.Infrastructure.Json;
using Npgsql;

namespace EasyFit.Features.Grupos;

internal static class GruposRoutes
{
    public static void MapGruposRoutes(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/grupos").RequireSessao();

        group.MapGet("/", ListAsync);
        group.MapPost("/", CreateAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapGet("/{id:guid}/membros", ListMembrosAsync);
        group.MapPut("/{id:guid}/membros/{usuarioId:guid}/papel", AlterarPapelAsync);
        group.MapDelete("/{id:guid}/membros/{usuarioId:guid}", RemoverMembroAsync);
        group.MapPost("/{id:guid}/sair", SairAsync);
        group.MapPost("/{id:guid}/transferir", TransferirAsync);
    }

    private static async Task<IResult> ListAsync(
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var grupos = await GruposQueries.ListByUsuarioAsync(
            dataSource,
            context.GetSessaoAtual().UsuarioId,
            cancellationToken);

        return Results.Ok(new GruposResponse(grupos));
    }

    private static async Task<IResult> CreateAsync(
        CriarGrupoRequest request,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        if (!GruposRules.IsValidNome(request.Nome)
            || !GruposRules.IsPlausibleFusoHorario(request.FusoHorario)
            || !await GruposQueries.IsFusoHorarioValidoAsync(dataSource, request.FusoHorario!, cancellationToken))
        {
            return InvalidData();
        }

        var grupo = await GruposCommands.CreateAsync(
            dataSource,
            GruposRules.NormalizeNome(request.Nome!),
            request.FusoHorario!,
            context.GetSessaoAtual().UsuarioId,
            cancellationToken);

        return Results.Json(grupo, AppJsonContext.Default.GrupoResponse, statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var grupo = await GruposQueries.GetAsync(
            dataSource,
            id,
            context.GetSessaoAtual().UsuarioId,
            cancellationToken);

        return grupo is null ? GrupoAcesso.GrupoNaoEncontrado() : Results.Ok(grupo);
    }

    private static async Task<IResult> ListMembrosAsync(
        Guid id,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var papel = await GrupoAcesso.GetPapelAsync(
            dataSource,
            id,
            context.GetSessaoAtual().UsuarioId,
            cancellationToken);
        if (papel is null)
        {
            return GrupoAcesso.GrupoNaoEncontrado();
        }

        var membros = await GruposQueries.ListMembrosAsync(dataSource, id, cancellationToken);
        return Results.Ok(new MembrosResponse(membros));
    }

    private static async Task<IResult> AlterarPapelAsync(
        Guid id,
        Guid usuarioId,
        AlterarPapelRequest request,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        if (!GruposRules.IsPapelAtribuivel(request.Papel))
        {
            return InvalidData();
        }

        var status = await GruposCommands.AlterarPapelAsync(
            dataSource,
            id,
            context.GetSessaoAtual().UsuarioId,
            usuarioId,
            request.Papel!,
            cancellationToken);

        return ToResult(status);
    }

    private static async Task<IResult> RemoverMembroAsync(
        Guid id,
        Guid usuarioId,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var status = await GruposCommands.RemoverMembroAsync(
            dataSource,
            id,
            context.GetSessaoAtual().UsuarioId,
            usuarioId,
            cancellationToken);

        return ToResult(status);
    }

    private static async Task<IResult> SairAsync(
        Guid id,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var status = await GruposCommands.SairAsync(
            dataSource,
            id,
            context.GetSessaoAtual().UsuarioId,
            cancellationToken);

        return ToResult(status);
    }

    private static async Task<IResult> TransferirAsync(
        Guid id,
        TransferirGrupoRequest request,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        if (request.UsuarioId is not { } alvoId)
        {
            return InvalidData();
        }

        var status = await GruposCommands.TransferirAsync(
            dataSource,
            id,
            context.GetSessaoAtual().UsuarioId,
            alvoId,
            cancellationToken);

        return ToResult(status);
    }

    private static IResult ToResult(GrupoOperacaoStatus status) => status switch
    {
        GrupoOperacaoStatus.Concluida => Results.NoContent(),
        GrupoOperacaoStatus.GrupoNaoEncontrado => GrupoAcesso.GrupoNaoEncontrado(),
        GrupoOperacaoStatus.AcessoNegado => GrupoAcesso.AcessoNegado(),
        GrupoOperacaoStatus.MembroNaoEncontrado => ApiProblem.Create(
            StatusCodes.Status404NotFound,
            "MEMBRO_NAO_ENCONTRADO",
            "Esta pessoa não faz parte do grupo."),
        GrupoOperacaoStatus.ProprietarioNaoPodeSair => ApiProblem.Create(
            StatusCodes.Status409Conflict,
            "PROPRIETARIO_NAO_PODE_SAIR",
            "Transfira a propriedade do grupo para outro membro antes de sair."),
        GrupoOperacaoStatus.DadosInvalidos => InvalidData(),
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    private static IResult InvalidData() => ApiProblem.Create(
        StatusCodes.Status400BadRequest,
        "DADOS_INVALIDOS",
        "Verifique os dados informados.");
}
