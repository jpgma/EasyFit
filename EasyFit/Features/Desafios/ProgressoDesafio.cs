namespace EasyFit.Features.Desafios;

internal sealed record ProgressoSemanal(int Verificadas, int Provisorias, bool MetaCumprida);

internal static class ProgressoDesafio
{
    private const int DiasPorSemana = 7;

    public static int CountSemanas(DateOnly inicio, DateOnly fim) =>
        (fim.DayNumber - inicio.DayNumber + 1) / DiasPorSemana;

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

    /// <returns>A semana que contém <paramref name="hoje"/>, ou <c>null</c> fora do período.</returns>
    public static SemanaResponse? GetSemana(DateOnly inicio, DateOnly fim, DateOnly hoje)
    {
        if (hoje < inicio || hoje > fim)
        {
            return null;
        }

        var semanasCompletas = (hoje.DayNumber - inicio.DayNumber) / DiasPorSemana;
        var inicioSemana = inicio.AddDays(semanasCompletas * DiasPorSemana);
        return new SemanaResponse(semanasCompletas + 1, inicioSemana, inicioSemana.AddDays(DiasPorSemana - 1));
    }

    /// <summary>Só as verificadas cumprem a meta. As pendentes aparecem apenas no progresso provisório.</summary>
    public static ProgressoSemanal CalculateProgresso(int verificadas, int pendentes, int sessoesPorSemana) =>
        new(verificadas, verificadas + pendentes, verificadas >= sessoesPorSemana);
}
