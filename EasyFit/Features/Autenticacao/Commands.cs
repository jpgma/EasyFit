using EasyFit.Infrastructure.Authentication;
using Npgsql;

namespace EasyFit.Features.Autenticacao;

internal static class AutenticacaoCommands
{
    private const string UniqueViolation = "23505";
    private const string EmailUniqueIndex = "usuarios_email_unique";

    /// <returns>O usuário e o token da sessão, ou <c>null</c> se o e-mail já estiver cadastrado.</returns>
    public static async Task<(UsuarioResponse Usuario, string Token)?> RegistrarAsync(
        NpgsqlDataSource dataSource,
        string nomeExibicao,
        string email,
        string passwordHash,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        Guid usuarioId;
        try
        {
            await using var insert = new NpgsqlCommand("""
                INSERT INTO usuarios (nome_exibicao, email, password_hash)
                VALUES ($1, $2, $3)
                RETURNING id
                """, connection, transaction);
            insert.Parameters.AddWithValue(nomeExibicao);
            insert.Parameters.AddWithValue(email);
            insert.Parameters.AddWithValue(passwordHash);
            usuarioId = (Guid)(await insert.ExecuteScalarAsync(cancellationToken))!;
        }
        catch (PostgresException exception) when (
            exception.SqlState == UniqueViolation && exception.ConstraintName == EmailUniqueIndex)
        {
            return null;
        }

        var token = await InsertSessaoAsync(connection, transaction, usuarioId, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return (new UsuarioResponse(usuarioId, nomeExibicao, email), token);
    }

    /// <returns>O token da nova sessão.</returns>
    public static async Task<string> CreateSessaoAsync(
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

        var token = await InsertSessaoAsync(connection, transaction, usuarioId, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return token;
    }

    public static async Task RevokeSessaoAsync(
        NpgsqlDataSource dataSource,
        string token,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("""
            UPDATE sessoes_autenticacao
            SET revoked_at = now()
            WHERE token_hash = $1 AND revoked_at IS NULL
            """);
        command.Parameters.AddWithValue(SessaoToken.Hash(token));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<string> InsertSessaoAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        var token = SessaoToken.Generate();

        await using var command = new NpgsqlCommand("""
            INSERT INTO sessoes_autenticacao (usuario_id, token_hash, expires_at)
            VALUES ($1, $2, $3)
            """, connection, transaction);
        command.Parameters.AddWithValue(usuarioId);
        command.Parameters.AddWithValue(SessaoToken.Hash(token));
        command.Parameters.AddWithValue(DateTimeOffset.UtcNow + SessaoPolicy.Lifetime);
        await command.ExecuteNonQueryAsync(cancellationToken);

        return token;
    }
}
