using Npgsql;

namespace EasyFit.Features.Autenticacao;

internal sealed record UsuarioCredenciais(Guid Id, string NomeExibicao, string Email, string PasswordHash);

internal static class AutenticacaoQueries
{
    public static async Task<UsuarioCredenciais?> GetCredenciaisByEmailAsync(
        NpgsqlDataSource dataSource,
        string email,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("""
            SELECT id, nome_exibicao, email, password_hash
            FROM usuarios
            WHERE lower(email) = lower($1)
            """);
        command.Parameters.AddWithValue(email);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new UsuarioCredenciais(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3))
            : null;
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
