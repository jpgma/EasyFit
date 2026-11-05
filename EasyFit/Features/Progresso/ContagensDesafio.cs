using EasyFit.Infrastructure.Database;
using Npgsql;

namespace EasyFit.Features.Progresso;

internal sealed record DesafioCompeticaoContext(
    Guid DesafioId,
    Guid GrupoId,
    DateOnly DataInicio,
    DateOnly DataFim,
    DateOnly Hoje,
    int SessoesPorSemana,
    bool Cancelado,
    DateOnly EntradaUsuario);

internal static class ContagensDesafio
{
    /// <returns><c>null</c> se o desafio não existir ou o usuário não for membro ativo.</returns>
    public static async Task<DesafioCompeticaoContext?> LoadDesafioAsync(
        NpgsqlConnection connection,
        Guid desafioId,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            SELECT d.grupo_id, d.data_inicio, d.data_fim,
                (now() AT TIME ZONE g.fuso_horario)::date,
                d.sessoes_por_semana, d.cancelled_at IS NOT NULL,
                (m.joined_at AT TIME ZONE g.fuso_horario)::date
            FROM desafios d
            JOIN grupos g ON g.id = d.grupo_id
            JOIN membros_grupo m ON m.grupo_id = d.grupo_id AND m.usuario_id = $2 AND m.removed_at IS NULL
            WHERE d.id = $1
            """, connection);
        command.Parameters.AddWithValue(desafioId);
        command.Parameters.AddWithValue(usuarioId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new DesafioCompeticaoContext(
            desafioId,
            reader.GetGuid(0),
            reader.GetFieldValue<DateOnly>(1),
            reader.GetFieldValue<DateOnly>(2),
            reader.GetFieldValue<DateOnly>(3),
            reader.GetInt32(4),
            reader.GetBoolean(5),
            reader.GetFieldValue<DateOnly>(6));
    }

    public static async Task<IReadOnlyList<(Guid UsuarioId, string NomeExibicao)>> LoadParticipantesAsync(
        NpgsqlConnection connection,
        Guid desafioId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            SELECT m.usuario_id, u.nome_exibicao
            FROM desafios d
            JOIN grupos g ON g.id = d.grupo_id
            JOIN membros_grupo m ON m.grupo_id = d.grupo_id AND m.removed_at IS NULL
            JOIN usuarios u ON u.id = m.usuario_id
            WHERE d.id = $1
              AND (m.joined_at AT TIME ZONE g.fuso_horario)::date <= d.data_fim - 6
            ORDER BY u.nome_exibicao, m.usuario_id
            """, connection);
        command.Parameters.AddWithValue(desafioId);

        var participantes = new List<(Guid, string)>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            participantes.Add((reader.GetGuid(0), reader.GetString(1)));
        }

        return participantes;
    }

    public static async Task<Dictionary<(Guid UsuarioId, int IndiceSemana), (int Verificadas, int Pendentes)>> LoadMatrizAsync(
        NpgsqlConnection connection,
        Guid desafioId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            ContagensAtividadeDesafio.BuildAgregadoPorUsuarioESemana(),
            connection);
        command.Parameters.AddWithValue(desafioId);

        var matriz = new Dictionary<(Guid, int), (int, int)>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            matriz[(reader.GetGuid(0), reader.GetInt32(1))] = (reader.GetInt32(2), reader.GetInt32(3));
        }

        return matriz;
    }
}
