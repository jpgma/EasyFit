using Npgsql;
using Wrexercise.Features.Grupos.Contracts;
using Wrexercise.Infrastructure.Authentication;

namespace Wrexercise.Features.Desafios;

internal static class CancelarDesafio
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

    /// <summary>O cancelamento preserva o desafio para o histórico.</summary>
    private static async Task<DesafioOperacaoStatus> ExecutarAsync(
        NpgsqlDataSource dataSource,
        Guid desafioId,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var encontrado = await ConsultasDesafio.GetEstadoAsync(connection, transaction, desafioId, cancellationToken);
        if (encontrado is null)
        {
            return DesafioOperacaoStatus.DesafioNaoEncontrado;
        }

        await GrupoAcesso.LockGrupoAsync(connection, transaction, encontrado.GrupoId, cancellationToken);
        var papel = await GrupoAcesso.GetPapelAsync(connection, transaction, encontrado.GrupoId, usuarioId, cancellationToken);
        if (papel is null)
        {
            return DesafioOperacaoStatus.DesafioNaoEncontrado;
        }

        if (!ValidacaoDesafio.CanGerenciar(papel))
        {
            return DesafioOperacaoStatus.AcessoNegado;
        }

        // Lido de novo depois da trava: outro cancelamento pode ter terminado durante a espera.
        var estado = await ConsultasDesafio.GetEstadoAsync(connection, transaction, desafioId, cancellationToken);
        if (ConsultasDesafio.GetBloqueio(estado!.Situacao) is { } bloqueio)
        {
            return bloqueio;
        }

        await using var update = new NpgsqlCommand(
            "UPDATE desafios SET cancelled_at = now() WHERE id = $1",
            connection,
            transaction);
        update.Parameters.AddWithValue(desafioId);
        await update.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return DesafioOperacaoStatus.Concluida;
    }
}
