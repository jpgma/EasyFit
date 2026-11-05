using EasyFit.Features.Desafios.Contracts;

namespace EasyFit.Features.Desafios;

internal sealed record ProgressoSemanal(int Verificadas, int Provisorias, bool MetaCumprida);

internal static class ProgressoDesafio
{
    public static int CountSemanas(DateOnly inicio, DateOnly fim) =>
        CalculoSemanaDesafio.CountSemanas(inicio, fim);

    public static string GetSituacao(DateOnly inicio, DateOnly fim, bool cancelado, DateOnly hoje)
    {
        if (cancelado)
        {
            return SituacaoDesafio.Cancelado;
        }

        if (hoje < inicio)
        {
            return SituacaoDesafio.Agendado;
        }

        return hoje > fim ? SituacaoDesafio.Encerrado : SituacaoDesafio.Ativo;
    }

    public static SemanaResponse? GetSemana(DateOnly inicio, DateOnly fim, DateOnly hoje)
    {
        var semana = CalculoSemanaDesafio.GetSemana(inicio, fim, hoje);
        return semana is null ? null : new SemanaResponse(semana.Numero, semana.Inicio, semana.Fim);
    }

    public static ProgressoSemanal CalculateProgresso(int verificadas, int pendentes, int sessoesPorSemana)
    {
        var calculado = CalculoSemanaDesafio.CalculateProgresso(verificadas, pendentes, sessoesPorSemana);
        return new ProgressoSemanal(calculado.Verificadas, calculado.Provisorias, calculado.MetaCumprida);
    }
}
