using EasyFit.Infrastructure.Authentication;
using EasyFit.Infrastructure.Json;
using Npgsql;

namespace EasyFit.Features.Atividades;

internal static class ObterAtividade
{
    public static async Task<IResult> HandleAsync(
        Guid id,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var atividade = await ConsultasAtividade.GetDetalheAsync(
            dataSource,
            id,
            context.GetSessaoAtual().UsuarioId,
            cancellationToken);

        return atividade is null
            ? OperacaoAtividade.AtividadeNaoEncontrada()
            : Results.Json(atividade, AppJsonContext.Default.AtividadeResponse);
    }
}
