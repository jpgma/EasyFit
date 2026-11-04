using EasyFit.Infrastructure.Authentication;
using Npgsql;

namespace EasyFit.Features.Desafios;

internal static class ObterDesafio
{
    public static async Task<IResult> HandleAsync(
        Guid id,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var desafio = await ConsultasDesafio.GetAsync(
            dataSource,
            id,
            context.GetSessaoAtual().UsuarioId,
            cancellationToken);

        return desafio is null ? OperacaoDesafio.DesafioNaoEncontrado() : Results.Ok(desafio);
    }
}
