using EasyFit.Features.Grupos.Contracts;
using Npgsql;
using NpgsqlTypes;

namespace EasyFit.Features.Desafios;

internal enum DesafioOperacaoStatus
{
    Concluida,
    GrupoNaoEncontrado,
    DesafioNaoEncontrado,
    AcessoNegado,
    DesafioEncerrado,
    DesafioCancelado,
    DadosInvalidos,
}

internal sealed record NovoDesafio(
    string Nome,
    string? Descricao,
    DateOnly DataInicio,
    DateOnly DataFim,
    int DuracaoMinimaSegundos,
    int SessoesPorSemana);

/// <summary>Quem altera um desafio trava o grupo antes de verificar o papel, como em toda operação do grupo.</summary>
internal static class DesafiosCommands
{
    /// <summary>Quem cria já entra como participante, na mesma transação.</summary>
    public static async Task<(DesafioOperacaoStatus Status, Guid Id)> CreateAsync(
        NpgsqlDataSource dataSource,
        Guid grupoId,
        Guid usuarioId,
        NovoDesafio novo,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await GrupoAcesso.LockGrupoAsync(connection, transaction, grupoId, cancellationToken);
        var papel = await GrupoAcesso.GetPapelAsync(connection, transaction, grupoId, usuarioId, cancellationToken);
        if (papel is null)
        {
            return (DesafioOperacaoStatus.GrupoNaoEncontrado, Guid.Empty);
        }

        if (!DesafiosRules.CanGerenciar(papel))
        {
            return (DesafioOperacaoStatus.AcessoNegado, Guid.Empty);
        }

        DateOnly hoje;
        await using (var select = new NpgsqlCommand(
            "SELECT (now() AT TIME ZONE fuso_horario)::date FROM grupos WHERE id = $1",
            connection,
            transaction))
        {
            select.Parameters.AddWithValue(grupoId);
            await using var reader = await select.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            hoje = reader.GetFieldValue<DateOnly>(0);
        }

        if (!DesafiosRules.IsValidPeriodo(novo.DataInicio, novo.DataFim, hoje))
        {
            return (DesafioOperacaoStatus.DadosInvalidos, Guid.Empty);
        }

        Guid desafioId;
        await using (var insert = new NpgsqlCommand("""
            INSERT INTO desafios (
                grupo_id, criador_id, nome, descricao, data_inicio, data_fim,
                duracao_minima_segundos, sessoes_por_semana)
            VALUES ($1, $2, $3, $4, $5, $6, $7, $8)
            RETURNING id
            """, connection, transaction))
        {
            insert.Parameters.AddWithValue(grupoId);
            insert.Parameters.AddWithValue(usuarioId);
            insert.Parameters.AddWithValue(novo.Nome);
            insert.Parameters.AddWithValue(NpgsqlDbType.Text, (object?)novo.Descricao ?? DBNull.Value);
            insert.Parameters.AddWithValue(novo.DataInicio);
            insert.Parameters.AddWithValue(novo.DataFim);
            insert.Parameters.AddWithValue(novo.DuracaoMinimaSegundos);
            insert.Parameters.AddWithValue(novo.SessoesPorSemana);
            desafioId = (Guid)(await insert.ExecuteScalarAsync(cancellationToken))!;
        }

        await InsertParticipanteAsync(connection, transaction, desafioId, usuarioId, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return (DesafioOperacaoStatus.Concluida, desafioId);
    }

    /// <summary>Participar de novo não muda nada, e a data de entrada continua sendo a primeira.</summary>
    public static async Task<DesafioOperacaoStatus> ParticiparAsync(
        NpgsqlDataSource dataSource,
        Guid desafioId,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var estado = await GetEstadoAsync(connection, null, desafioId, cancellationToken);
        if (estado is null
            || await GrupoAcesso.GetPapelAsync(connection, null, estado.GrupoId, usuarioId, cancellationToken) is null)
        {
            return DesafioOperacaoStatus.DesafioNaoEncontrado;
        }

        if (GetBloqueio(estado.Situacao) is { } bloqueio)
        {
            return bloqueio;
        }

        await InsertParticipanteAsync(connection, null, desafioId, usuarioId, cancellationToken);
        return DesafioOperacaoStatus.Concluida;
    }

    /// <summary>O cancelamento preserva o desafio e os participantes para o histórico.</summary>
    public static async Task<DesafioOperacaoStatus> CancelarAsync(
        NpgsqlDataSource dataSource,
        Guid desafioId,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var encontrado = await GetEstadoAsync(connection, transaction, desafioId, cancellationToken);
        if (encontrado is null)
        {
            return DesafioOperacaoStatus.DesafioNaoEncontrado;
        }

        await GrupoAcesso.LockGrupoAsync(connection, transaction, encontrado.GrupoId, cancellationToken);
        var papel = await GrupoAcesso.GetPapelAsync(connection, transaction, encontrado.GrupoId, usuarioId, cancellationToken);
        if (papel is null)
        {
            return DesafioOperacaoStatus.DesafioNaoEncontrado;
        }

        if (!DesafiosRules.CanGerenciar(papel))
        {
            return DesafioOperacaoStatus.AcessoNegado;
        }

        // Lido de novo depois da trava: outro cancelamento pode ter terminado durante a espera.
        var estado = await GetEstadoAsync(connection, transaction, desafioId, cancellationToken);
        if (GetBloqueio(estado!.Situacao) is { } bloqueio)
        {
            return bloqueio;
        }

        await using var update = new NpgsqlCommand(
            "UPDATE desafios SET cancelled_at = now() WHERE id = $1",
            connection,
            transaction);
        update.Parameters.AddWithValue(desafioId);
        await update.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return DesafioOperacaoStatus.Concluida;
    }

    private sealed record EstadoDesafio(Guid GrupoId, string Situacao);

    private static DesafioOperacaoStatus? GetBloqueio(string situacao) => situacao switch
    {
        SituacaoDesafio.Encerrado => DesafioOperacaoStatus.DesafioEncerrado,
        SituacaoDesafio.Cancelado => DesafioOperacaoStatus.DesafioCancelado,
        _ => null,
    };

    private static async Task<EstadoDesafio?> GetEstadoAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        Guid desafioId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            SELECT d.grupo_id, d.data_inicio, d.data_fim, d.cancelled_at IS NOT NULL,
                (now() AT TIME ZONE g.fuso_horario)::date
            FROM desafios d
            JOIN grupos g ON g.id = d.grupo_id
            WHERE d.id = $1
            """, connection, transaction);
        command.Parameters.AddWithValue(desafioId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new EstadoDesafio(
                reader.GetGuid(0),
                DesafiosRules.GetSituacao(
                    reader.GetFieldValue<DateOnly>(1),
                    reader.GetFieldValue<DateOnly>(2),
                    reader.GetBoolean(3),
                    reader.GetFieldValue<DateOnly>(4)))
            : null;
    }

    private static async Task InsertParticipanteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        Guid desafioId,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await using var insert = new NpgsqlCommand("""
            INSERT INTO participantes_desafio (desafio_id, usuario_id)
            VALUES ($1, $2)
            ON CONFLICT (desafio_id, usuario_id) DO NOTHING
            """, connection, transaction);
        insert.Parameters.AddWithValue(desafioId);
        insert.Parameters.AddWithValue(usuarioId);
        await insert.ExecuteNonQueryAsync(cancellationToken);
    }
}
