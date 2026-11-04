using Npgsql;

namespace EasyFit.Features.Grupos.Contracts;

internal enum GrupoEntradaStatus
{
    Adicionado,
    JaMembro,
    GrupoCheio,
}

internal static class GrupoEntrada
{
    public const int MaxMembros = 10;

    /// <summary>Quem volta ao grupo reaproveita o vínculo antigo, sempre como <see cref="PapelMembro.Membro"/>.</summary>
    public static async Task<GrupoEntradaStatus> AddMembroAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid grupoId,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await GrupoAcesso.LockGrupoAsync(connection, transaction, grupoId, cancellationToken);

        long total;
        long proprio;
        await using (var count = new NpgsqlCommand("""
            SELECT count(*), count(*) FILTER (WHERE usuario_id = $2)
            FROM membros_grupo
            WHERE grupo_id = $1 AND removed_at IS NULL
            """, connection, transaction))
        {
            count.Parameters.AddWithValue(grupoId);
            count.Parameters.AddWithValue(usuarioId);

            await using var reader = await count.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            total = reader.GetInt64(0);
            proprio = reader.GetInt64(1);
        }

        if (proprio > 0)
        {
            return GrupoEntradaStatus.JaMembro;
        }

        if (total >= MaxMembros)
        {
            return GrupoEntradaStatus.GrupoCheio;
        }

        await using var insert = new NpgsqlCommand("""
            INSERT INTO membros_grupo (grupo_id, usuario_id, papel)
            VALUES ($1, $2, $3)
            ON CONFLICT (grupo_id, usuario_id) DO UPDATE
            SET papel = EXCLUDED.papel, joined_at = now(), removed_at = NULL
            """, connection, transaction);
        insert.Parameters.AddWithValue(grupoId);
        insert.Parameters.AddWithValue(usuarioId);
        insert.Parameters.AddWithValue(PapelMembro.Membro);
        await insert.ExecuteNonQueryAsync(cancellationToken);

        return GrupoEntradaStatus.Adicionado;
    }
}
