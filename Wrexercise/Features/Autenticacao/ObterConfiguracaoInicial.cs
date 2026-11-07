using Npgsql;
using Wrexercise.Infrastructure.Json;

namespace Wrexercise.Features.Autenticacao;

internal sealed record ConfiguracaoInicialResponse(bool Pendente);

internal static class ObterConfiguracaoInicial
{
    /// <summary>
    /// Diz se o app ainda não tem nenhum usuário. É público porque a tela de configuração inicial
    /// roda antes de existir qualquer sessão, e a resposta é só um booleano.
    /// </summary>
    public static async Task<IResult> HandleAsync(
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("SELECT NOT EXISTS (SELECT 1 FROM usuarios)");
        var pendente = (bool)(await command.ExecuteScalarAsync(cancellationToken))!;

        return Results.Json(
            new ConfiguracaoInicialResponse(pendente),
            AppJsonContext.Default.ConfiguracaoInicialResponse);
    }
}
