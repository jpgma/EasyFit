using Npgsql;
using Wrexercise.Features.Grupos.Contracts;
using Wrexercise.Infrastructure.Authentication;

namespace Wrexercise.Features.Grupos;

internal sealed record AlterarPapelRequest(string? Papel);

/// <summary>
/// As permissões são verificadas depois de travar o grupo: duas alterações simultâneas
/// não podem decidir com base em papéis que a outra acabou de mudar.
/// </summary>
internal static class AlterarPapel
{
    public static async Task<IResult> HandleAsync(
        Guid id,
        Guid usuarioId,
        AlterarPapelRequest request,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        if (!ValidacaoGrupo.IsPapelAtribuivel(request.Papel))
        {
            return MembrosGrupo.InvalidData();
        }

        var status = await ExecutarAsync(
            dataSource,
            id,
            context.GetSessaoAtual().UsuarioId,
            usuarioId,
            request.Papel!,
            cancellationToken);

        return MembrosGrupo.ToResult(status);
    }

    private static async Task<GrupoOperacaoStatus> ExecutarAsync(
        NpgsqlDataSource dataSource,
        Guid grupoId,
        Guid atorId,
        Guid alvoId,
        string papel,
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

        if (!ValidacaoGrupo.CanAlterarPapel(ator, alvo))
        {
            return GrupoOperacaoStatus.AcessoNegado;
        }

        await MembrosGrupo.SetPapelAsync(connection, transaction, grupoId, alvoId, papel, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return GrupoOperacaoStatus.Concluida;
    }
}
