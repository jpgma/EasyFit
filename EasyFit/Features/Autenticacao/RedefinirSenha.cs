using EasyFit.Infrastructure.Authentication;
using EasyFit.Infrastructure.Errors;
using Npgsql;

namespace EasyFit.Features.Autenticacao;

internal sealed record RedefinirSenhaRequest(string? Token, string? NovaSenha);

/// <summary>Conclui, pelo link que o operador gerou, a redefinição iniciada por <see cref="ResetSenhaCommand"/>.</summary>
internal static class RedefinirSenha
{
    private const string LoggerCategory = "EasyFit.Autenticacao";
    private const int MaxTokenLength = 128;

    public static async Task<IResult> HandleAsync(
        RedefinirSenhaRequest request,
        NpgsqlDataSource dataSource,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (!ValidacaoConta.IsValidSenha(request.NovaSenha))
        {
            return ApiProblem.Create(
                StatusCodes.Status400BadRequest,
                "DADOS_INVALIDOS",
                "Verifique os dados informados.");
        }

        if (request.Token?.Length is not (> 0 and <= MaxTokenLength))
        {
            return LinkInvalido();
        }

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        Guid redefinicaoId;
        Guid usuarioId;
        // A trava faz duas tentativas simultâneas com o mesmo link terminarem com uma só aceita.
        await using (var select = new NpgsqlCommand("""
            SELECT id, usuario_id
            FROM redefinicoes_senha
            WHERE token_hash = $1 AND used_at IS NULL AND expires_at > now()
            FOR UPDATE
            """, connection, transaction))
        {
            select.Parameters.AddWithValue(OpaqueToken.Hash(request.Token));
            await using var reader = await select.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return LinkInvalido();
            }

            redefinicaoId = reader.GetGuid(0);
            usuarioId = reader.GetGuid(1);
        }

        await using (var usado = new NpgsqlCommand(
            "UPDATE redefinicoes_senha SET used_at = now() WHERE id = $1",
            connection,
            transaction))
        {
            usado.Parameters.AddWithValue(redefinicaoId);
            await usado.ExecuteNonQueryAsync(cancellationToken);
        }

        await Sessoes.SetSenhaAsync(
            connection,
            transaction,
            usuarioId,
            Senhas.Hash(request.NovaSenha!),
            cancellationToken);
        await Sessoes.RevokeDoUsuarioAsync(connection, transaction, usuarioId, null, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        loggerFactory.CreateLogger(LoggerCategory).LogInformation("Usuário {UsuarioId} redefiniu a senha.", usuarioId);
        return Results.NoContent();
    }

    private static IResult LinkInvalido() => ApiProblem.Create(
        StatusCodes.Status400BadRequest,
        "REDEFINICAO_INVALIDA",
        "Este link não é válido. Peça um novo a quem administra o EasyFit.");
}
