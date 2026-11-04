using EasyFit.Features.Grupos.Contracts;
using EasyFit.Infrastructure.Authentication;
using Npgsql;

namespace EasyFit.Features.Desafios;

internal static class ParticiparDesafio
{
    public static async Task<IResult> HandleAsync(
        Guid id,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var status = await ExecutarAsync(
            dataSource,
            id,
            context.GetSessaoAtual().UsuarioId,
            cancellationToken);

        return OperacaoDesafio.ToResult(status);
    }

    /// <summary>Participar de novo não muda nada, e a data de entrada continua sendo a primeira.</summary>
    private static async Task<DesafioOperacaoStatus> ExecutarAsync(
        NpgsqlDataSource dataSource,
        Guid desafioId,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var estado = await ParticipantesDesafio.GetEstadoAsync(connection, null, desafioId, cancellationToken);
        if (estado is null
            || await GrupoAcesso.GetPapelAsync(connection, null, estado.GrupoId, usuarioId, cancellationToken) is null)
        {
            return DesafioOperacaoStatus.DesafioNaoEncontrado;
        }

        if (ParticipantesDesafio.GetBloqueio(estado.Situacao) is { } bloqueio)
        {
            return bloqueio;
        }

        await ParticipantesDesafio.InsertAsync(connection, null, desafioId, usuarioId, cancellationToken);
        return DesafioOperacaoStatus.Concluida;
    }
}
