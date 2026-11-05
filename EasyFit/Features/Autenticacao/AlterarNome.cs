using EasyFit.Infrastructure.Authentication;
using EasyFit.Infrastructure.Errors;
using EasyFit.Infrastructure.Json;
using Npgsql;

namespace EasyFit.Features.Autenticacao;

internal sealed record AlterarNomeRequest(string? NomeExibicao);

internal static class AlterarNome
{
    public static async Task<IResult> HandleAsync(
        AlterarNomeRequest request,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        if (!ValidacaoConta.IsValidNomeExibicao(request.NomeExibicao))
        {
            return ApiProblem.Create(
                StatusCodes.Status400BadRequest,
                "DADOS_INVALIDOS",
                "Verifique os dados informados.");
        }

        var usuarioId = context.GetSessaoAtual().UsuarioId;
        await using (var update = dataSource.CreateCommand("UPDATE usuarios SET nome_exibicao = $2 WHERE id = $1"))
        {
            update.Parameters.AddWithValue(usuarioId);
            update.Parameters.AddWithValue(ValidacaoConta.NormalizeNomeExibicao(request.NomeExibicao!));
            await update.ExecuteNonQueryAsync(cancellationToken);
        }

        var usuario = await Sessoes.GetUsuarioAsync(dataSource, usuarioId, cancellationToken);
        return usuario is null
            ? SessaoFilter.NotAuthenticated()
            : Results.Json(new SessaoResponse(usuario), AppJsonContext.Default.SessaoResponse);
    }
}
