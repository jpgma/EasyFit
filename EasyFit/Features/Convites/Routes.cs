using EasyFit.Features.Convites.Contracts;
using EasyFit.Features.Grupos.Contracts;
using EasyFit.Infrastructure.Authentication;
using EasyFit.Infrastructure.Errors;
using EasyFit.Infrastructure.Json;
using Npgsql;

namespace EasyFit.Features.Convites;

internal static class ConvitesRoutes
{
    private const string LoggerCategory = "EasyFit.Convites";

    public static void MapConvitesRoutes(this IEndpointRouteBuilder routes)
    {
        var grupo = routes.MapGroup("/api/grupos/{id:guid}/convites").RequireSessao();
        grupo.MapPost("/", CreateAsync);
        grupo.MapGet("/", ListAsync);
        grupo.MapDelete("/{conviteId:guid}", RevogarAsync);

        routes.MapPost("/api/grupos/entrar", EntrarAsync).RequireSessao();

        // Público: quem foi convidado ainda pode não ter conta. O token de 256 bits não pode ser adivinhado.
        routes.MapPost("/api/convites/consultar", ConsultarAsync);
    }

    private static async Task<IResult> CreateAsync(
        Guid id,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var result = await ConvitesCommands.CreateAsync(
            dataSource,
            id,
            context.GetSessaoAtual().UsuarioId,
            cancellationToken);

        return result.Status switch
        {
            ConviteCriacaoStatus.Criado => Results.Json(
                result.Convite!,
                AppJsonContext.Default.ConviteCriadoResponse,
                statusCode: StatusCodes.Status201Created),
            ConviteCriacaoStatus.GrupoNaoEncontrado => GrupoAcesso.GrupoNaoEncontrado(),
            ConviteCriacaoStatus.AcessoNegado => GrupoAcesso.AcessoNegado(),
            ConviteCriacaoStatus.GrupoCheio => GrupoAcesso.GrupoCheio(),
            _ => throw new ArgumentOutOfRangeException(nameof(result)),
        };
    }

    private static async Task<IResult> ListAsync(
        Guid id,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var denied = await CheckGerenciarAsync(context, dataSource, id, cancellationToken);
        if (denied is not null)
        {
            return denied;
        }

        var convites = await ConvitesQueries.ListPendentesAsync(dataSource, id, cancellationToken);
        return Results.Ok(new ConvitesResponse(convites));
    }

    private static async Task<IResult> RevogarAsync(
        Guid id,
        Guid conviteId,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var denied = await CheckGerenciarAsync(context, dataSource, id, cancellationToken);
        if (denied is not null)
        {
            return denied;
        }

        return await ConvitesCommands.RevogarAsync(dataSource, id, conviteId, cancellationToken)
            ? Results.NoContent()
            : ApiProblem.Create(
                StatusCodes.Status404NotFound,
                "CONVITE_NAO_ENCONTRADO",
                "Este convite não está mais pendente.");
    }

    private static async Task<IResult> EntrarAsync(
        ConviteTokenRequest request,
        HttpContext context,
        NpgsqlDataSource dataSource,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var usuarioId = context.GetSessaoAtual().UsuarioId;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var result = await ConviteAceite.AceitarAsync(
            connection,
            transaction,
            request.Token,
            usuarioId,
            cancellationToken);

        switch (result.Status)
        {
            case ConviteAceiteStatus.Aceito:
                await transaction.CommitAsync(cancellationToken);
                loggerFactory.CreateLogger(LoggerCategory).LogInformation(
                    "Usuário {UsuarioId} entrou no grupo {GrupoId} por convite.", usuarioId, result.GrupoId);
                return Results.Ok(new EntrarGrupoResponse(result.GrupoId));
            case ConviteAceiteStatus.JaMembro:
                return ApiProblem.Create(
                    StatusCodes.Status409Conflict,
                    "JA_E_MEMBRO",
                    "Você já faz parte deste grupo.");
            case ConviteAceiteStatus.GrupoCheio:
                return GrupoAcesso.GrupoCheio();
            default:
                return ConviteAceite.ConviteInvalido();
        }
    }

    private static async Task<IResult> ConsultarAsync(
        ConviteTokenRequest request,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var convite = await ConvitesQueries.ConsultarAsync(dataSource, request.Token, cancellationToken);
        return convite is null ? ConviteAceite.ConviteInvalido() : Results.Ok(convite);
    }

    private static async Task<IResult?> CheckGerenciarAsync(
        HttpContext context,
        NpgsqlDataSource dataSource,
        Guid grupoId,
        CancellationToken cancellationToken)
    {
        var papel = await GrupoAcesso.GetPapelAsync(
            dataSource,
            grupoId,
            context.GetSessaoAtual().UsuarioId,
            cancellationToken);

        if (papel is null)
        {
            return GrupoAcesso.GrupoNaoEncontrado();
        }

        return ConvitesRules.CanGerenciarConvites(papel) ? null : GrupoAcesso.AcessoNegado();
    }
}
