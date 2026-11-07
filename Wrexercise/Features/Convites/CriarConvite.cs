using Npgsql;
using Wrexercise.Features.Grupos.Contracts;
using Wrexercise.Infrastructure.Authentication;
using Wrexercise.Infrastructure.Json;

namespace Wrexercise.Features.Convites;

internal enum ConviteCriacaoStatus
{
    Criado,
    GrupoNaoEncontrado,
    AcessoNegado,
    GrupoCheio,
}

internal readonly record struct ConviteCriacaoResult(ConviteCriacaoStatus Status, ConviteResponse? Convite = null);

/// <summary>Gera o link do grupo, substituindo o anterior.</summary>
internal static class CriarConvite
{
    public static async Task<IResult> HandleAsync(
        Guid id,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var result = await ExecutarAsync(
            dataSource,
            id,
            context.GetSessaoAtual().UsuarioId,
            cancellationToken);

        return result.Status switch
        {
            ConviteCriacaoStatus.Criado => Results.Json(
                result.Convite!,
                AppJsonContext.Default.ConviteResponse,
                statusCode: StatusCodes.Status201Created),
            ConviteCriacaoStatus.GrupoNaoEncontrado => GrupoAcesso.GrupoNaoEncontrado(),
            ConviteCriacaoStatus.AcessoNegado => GrupoAcesso.AcessoNegado(),
            ConviteCriacaoStatus.GrupoCheio => GrupoAcesso.GrupoCheio(),
            _ => throw new ArgumentOutOfRangeException(nameof(result)),
        };
    }

    private static async Task<ConviteCriacaoResult> ExecutarAsync(
        NpgsqlDataSource dataSource,
        Guid grupoId,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // Com o grupo travado, quem acabou de ser removido não consegue deixar um convite novo para trás.
        await GrupoAcesso.LockGrupoAsync(connection, transaction, grupoId, cancellationToken);
        var papel = await GrupoAcesso.GetPapelAsync(connection, transaction, grupoId, usuarioId, cancellationToken);
        if (papel is null)
        {
            return new ConviteCriacaoResult(ConviteCriacaoStatus.GrupoNaoEncontrado);
        }

        if (!ValidacaoConvite.CanGerenciarConvites(papel))
        {
            return new ConviteCriacaoResult(ConviteCriacaoStatus.AcessoNegado);
        }

        await using (var count = new NpgsqlCommand(
            "SELECT count(*) FROM membros_grupo WHERE grupo_id = $1 AND removed_at IS NULL",
            connection,
            transaction))
        {
            count.Parameters.AddWithValue(grupoId);
            if ((long)(await count.ExecuteScalarAsync(cancellationToken))! >= GrupoEntrada.MaxMembros)
            {
                return new ConviteCriacaoResult(ConviteCriacaoStatus.GrupoCheio);
            }
        }

        await using (var anterior = new NpgsqlCommand(
            "UPDATE convites_grupo SET revoked_at = now() WHERE grupo_id = $1 AND revoked_at IS NULL",
            connection,
            transaction))
        {
            anterior.Parameters.AddWithValue(grupoId);
            await anterior.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var insert = new NpgsqlCommand("""
            INSERT INTO convites_grupo (grupo_id, criador_id, token, expires_at)
            VALUES ($1, $2, $3, $4)
            """, connection, transaction))
        {
            insert.Parameters.AddWithValue(grupoId);
            insert.Parameters.AddWithValue(usuarioId);
            insert.Parameters.AddWithValue(OpaqueToken.Generate());
            insert.Parameters.AddWithValue(DateTimeOffset.UtcNow + ValidacaoConvite.Validade);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        var convite = await ObterConvite.GetAtivoAsync(connection, transaction, grupoId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new ConviteCriacaoResult(ConviteCriacaoStatus.Criado, convite);
    }
}
