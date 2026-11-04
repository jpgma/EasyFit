using EasyFit.Features.Grupos.Contracts;
using EasyFit.Infrastructure.Authentication;
using Npgsql;

namespace EasyFit.Features.Grupos;

internal static class RemoverMembro
{
    public static async Task<IResult> HandleAsync(
        Guid id,
        Guid usuarioId,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var status = await ExecutarAsync(
            dataSource,
            id,
            context.GetSessaoAtual().UsuarioId,
            usuarioId,
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

        var alvo = await GrupoAcesso.GetPapelAsync(connection, transaction, grupoId, alvoId, cancellationToken);
        if (alvo is null)
        {
            return GrupoOperacaoStatus.MembroNaoEncontrado;
        }

        if (!ValidacaoGrupo.CanRemoverMembro(ator, alvo))
        {
            return GrupoOperacaoStatus.AcessoNegado;
        }

        await MembrosGrupo.DesligarAsync(connection, transaction, grupoId, alvoId, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return GrupoOperacaoStatus.Concluida;
    }
}
