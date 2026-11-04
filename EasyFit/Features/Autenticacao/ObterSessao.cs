using EasyFit.Infrastructure.Authentication;
using Npgsql;

namespace EasyFit.Features.Autenticacao;

internal static class ObterSessao
{
    public static async Task<IResult> HandleAsync(
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var usuario = await Sessoes.GetUsuarioAsync(
            dataSource,
            context.GetSessaoAtual().UsuarioId,
            cancellationToken);

        // A chave estrangeira apaga as sessões junto com o usuário; só uma corrida chega aqui.
        return usuario is null
            ? SessaoFilter.NotAuthenticated()
            : Results.Ok(new SessaoResponse(usuario));
    }
}
