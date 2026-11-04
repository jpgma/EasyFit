using EasyFit.Infrastructure.Authentication;
using Npgsql;

namespace EasyFit.Features.Autenticacao;

internal static class Sessoes
{
    /// <returns>O token da nova sessão.</returns>
    public static async Task<string> CreateAsync(
        NpgsqlDataSource dataSource,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // A limpeza acontece aqui para dispensar uma tarefa agendada.
        await using (var cleanup = new NpgsqlCommand(
            "DELETE FROM sessoes_autenticacao WHERE usuario_id = $1 AND expires_at <= now()",
            connection,
            transaction))
        {
            cleanup.Parameters.AddWithValue(usuarioId);
            await cleanup.ExecuteNonQueryAsync(cancellationToken);
        }

        var token = await InsertAsync(connection, transaction, usuarioId, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return token;
    }

    public static async Task RevokeAsync(
        NpgsqlDataSource dataSource,
        string token,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("""
            UPDATE sessoes_autenticacao
            SET revoked_at = now()
            WHERE token_hash = $1 AND revoked_at IS NULL
            """);
        command.Parameters.AddWithValue(OpaqueToken.Hash(token));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public static async Task<string> InsertAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        var token = OpaqueToken.Generate();

        await using var command = new NpgsqlCommand("""
            INSERT INTO sessoes_autenticacao (usuario_id, token_hash, expires_at)
            VALUES ($1, $2, $3)
            """, connection, transaction);
        command.Parameters.AddWithValue(usuarioId);
        command.Parameters.AddWithValue(OpaqueToken.Hash(token));
        command.Parameters.AddWithValue(DateTimeOffset.UtcNow + SessaoPolicy.Lifetime);
        await command.ExecuteNonQueryAsync(cancellationToken);

        return token;
    }

    public static async Task<UsuarioResponse?> GetUsuarioAsync(
        NpgsqlDataSource dataSource,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            "SELECT id, nome_exibicao, email FROM usuarios WHERE id = $1");
        command.Parameters.AddWithValue(usuarioId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new UsuarioResponse(reader.GetGuid(0), reader.GetString(1), reader.GetString(2))
            : null;
    }
}
