using EasyFit.Features.Grupos.Contracts;
using EasyFit.Infrastructure.Authentication;
using Npgsql;

namespace EasyFit.Features.Convites;

internal enum ConviteCriacaoStatus
{
    Criado,
    GrupoNaoEncontrado,
    AcessoNegado,
    GrupoCheio,
}

internal readonly record struct ConviteCriacaoResult(ConviteCriacaoStatus Status, ConviteCriadoResponse? Convite = null);

internal static class ConvitesCommands
{
    public static async Task<ConviteCriacaoResult> CreateAsync(
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

        if (!ConvitesRules.CanGerenciarConvites(papel))
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

        var token = OpaqueToken.Generate();
        var expiresAt = DateTimeOffset.UtcNow + ConvitesRules.Validade;

        await using var insert = new NpgsqlCommand("""
            INSERT INTO convites_grupo (grupo_id, criador_id, token_hash, expires_at)
            VALUES ($1, $2, $3, $4)
            RETURNING id
            """, connection, transaction);
        insert.Parameters.AddWithValue(grupoId);
        insert.Parameters.AddWithValue(usuarioId);
        insert.Parameters.AddWithValue(OpaqueToken.Hash(token));
        insert.Parameters.AddWithValue(expiresAt);
        var id = (Guid)(await insert.ExecuteScalarAsync(cancellationToken))!;

        await transaction.CommitAsync(cancellationToken);
        return new ConviteCriacaoResult(ConviteCriacaoStatus.Criado, new ConviteCriadoResponse(id, token, expiresAt));
    }

    /// <returns><c>false</c> se o convite não existir neste grupo ou não estiver mais pendente.</returns>
    public static async Task<bool> RevogarAsync(
        NpgsqlDataSource dataSource,
        Guid grupoId,
        Guid conviteId,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("""
            UPDATE convites_grupo
            SET revoked_at = now()
            WHERE id = $1 AND grupo_id = $2 AND used_at IS NULL AND revoked_at IS NULL AND expires_at > now()
            """);
        command.Parameters.AddWithValue(conviteId);
        command.Parameters.AddWithValue(grupoId);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }
}
