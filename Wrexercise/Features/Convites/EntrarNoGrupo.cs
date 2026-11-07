using Npgsql;
using Wrexercise.Features.Convites.Contracts;
using Wrexercise.Features.Grupos.Contracts;
using Wrexercise.Infrastructure.Authentication;
using Wrexercise.Infrastructure.Errors;

namespace Wrexercise.Features.Convites;

internal static class EntrarNoGrupo
{
    private const string LoggerCategory = "Wrexercise.Convites";

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
