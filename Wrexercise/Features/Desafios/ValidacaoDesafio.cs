using Wrexercise.Features.Grupos.Contracts;

namespace Wrexercise.Features.Desafios;

internal static class ValidacaoDesafio
{
    private const int MaxNomeLength = 80;
    private const int MaxDescricaoLength = 500;
    private const int MinDuracaoSegundos = 60;
    private const int MaxDuracaoSegundos = 24 * 60 * 60;
    private const int MaxSessoesPorSemana = 7;
    private const int MaxSemanas = 52;

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
        && ProgressoDesafio.CountSemanas(inicio, fim) <= MaxSemanas;

    public static bool CanGerenciar(string papel) =>
        papel is PapelMembro.Proprietario or PapelMembro.Administrador;
}
