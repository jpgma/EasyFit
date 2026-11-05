namespace EasyFit.Infrastructure.Database;

/// <summary>
/// Fragmento SQL canônico para contagens de atividades elegíveis a um desafio.
/// Ver docs/decisoes/0013-competicao-e-progresso.md (SQL de contagens por desafio).
/// </summary>
internal static class ContagensAtividadeDesafio
{
    internal const string IndiceSemanaSql =
        "((a.started_at AT TIME ZONE g.fuso_horario)::date - d.data_inicio) / 7";

    internal const string VerificadasSql =
        "count(*) FILTER (WHERE a.situacao_verificacao = 'Verificada')::int";

    internal const string PendentesSql = """
        count(*) FILTER (WHERE a.situacao_verificacao IN (
            'AguardandoFeedback', 'EmRevisao', 'Contestada'))::int
        """;

    /// <summary>
    /// Corpo comum: atividades elegíveis de membros ativos, só em semanas que começaram no dia
    /// da entrada do membro ou depois. Parâmetro $1 = desafio_id.
    /// </summary>
    internal const string FromElegiveisSql = $"""
        FROM atividades a
        JOIN desafios d ON d.id = $1 AND d.grupo_id = a.grupo_id
        JOIN grupos g ON g.id = d.grupo_id
        JOIN membros_grupo m ON m.grupo_id = d.grupo_id AND m.usuario_id = a.usuario_id AND m.removed_at IS NULL
        WHERE a.duracao_segundos >= d.duracao_minima_segundos
          AND (a.started_at AT TIME ZONE g.fuso_horario)::date >= d.data_inicio
          AND (a.started_at AT TIME ZONE g.fuso_horario)::date <= d.data_fim
          AND d.data_inicio + {IndiceSemanaSql} * 7 >= (m.joined_at AT TIME ZONE g.fuso_horario)::date
        """;

    internal static string BuildAgregadoPorUsuarioESemana() => $"""
        SELECT a.usuario_id,
            {IndiceSemanaSql} AS indice_semana,
            {VerificadasSql} AS verificadas,
            {PendentesSql} AS pendentes
        {FromElegiveisSql}
        GROUP BY a.usuario_id, {IndiceSemanaSql}
        """;

    /// <summary>Dias com treino elegível e não rejeitado. $1 = desafio_id, $2 e $3 = a semana, $4 = usuario_id.</summary>
    internal static string BuildDiasComAtividade() => $"""
        SELECT DISTINCT (a.started_at AT TIME ZONE g.fuso_horario)::date
        {FromElegiveisSql}
          AND (a.started_at AT TIME ZONE g.fuso_horario)::date >= $2
          AND (a.started_at AT TIME ZONE g.fuso_horario)::date <= $3
          AND a.usuario_id = $4
          AND a.situacao_verificacao <> 'Rejeitada'
        """;

    internal static string BuildAgregadoPorUsuarioSemanaAtual() => $"""
        SELECT a.usuario_id,
            {VerificadasSql} AS verificadas,
            {PendentesSql} AS pendentes
        {FromElegiveisSql}
          AND (a.started_at AT TIME ZONE g.fuso_horario)::date >= $2
          AND (a.started_at AT TIME ZONE g.fuso_horario)::date <= $3
        GROUP BY a.usuario_id
        """;
}
