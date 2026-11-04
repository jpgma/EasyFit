using EasyFit.Features.Convites.Contracts;
using EasyFit.Features.Grupos.Contracts;
using EasyFit.Infrastructure.Authentication;
using EasyFit.Infrastructure.Errors;
using Npgsql;

namespace EasyFit.Features.Convites;

internal static class EntrarNoGrupo
{
    private const string LoggerCategory = "EasyFit.Convites";

    public static async Task<IResult> HandleAsync(
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
}
