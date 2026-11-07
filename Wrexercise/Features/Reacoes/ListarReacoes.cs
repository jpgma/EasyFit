using Npgsql;
using Wrexercise.Infrastructure.Authentication;
using Wrexercise.Infrastructure.Database;
using Wrexercise.Infrastructure.Json;

namespace Wrexercise.Features.Reacoes;

internal static class ListarReacoes
{
    public static async Task<IResult> HandleAsync(
        Guid id,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var reacoes = await GetReacoesAsync(
            dataSource,
            id,
            context.GetSessaoAtual().UsuarioId,
            cancellationToken);

        return reacoes is null
            ? OperacaoReacao.ToResult(ReacaoOperacaoStatus.AtividadeNaoEncontrada)
            : Results.Json(reacoes, AppJsonContext.Default.ReacoesResponse);
    }

    internal static async Task<ReacoesResponse?> GetReacoesAsync(
        NpgsqlDataSource dataSource,
        Guid atividadeId,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        DateTimeOffset submittedAt;
        await using (var atividade = new NpgsqlCommand("""
            SELECT a.submitted_at
            FROM atividades a
            JOIN membros_grupo m ON m.grupo_id = a.grupo_id AND m.usuario_id = $2 AND m.removed_at IS NULL
            WHERE a.id = $1
            """, connection))
        {
            atividade.Parameters.AddWithValue(atividadeId);
            atividade.Parameters.AddWithValue(usuarioId);
            var scalar = await atividade.ExecuteScalarAsync(cancellationToken);
            if (scalar is null)
            {
                return null;
            }

            submittedAt = TimestampLeitura.FromScalar(scalar);
        }

        var (contagens, totalRevisores) = await VerificacaoAtividade.LoadContagensAsync(
            connection,
            null,
            atividadeId,
            cancellationToken);

        var voceERevisor = false;
        await using (var revisor = new NpgsqlCommand(
            "SELECT 1 FROM revisores_atividade WHERE atividade_id = $1 AND usuario_id = $2",
            connection))
        {
            revisor.Parameters.AddWithValue(atividadeId);
            revisor.Parameters.AddWithValue(usuarioId);
            voceERevisor = await revisor.ExecuteScalarAsync(cancellationToken) is not null;
        }

        string? suaReacao = null;
        var itens = new List<ReacaoItemResponse>();
        await using (var lista = new NpgsqlCommand("""
            SELECT r.usuario_id, u.nome_exibicao, r.tipo, r.updated_at
            FROM reacoes_atividade r
            JOIN usuarios u ON u.id = r.usuario_id
            WHERE r.atividade_id = $1
            ORDER BY u.nome_exibicao, r.usuario_id
            """, connection))
        {
            lista.Parameters.AddWithValue(atividadeId);
            await using var reader = await lista.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var revisorId = reader.GetGuid(0);
                var tipo = reader.GetString(2);
                if (revisorId == usuarioId)
                {
                    suaReacao = tipo;
                }

                itens.Add(new ReacaoItemResponse(
                    revisorId,
                    reader.GetString(1),
                    tipo,
                    TimestampLeitura.FromReader(reader, 3)));
            }
        }

        var encerraEm = ConsensoAtividade.RevisaoEncerraEm(submittedAt);
        var janelaAberta = ConsensoAtividade.JanelaAberta(submittedAt, DateTimeOffset.UtcNow);

        return new ReacoesResponse(
            totalRevisores,
            totalRevisores == 0 ? 0 : ConsensoAtividade.CalculateRejeicoesNecessarias(totalRevisores),
            contagens.Kudos,
            contagens.Suspeito,
            contagens.Rejeitar,
            suaReacao,
            voceERevisor,
            janelaAberta,
            encerraEm,
            itens);
    }
}
