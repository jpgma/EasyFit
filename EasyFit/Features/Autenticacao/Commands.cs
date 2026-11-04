using EasyFit.Features.Convites.Contracts;
using EasyFit.Infrastructure.Authentication;
using Npgsql;

namespace EasyFit.Features.Autenticacao;

internal enum RegistroStatus
{
    Criado,
    EmailJaCadastrado,
    ConviteInvalido,
    GrupoCheio,
}

internal readonly record struct RegistroResult(
    RegistroStatus Status,
    UsuarioResponse? Usuario = null,
    string? Token = null);

internal static class AutenticacaoCommands
{
    private const string UniqueViolation = "23505";
    private const string EmailUniqueIndex = "usuarios_email_unique";

    /// <summary>
    /// Conta, sessão e entrada no grupo acontecem na mesma transação: um convite recusado
    /// não deixa conta para trás, e um convite nunca cria mais de uma conta.
    /// </summary>
    public static async Task<RegistroResult> RegistrarAsync(
        NpgsqlDataSource dataSource,
        string nomeExibicao,
        string email,
        string passwordHash,
        string? conviteToken,
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
            return new RegistroResult(RegistroStatus.EmailJaCadastrado);
        }

        if (conviteToken is not null)
        {
            var convite = await ConviteAceite.AceitarAsync(
                connection,
                transaction,
                conviteToken,
                usuarioId,
                cancellationToken);

            // Uma conta recém-criada não pode já ser membro, então só restam estes dois motivos de recusa.
            if (convite.Status != ConviteAceiteStatus.Aceito)
            {
                return new RegistroResult(convite.Status == ConviteAceiteStatus.GrupoCheio
                    ? RegistroStatus.GrupoCheio
                    : RegistroStatus.ConviteInvalido);
            }
        }

        var token = await InsertSessaoAsync(connection, transaction, usuarioId, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return new RegistroResult(RegistroStatus.Criado, new UsuarioResponse(usuarioId, nomeExibicao, email), token);
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
        command.Parameters.AddWithValue(OpaqueToken.Hash(token));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<string> InsertSessaoAsync(
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
}
