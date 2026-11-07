using Npgsql;
using Wrexercise.Features.Notificacoes.Contracts;
using Wrexercise.Infrastructure.Authentication;
using Wrexercise.Infrastructure.Database;
using Wrexercise.Infrastructure.Json;

namespace Wrexercise.Features.Reacoes;

internal static class RegistrarReacao
{
    public static async Task<IResult> HandleAsync(
        Guid id,
        ReacaoRequest request,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        if (!TipoReacao.IsValid(request.Tipo))
        {
            return OperacaoReacao.InvalidData();
        }

        var usuarioId = context.GetSessaoAtual().UsuarioId;
        var status = await ExecutarAsync(dataSource, id, usuarioId, request.Tipo!, cancellationToken);
        if (status != ReacaoOperacaoStatus.Concluida)
        {
            return OperacaoReacao.ToResult(status);
        }

        await VerificacaoAtividade.EncerrarJanelasExpiradasAsync(dataSource, cancellationToken);

        var reacoes = await ListarReacoes.GetReacoesAsync(dataSource, id, usuarioId, cancellationToken);
        return reacoes is null
            ? OperacaoReacao.ToResult(ReacaoOperacaoStatus.AtividadeNaoEncontrada)
            : Results.Json(reacoes, AppJsonContext.Default.ReacoesResponse);
    }

    private static async Task<ReacaoOperacaoStatus> ExecutarAsync(
        NpgsqlDataSource dataSource,
        Guid atividadeId,
        Guid usuarioId,
        string tipo,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        Guid autorId;
        string situacao;
        DateTimeOffset submittedAt;

        await using (var load = new NpgsqlCommand("""
            SELECT usuario_id, situacao_verificacao, submitted_at
            FROM atividades
            WHERE id = $1
            FOR UPDATE
            """, connection, transaction))
        {
            load.Parameters.AddWithValue(atividadeId);
            await using var reader = await load.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return ReacaoOperacaoStatus.AtividadeNaoEncontrada;
            }

            autorId = reader.GetGuid(0);
            situacao = reader.GetString(1);
            submittedAt = TimestampLeitura.FromReader(reader, 2);
        }

        if (autorId == usuarioId)
        {
            return ReacaoOperacaoStatus.AutorNaoReage;
        }

        if (!await IsMembroDoGrupoAsync(connection, transaction, atividadeId, usuarioId, cancellationToken))
        {
            return ReacaoOperacaoStatus.AtividadeNaoEncontrada;
        }

        if (situacao is not (SituacaoVerificacao.AguardandoFeedback or SituacaoVerificacao.EmRevisao))
        {
            return ReacaoOperacaoStatus.ReacaoEncerrada;
        }

        if (!ConsensoAtividade.JanelaAberta(submittedAt, DateTimeOffset.UtcNow))
        {
            return ReacaoOperacaoStatus.JanelaEncerrada;
        }

        await using (var revisor = new NpgsqlCommand("""
            SELECT 1 FROM revisores_atividade WHERE atividade_id = $1 AND usuario_id = $2
            """, connection, transaction))
        {
            revisor.Parameters.AddWithValue(atividadeId);
            revisor.Parameters.AddWithValue(usuarioId);
            if (await revisor.ExecuteScalarAsync(cancellationToken) is null)
            {
                return ReacaoOperacaoStatus.NaoRevisor;
            }
        }

        await using (var upsert = new NpgsqlCommand("""
            INSERT INTO reacoes_atividade (atividade_id, usuario_id, tipo)
            VALUES ($1, $2, $3)
            ON CONFLICT (atividade_id, usuario_id)
            DO UPDATE SET tipo = EXCLUDED.tipo, updated_at = now()
            RETURNING created_at = updated_at
            """, connection, transaction))
        {
            upsert.Parameters.AddWithValue(atividadeId);
            upsert.Parameters.AddWithValue(usuarioId);
            upsert.Parameters.AddWithValue(tipo);

            // Trocar de reação não é uma reação nova: o autor já foi avisado desta pessoa.
            if ((bool)(await upsert.ExecuteScalarAsync(cancellationToken))!)
            {
                await NotificacoesAtividade.NotificarReacaoAsync(
                    connection,
                    transaction,
                    atividadeId,
                    usuarioId,
                    cancellationToken);
            }
        }

        await VerificacaoAtividade.AplicarSituacaoCalculadaAsync(
            connection,
            transaction,
            atividadeId,
            submittedAt,
            situacao,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return ReacaoOperacaoStatus.Concluida;
    }

    private static async Task<bool> IsMembroDoGrupoAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid atividadeId,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            SELECT 1
            FROM atividades a
            JOIN membros_grupo m ON m.grupo_id = a.grupo_id AND m.usuario_id = $2 AND m.removed_at IS NULL
            WHERE a.id = $1
            """, connection, transaction);
        command.Parameters.AddWithValue(atividadeId);
        command.Parameters.AddWithValue(usuarioId);
        return await command.ExecuteScalarAsync(cancellationToken) is not null;
    }
}
