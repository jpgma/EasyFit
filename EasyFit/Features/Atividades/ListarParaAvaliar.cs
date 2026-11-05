using EasyFit.Features.Grupos.Contracts;
using EasyFit.Infrastructure.Authentication;
using EasyFit.Infrastructure.Json;
using Npgsql;

namespace EasyFit.Features.Atividades;

/// <summary>O que ainda espera a reação de quem consulta. A janela de 48 horas limita o tamanho da fila.</summary>
internal static class ListarParaAvaliar
{
    public static async Task<IResult> HandleAsync(
        Guid grupoId,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var usuarioId = context.GetSessaoAtual().UsuarioId;
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        if (await GrupoAcesso.GetPapelAsync(connection, null, grupoId, usuarioId, cancellationToken) is null)
        {
            return OperacaoAtividade.ToResult(AtividadeOperacaoStatus.GrupoNaoEncontrado);
        }

        await using var command = new NpgsqlCommand($"""
            {ConsultasAtividade.SelectResumo}
            WHERE a.grupo_id = $2
              AND a.situacao_verificacao IN ('AguardandoFeedback', 'EmRevisao')
              AND a.submitted_at + interval '48 hours' > now()
              AND EXISTS (
                  SELECT 1 FROM revisores_atividade v
                  WHERE v.atividade_id = a.id AND v.usuario_id = $1)
              AND NOT EXISTS (
                  SELECT 1 FROM reacoes_atividade r
                  WHERE r.atividade_id = a.id AND r.usuario_id = $1)
            ORDER BY a.submitted_at, a.id
            """, connection);
        command.Parameters.AddWithValue(usuarioId);
        command.Parameters.AddWithValue(grupoId);

        var atividades = new List<AtividadeResumoResponse>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            atividades.Add(ConsultasAtividade.ReadResumo(reader));
        }

        return Results.Json(new FilaAvaliacaoResponse(atividades), AppJsonContext.Default.FilaAvaliacaoResponse);
    }
}
