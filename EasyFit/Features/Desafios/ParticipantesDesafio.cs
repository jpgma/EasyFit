using Npgsql;

namespace EasyFit.Features.Desafios;

internal sealed record EstadoDesafio(Guid GrupoId, string Situacao);

internal static class ParticipantesDesafio
{
    public static DesafioOperacaoStatus? GetBloqueio(string situacao) => situacao switch
    {
        SituacaoDesafio.Encerrado => DesafioOperacaoStatus.DesafioEncerrado,
        SituacaoDesafio.Cancelado => DesafioOperacaoStatus.DesafioCancelado,
        _ => null,
    };

    public static async Task<EstadoDesafio?> GetEstadoAsync(
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
                ProgressoDesafio.GetSituacao(
                    reader.GetFieldValue<DateOnly>(1),
                    reader.GetFieldValue<DateOnly>(2),
                    reader.GetBoolean(3),
                    reader.GetFieldValue<DateOnly>(4)))
            : null;
    }

    public static async Task InsertAsync(
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
