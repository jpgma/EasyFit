using Npgsql;
using Wrexercise.Infrastructure.Authentication;

namespace Wrexercise.Features.Autenticacao;

internal static class Sair
{
    public static async Task<IResult> HandleAsync(
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var token = SessaoCookie.Read(context);
        if (!string.IsNullOrEmpty(token))
        {
            await Sessoes.RevokeAsync(dataSource, token, cancellationToken);
        }

        SessaoCookie.Delete(context);
        return Results.NoContent();
    }
}
