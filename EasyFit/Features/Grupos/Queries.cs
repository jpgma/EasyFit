using EasyFit.Features.Grupos.Contracts;
using Npgsql;

namespace EasyFit.Features.Grupos;

internal static class GruposQueries
{
    public static async Task<IReadOnlyList<GrupoResumoResponse>> ListByUsuarioAsync(
        NpgsqlDataSource dataSource,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("""
            SELECT g.id, g.nome, m.papel,
                (SELECT count(*)::int FROM membros_grupo t WHERE t.grupo_id = g.id AND t.removed_at IS NULL)
            FROM membros_grupo m
            JOIN grupos g ON g.id = m.grupo_id
            WHERE m.usuario_id = $1 AND m.removed_at IS NULL
            ORDER BY m.joined_at, g.id
            """);
        command.Parameters.AddWithValue(usuarioId);

        var grupos = new List<GrupoResumoResponse>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            grupos.Add(new GrupoResumoResponse(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3)));
        }

        return grupos;
    }

    /// <returns>O grupo, ou <c>null</c> se ele não existir ou o usuário não for membro.</returns>
    public static async Task<GrupoResponse?> GetAsync(
        NpgsqlDataSource dataSource,
        Guid grupoId,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("""
            SELECT g.id, g.nome, g.fuso_horario, m.papel,
                (SELECT count(*)::int FROM membros_grupo t WHERE t.grupo_id = g.id AND t.removed_at IS NULL)
            FROM grupos g
            JOIN membros_grupo m ON m.grupo_id = g.id
            WHERE g.id = $1 AND m.usuario_id = $2 AND m.removed_at IS NULL
            """);
        command.Parameters.AddWithValue(grupoId);
        command.Parameters.AddWithValue(usuarioId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new GrupoResponse(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetInt32(4),
                GrupoEntrada.MaxMembros)
            : null;
    }

    public static async Task<IReadOnlyList<MembroResponse>> ListMembrosAsync(
        NpgsqlDataSource dataSource,
        Guid grupoId,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("""
            SELECT m.usuario_id, u.nome_exibicao, m.papel, m.joined_at
            FROM membros_grupo m
            JOIN usuarios u ON u.id = m.usuario_id
            WHERE m.grupo_id = $1 AND m.removed_at IS NULL
            ORDER BY
                CASE m.papel WHEN 'Proprietario' THEN 0 WHEN 'Administrador' THEN 1 ELSE 2 END,
                u.nome_exibicao,
                m.usuario_id
            """);
        command.Parameters.AddWithValue(grupoId);

        var membros = new List<MembroResponse>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            membros.Add(new MembroResponse(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetFieldValue<DateTimeOffset>(3)));
        }

        return membros;
    }

    // O PostgreSQL decide: com InvariantGlobalization, o .NET não reconhece fusos IANA no Windows.
    public static async Task<bool> IsFusoHorarioValidoAsync(
        NpgsqlDataSource dataSource,
        string fusoHorario,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            "SELECT EXISTS (SELECT 1 FROM pg_timezone_names WHERE name = $1)");
        command.Parameters.AddWithValue(fusoHorario);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
    }
}
