using EasyFit.Features.Grupos.Contracts;
using EasyFit.Infrastructure.Authentication;
using Npgsql;

namespace EasyFit.Features.Grupos;

internal static class SairDoGrupo
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

        return MembrosGrupo.ToResult(status);
    }

    private static async Task<GrupoOperacaoStatus> ExecutarAsync(
        NpgsqlDataSource dataSource,
        Guid grupoId,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await GrupoAcesso.LockGrupoAsync(connection, transaction, grupoId, cancellationToken);
        var papel = await GrupoAcesso.GetPapelAsync(connection, transaction, grupoId, usuarioId, cancellationToken);
        if (papel is null)
        {
            return GrupoOperacaoStatus.GrupoNaoEncontrado;
        }

        if (!ValidacaoGrupo.CanSair(papel))
        {
            return GrupoOperacaoStatus.ProprietarioNaoPodeSair;
        }

        await MembrosGrupo.DesligarAsync(connection, transaction, grupoId, usuarioId, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return GrupoOperacaoStatus.Concluida;
    }
}
