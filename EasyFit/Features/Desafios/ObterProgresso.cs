using EasyFit.Infrastructure.Authentication;
using Npgsql;

namespace EasyFit.Features.Desafios;

internal static class ObterProgresso
{
    public static async Task<IResult> HandleAsync(
        Guid id,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var progresso = await GetProgressoAsync(
            dataSource,
            id,
            context.GetSessaoAtual().UsuarioId,
            cancellationToken);

        return progresso is null ? OperacaoDesafio.DesafioNaoEncontrado() : Results.Ok(progresso);
    }

    /// <returns>O progresso, ou <c>null</c> se o desafio não existir ou o usuário não for membro do grupo.</returns>
    private static async Task<ProgressoResponse?> GetProgressoAsync(
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
        var zerado = ProgressoDesafio.CalculateProgresso(0, 0, sessoesPorSemana);
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
            ProgressoDesafio.CountSemanas(inicio, fim),
            cancelado ? null : ProgressoDesafio.GetSemana(inicio, fim, hoje),
            progresso);
    }
}
