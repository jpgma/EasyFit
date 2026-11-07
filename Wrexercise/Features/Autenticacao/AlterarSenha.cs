using Npgsql;
using Wrexercise.Infrastructure.Authentication;
using Wrexercise.Infrastructure.Errors;

namespace Wrexercise.Features.Autenticacao;

internal sealed record AlterarSenhaRequest(string? SenhaAtual, string? NovaSenha);

internal static class AlterarSenha
{
    private const string LoggerCategory = "Wrexercise.Autenticacao";

    public static async Task<IResult> HandleAsync(
        AlterarSenhaRequest request,
        HttpContext context,
        NpgsqlDataSource dataSource,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.SenhaAtual)
            || request.SenhaAtual.Length > ValidacaoConta.MaxSenhaLength
            || !ValidacaoConta.IsValidSenha(request.NovaSenha))
        {
            return ApiProblem.Create(
                StatusCodes.Status400BadRequest,
                "DADOS_INVALIDOS",
                "Verifique os dados informados.");
        }

        var usuarioId = context.GetSessaoAtual().UsuarioId;

        string passwordHash;
        await using (var select = dataSource.CreateCommand("SELECT password_hash FROM usuarios WHERE id = $1"))
        {
            select.Parameters.AddWithValue(usuarioId);
            passwordHash = (string)(await select.ExecuteScalarAsync(cancellationToken))!;
        }

        // 400, não 401: a sessão continua válida, e a interface não deve tratar isso como saída da conta.
        if (!Senhas.Verify(request.SenhaAtual, passwordHash))
        {
            return ApiProblem.Create(
                StatusCodes.Status400BadRequest,
                "SENHA_ATUAL_INCORRETA",
                "A senha atual está incorreta.");
        }

        var novoHash = Senhas.Hash(request.NovaSenha!);
        var sessaoAtual = OpaqueToken.Hash(SessaoCookie.Read(context)!);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await Sessoes.SetSenhaAsync(connection, transaction, usuarioId, novoHash, cancellationToken);
        await Sessoes.RevokeDoUsuarioAsync(connection, transaction, usuarioId, sessaoAtual, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        loggerFactory.CreateLogger(LoggerCategory).LogInformation("Usuário {UsuarioId} alterou a senha.", usuarioId);
        return Results.NoContent();
    }
}
