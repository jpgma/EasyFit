using EasyFit.Features.Grupos.Contracts;
using EasyFit.Infrastructure.Authentication;
using Npgsql;

namespace EasyFit.Features.Grupos;

internal sealed record TransferirGrupoRequest(Guid? UsuarioId);

internal static class TransferirGrupo
{
    public static async Task<IResult> HandleAsync(
        Guid id,
        TransferirGrupoRequest request,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        if (request.UsuarioId is not { } alvoId)
        {
            return MembrosGrupo.InvalidData();
        }

        var status = await ExecutarAsync(
            dataSource,
            id,
            context.GetSessaoAtual().UsuarioId,
            alvoId,
            cancellationToken);

        return MembrosGrupo.ToResult(status);
    }

    private static async Task<GrupoOperacaoStatus> ExecutarAsync(
        NpgsqlDataSource dataSource,
        Guid grupoId,
        Guid atorId,
        Guid alvoId,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await GrupoAcesso.LockGrupoAsync(connection, transaction, grupoId, cancellationToken);
        var ator = await GrupoAcesso.GetPapelAsync(connection, transaction, grupoId, atorId, cancellationToken);
        if (ator is null)
        {
            return GrupoOperacaoStatus.GrupoNaoEncontrado;
        }

        if (!ValidacaoGrupo.CanTransferir(ator))
        {
            return GrupoOperacaoStatus.AcessoNegado;
        }

        if (alvoId == atorId)
        {
            return GrupoOperacaoStatus.DadosInvalidos;
        }

        var alvo = await GrupoAcesso.GetPapelAsync(connection, transaction, grupoId, alvoId, cancellationToken);
        if (alvo is null)
        {
            return GrupoOperacaoStatus.MembroNaoEncontrado;
        }

        // Nesta ordem por causa do índice que garante um único proprietário ativo.
        await MembrosGrupo.SetPapelAsync(connection, transaction, grupoId, atorId, PapelMembro.Administrador, cancellationToken);
        await MembrosGrupo.SetPapelAsync(connection, transaction, grupoId, alvoId, PapelMembro.Proprietario, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return GrupoOperacaoStatus.Concluida;
    }
}
