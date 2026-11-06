using EasyFit.Features.Desafios.Contracts;
using EasyFit.Infrastructure.Authentication;
using EasyFit.Infrastructure.Database;
using EasyFit.Infrastructure.Json;
using Npgsql;

namespace EasyFit.Features.Usuarios;

internal static class ObterMeuPerfil
{
    private const int SemanasNoGrafico = 8;

    public static async Task<IResult> HandleAsync(
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var usuarioId = context.GetSessaoAtual().UsuarioId;
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        DateTimeOffset membroDesde;
        await using (var usuario = new NpgsqlCommand("SELECT created_at FROM usuarios WHERE id = $1", connection))
        {
            usuario.Parameters.AddWithValue(usuarioId);
            membroDesde = TimestampLeitura.FromScalar((await usuario.ExecuteScalarAsync(cancellationToken))!);
        }

        int totalTreinos;
        long totalSegundos;
        int antesDas7;
        await using (var totais = new NpgsqlCommand("""
            SELECT count(*)::int,
                COALESCE(sum(a.duracao_segundos), 0)::bigint,
                count(*) FILTER (WHERE (a.started_at AT TIME ZONE g.fuso_horario)::time < time '07:00')::int
            FROM atividades a
            JOIN grupos g ON g.id = a.grupo_id
            WHERE a.usuario_id = $1 AND a.situacao_verificacao = 'Verificada'
            """, connection))
        {
            totais.Parameters.AddWithValue(usuarioId);
            await using var reader = await totais.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            totalTreinos = reader.GetInt32(0);
            totalSegundos = reader.GetInt64(1);
            antesDas7 = reader.GetInt32(2);
        }

        var melhorSequencia = await LoadMelhorSequenciaAsync(connection, usuarioId, cancellationToken);
        var semanas = await LoadSemanasAsync(connection, usuarioId, cancellationToken);

        return Results.Json(
            new MeuPerfilResponse(
                membroDesde,
                totalTreinos,
                totalSegundos,
                melhorSequencia,
                semanas,
                ConquistasUsuario.Calcular(new EstatisticasConquistas(totalTreinos, melhorSequencia, antesDas7))),
            AppJsonContext.Default.MeuPerfilResponse);
    }

    /// <summary>
    /// A melhor sequência em um único desafio, com as mesmas regras de elegibilidade da
    /// classificação. Sair de um grupo não apaga o que a pessoa conquistou nele.
    /// </summary>
    private static async Task<int> LoadMelhorSequenciaAsync(
        NpgsqlConnection connection,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand($"""
            SELECT d.id, {ContagensAtividadeDesafio.IndiceSemanaSql}
            FROM atividades a
            JOIN desafios d ON d.grupo_id = a.grupo_id AND d.cancelled_at IS NULL
            JOIN grupos g ON g.id = d.grupo_id
            JOIN membros_grupo m ON m.grupo_id = d.grupo_id AND m.usuario_id = a.usuario_id
            WHERE a.usuario_id = $1
              AND a.situacao_verificacao = 'Verificada'
              AND a.duracao_segundos >= d.duracao_minima_segundos
              AND (a.started_at AT TIME ZONE g.fuso_horario)::date >= d.data_inicio
              AND (a.started_at AT TIME ZONE g.fuso_horario)::date <= d.data_fim
              AND d.data_inicio + {ContagensAtividadeDesafio.IndiceSemanaSql} * 7 >= (m.joined_at AT TIME ZONE g.fuso_horario)::date
            GROUP BY d.id, d.sessoes_por_semana, {ContagensAtividadeDesafio.IndiceSemanaSql}
            HAVING count(*) >= d.sessoes_por_semana
            """, connection);
        command.Parameters.AddWithValue(usuarioId);

        var porDesafio = new Dictionary<Guid, List<int>>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var desafioId = reader.GetGuid(0);
            if (!porDesafio.TryGetValue(desafioId, out var indices))
            {
                indices = [];
                porDesafio[desafioId] = indices;
            }

            indices.Add(reader.GetInt32(1));
        }

        return porDesafio.Values.Select(CalculoSemanaDesafio.GetMelhorSequencia).DefaultIfEmpty(0).Max();
    }

    private static async Task<List<SemanaConsistenciaResponse>> LoadSemanasAsync(
        NpgsqlConnection connection,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        // date_trunc('week') segue a semana ISO, de segunda a domingo. A semana atual vem do fuso
        // dos grupos da pessoa, como as atividades; com fusos diferentes, vale o primeiro a virar
        // a semana. Sem grupo, vale o UTC.
        await using var command = new NpgsqlCommand($"""
            WITH hoje AS (
                SELECT COALESCE(
                    (SELECT max((now() AT TIME ZONE g.fuso_horario)::date)
                     FROM membros_grupo m
                     JOIN grupos g ON g.id = m.grupo_id
                     WHERE m.usuario_id = $1 AND m.removed_at IS NULL),
                    (now() AT TIME ZONE 'UTC')::date) AS dia
            ),
            semanas AS (
                SELECT generate_series(
                    date_trunc('week', hoje.dia) - interval '{SemanasNoGrafico - 1} weeks',
                    date_trunc('week', hoje.dia),
                    interval '1 week')::date AS inicio
                FROM hoje
            )
            SELECT s.inicio,
                count(a.id) FILTER (WHERE a.situacao_verificacao = 'Verificada')::int,
                count(a.id) FILTER (WHERE a.situacao_verificacao <> 'Rejeitada')::int
            FROM semanas s
            LEFT JOIN (atividades a JOIN grupos g ON g.id = a.grupo_id)
                ON a.usuario_id = $1
                AND date_trunc('week', a.started_at AT TIME ZONE g.fuso_horario)::date = s.inicio
            GROUP BY s.inicio
            ORDER BY s.inicio
            """, connection);
        command.Parameters.AddWithValue(usuarioId);

        var semanas = new List<SemanaConsistenciaResponse>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            semanas.Add(new SemanaConsistenciaResponse(
                reader.GetFieldValue<DateOnly>(0),
                reader.GetInt32(1),
                reader.GetInt32(2)));
        }

        return semanas;
    }
}
