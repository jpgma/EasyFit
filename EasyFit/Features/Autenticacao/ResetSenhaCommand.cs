using EasyFit.Infrastructure.Authentication;
using Npgsql;

namespace EasyFit.Features.Autenticacao;

/// <summary>
/// Subcomando "reset-senha": o operador gera um link de uso único para quem perdeu a senha.
/// Encerra as sessões da conta e os links anteriores. O link vai só para o terminal, nunca para o log.
/// </summary>
internal static class ResetSenhaCommand
{
    private static readonly TimeSpan Validade = TimeSpan.FromHours(1);

    public static async Task<int> RunAsync(IServiceProvider services, string email)
    {
        var dataSource = services.GetRequiredService<NpgsqlDataSource>();
        var token = await CriarTokenAsync(dataSource, email, CancellationToken.None);
        if (token is null)
        {
            await Console.Error.WriteLineAsync("Nenhuma conta com este e-mail.");
            return 1;
        }

        Console.WriteLine("Envie este caminho à pessoa, junto com o endereço do EasyFit. Vale por 1 hora e uma única vez:");
        Console.WriteLine($"/redefinir-senha#{token}");
        return 0;
    }

    /// <returns>O token, ou <c>null</c> se o e-mail não tiver conta.</returns>
    public static async Task<string?> CriarTokenAsync(
        NpgsqlDataSource dataSource,
        string email,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        Guid usuarioId;
        await using (var select = new NpgsqlCommand(
            "SELECT id FROM usuarios WHERE lower(email) = lower($1)",
            connection,
            transaction))
        {
            select.Parameters.AddWithValue(ValidacaoConta.NormalizeEmail(email));
            if (await select.ExecuteScalarAsync(cancellationToken) is not Guid encontrado)
            {
                return null;
            }

            usuarioId = encontrado;
        }

        await using (var anteriores = new NpgsqlCommand(
            "UPDATE redefinicoes_senha SET used_at = now() WHERE usuario_id = $1 AND used_at IS NULL",
            connection,
            transaction))
        {
            anteriores.Parameters.AddWithValue(usuarioId);
            await anteriores.ExecuteNonQueryAsync(cancellationToken);
        }

        await Sessoes.RevokeDoUsuarioAsync(connection, transaction, usuarioId, null, cancellationToken);

        var token = OpaqueToken.Generate();
        await using (var insert = new NpgsqlCommand("""
            INSERT INTO redefinicoes_senha (usuario_id, token_hash, expires_at)
            VALUES ($1, $2, $3)
            """, connection, transaction))
        {
            insert.Parameters.AddWithValue(usuarioId);
            insert.Parameters.AddWithValue(OpaqueToken.Hash(token));
            insert.Parameters.AddWithValue(DateTimeOffset.UtcNow + Validade);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return token;
    }
}
