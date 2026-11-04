using Npgsql;

namespace EasyFit.Features.Desafios;

/// <summary>
/// Toda consulta parte do vínculo de quem pergunta: sem ser membro ativo do grupo,
/// o desafio simplesmente não aparece.
/// </summary>
internal static class DesafiosQueries
{
    // Participante ativo é quem tem a linha e continua no grupo; voltar ao grupo devolve a participação.
    private const string SelectDesafio = """
        SELECT d.id, d.grupo_id, d.nome, d.descricao, d.data_inicio, d.data_fim,
            d.duracao_minima_segundos, d.sessoes_por_semana, d.cancelled_at IS NOT NULL,
            (now() AT TIME ZONE g.fuso_horario)::date,
            g.fuso_horario, m.papel,
            EXISTS (SELECT 1 FROM participantes_desafio p WHERE p.desafio_id = d.id AND p.usuario_id = $1),
            (SELECT count(*)::int
             FROM participantes_desafio p
             JOIN membros_grupo t ON t.grupo_id = d.grupo_id AND t.usuario_id = p.usuario_id AND t.removed_at IS NULL
             WHERE p.desafio_id = d.id)
        FROM desafios d
        JOIN grupos g ON g.id = d.grupo_id
        JOIN membros_grupo m ON m.grupo_id = d.grupo_id AND m.usuario_id = $1 AND m.removed_at IS NULL
        """;

    /// <summary>Os que ainda valem vêm primeiro; dentro de cada faixa, os de início mais recente.</summary>
    public static async Task<IReadOnlyList<DesafioResponse>> ListByGrupoAsync(
        NpgsqlDataSource dataSource,
        Guid grupoId,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(SelectDesafio + """

            WHERE d.grupo_id = $2
            ORDER BY
                d.cancelled_at IS NOT NULL,
                d.data_fim < (now() AT TIME ZONE g.fuso_horario)::date,
                d.data_inicio DESC,
                d.id
            """);
        command.Parameters.AddWithValue(usuarioId);
        command.Parameters.AddWithValue(grupoId);

        var desafios = new List<DesafioResponse>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            desafios.Add(ReadDesafio(reader));
        }

        return desafios;
    }

    /// <returns>O desafio, ou <c>null</c> se ele não existir ou o usuário não for membro do grupo.</returns>
    public static async Task<DesafioResponse?> GetAsync(
        NpgsqlDataSource dataSource,
        Guid desafioId,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(SelectDesafio + """

            WHERE d.id = $2
            """);
        command.Parameters.AddWithValue(usuarioId);
        command.Parameters.AddWithValue(desafioId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadDesafio(reader) : null;
    }

    /// <returns>O progresso, ou <c>null</c> se o desafio não existir ou o usuário não for membro do grupo.</returns>
    public static async Task<ProgressoResponse?> GetProgressoAsync(
        NpgsqlDataSource dataSource,
        Guid desafioId,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        DateOnly inicio;
        DateOnly fim;
        DateOnly hoje;
        int sessoesPorSemana;
        bool cancelado;
        await using (var desafio = new NpgsqlCommand("""
            SELECT d.data_inicio, d.data_fim, (now() AT TIME ZONE g.fuso_horario)::date,
                d.sessoes_por_semana, d.cancelled_at IS NOT NULL
            FROM desafios d
            JOIN grupos g ON g.id = d.grupo_id
            JOIN membros_grupo m ON m.grupo_id = d.grupo_id AND m.usuario_id = $2 AND m.removed_at IS NULL
            WHERE d.id = $1
            """, connection))
        {
            desafio.Parameters.AddWithValue(desafioId);
            desafio.Parameters.AddWithValue(usuarioId);

            await using var reader = await desafio.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            inicio = reader.GetFieldValue<DateOnly>(0);
            fim = reader.GetFieldValue<DateOnly>(1);
            hoje = reader.GetFieldValue<DateOnly>(2);
            sessoesPorSemana = reader.GetInt32(3);
            cancelado = reader.GetBoolean(4);
        }

        await using var participantes = new NpgsqlCommand("""
            SELECT p.usuario_id, u.nome_exibicao
            FROM participantes_desafio p
            JOIN desafios d ON d.id = p.desafio_id
            JOIN membros_grupo m ON m.grupo_id = d.grupo_id AND m.usuario_id = p.usuario_id AND m.removed_at IS NULL
            JOIN usuarios u ON u.id = p.usuario_id
            WHERE p.desafio_id = $1
            ORDER BY u.nome_exibicao, p.usuario_id
            """, connection);
        participantes.Parameters.AddWithValue(desafioId);

        // As atividades só existem a partir da Fase 5: até lá, toda contagem é zero.
        var zerado = DesafiosRules.CalculateProgresso(0, 0, sessoesPorSemana);
        var progresso = new List<ProgressoParticipanteResponse>();
        await using (var reader = await participantes.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                progresso.Add(new ProgressoParticipanteResponse(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    zerado.Verificadas,
                    zerado.Provisorias,
                    zerado.MetaCumprida));
            }
        }

        return new ProgressoResponse(
            sessoesPorSemana,
            DesafiosRules.CountSemanas(inicio, fim),
            cancelado ? null : DesafiosRules.GetSemana(inicio, fim, hoje),
            progresso);
    }

    private static DesafioResponse ReadDesafio(NpgsqlDataReader reader)
    {
        var inicio = reader.GetFieldValue<DateOnly>(4);
        var fim = reader.GetFieldValue<DateOnly>(5);

        return new DesafioResponse(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            inicio,
            fim,
            reader.GetInt32(6),
            reader.GetInt32(7),
            DesafiosRules.GetSituacao(inicio, fim, reader.GetBoolean(8), reader.GetFieldValue<DateOnly>(9)),
            reader.GetString(10),
            reader.GetString(11),
            reader.GetBoolean(12),
            reader.GetInt32(13));
    }
}
