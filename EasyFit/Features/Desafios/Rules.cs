using EasyFit.Features.Grupos.Contracts;

namespace EasyFit.Features.Desafios;

internal static class SituacaoDesafio
{
    public const string Agendado = "Agendado";
    public const string Ativo = "Ativo";
    public const string Encerrado = "Encerrado";
    public const string Cancelado = "Cancelado";
}

internal sealed record ProgressoSemanal(int Verificadas, int Provisorias, bool MetaCumprida);

internal static class DesafiosRules
{
    private const int MaxNomeLength = 80;
    private const int MaxDescricaoLength = 500;
    private const int MinDuracaoSegundos = 60;
    private const int MaxDuracaoSegundos = 24 * 60 * 60;
    private const int MaxSessoesPorSemana = 7;
    private const int MaxSemanas = 52;
    private const int DiasPorSemana = 7;

    public static string NormalizeNome(string nome) => nome.Trim();

    public static bool IsValidNome(string? nome)
    {
        var length = nome?.Trim().Length ?? 0;
        return length is > 0 and <= MaxNomeLength;
    }

    /// <summary>A descrição é opcional: só espaços equivalem a não ter descrição.</summary>
    public static string? NormalizeDescricao(string? descricao) =>
        string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();

    public static bool IsValidDescricao(string? descricao) =>
        (descricao?.Trim().Length ?? 0) <= MaxDescricaoLength;

    public static bool IsValidDuracaoMinima(int? segundos) =>
        segundos is >= MinDuracaoSegundos and <= MaxDuracaoSegundos;

    public static bool IsValidSessoesPorSemana(int? sessoes) =>
        sessoes is > 0 and <= MaxSessoesPorSemana;

    /// <summary>
    /// Só semanas inteiras, de segunda a domingo, e sem começar no passado: uma semana parcial
    /// não poderia cobrar a meta inteira, e um início retroativo mudaria semanas já vividas.
    /// </summary>
    public static bool IsValidPeriodo(DateOnly inicio, DateOnly fim, DateOnly hoje) =>
        inicio.DayOfWeek == DayOfWeek.Monday
        && fim.DayOfWeek == DayOfWeek.Sunday
        && fim > inicio
        && inicio >= hoje
        && CountSemanas(inicio, fim) <= MaxSemanas;

    public static int CountSemanas(DateOnly inicio, DateOnly fim) =>
        (fim.DayNumber - inicio.DayNumber + 1) / DiasPorSemana;

    public static bool CanGerenciar(string papel) =>
        papel is PapelMembro.Proprietario or PapelMembro.Administrador;

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
