using Npgsql;
using Wrexercise.Features.Desafios.Contracts;
using Wrexercise.Infrastructure.Authentication;

namespace Wrexercise.Features.Progresso;

internal static class ObterHistoricoSemanal
{
    public static async Task<IResult> HandleAsync(
        Guid id,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var usuarioId = context.GetSessaoAtual().UsuarioId;

        var desafio = await ContagensDesafio.LoadDesafioAsync(connection, id, usuarioId, cancellationToken);
        if (desafio is null)
        {
            return OperacaoProgresso.DesafioNaoEncontrado();
        }

        var matriz = await ContagensDesafio.LoadMatrizAsync(connection, id, cancellationToken);
        var semanas = HistoricoSemanalDesafio.MontarHistorico(
            usuarioId,
            matriz,
            desafio.DataInicio,
            desafio.DataFim,
            desafio.SessoesPorSemana,
            desafio.EntradaUsuario);

        return Results.Ok(new HistoricoSemanalResponse(
            desafio.SessoesPorSemana,
            CalculoSemanaDesafio.CountSemanas(desafio.DataInicio, desafio.DataFim),
            semanas));
    }
}
