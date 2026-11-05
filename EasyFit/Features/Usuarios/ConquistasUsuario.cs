namespace EasyFit.Features.Usuarios;

internal readonly record struct EstatisticasConquistas(int TotalTreinos, int MelhorSequencia, int TreinosAntesDas7);

/// <summary>Conquistas são só reconhecimento: calculadas na leitura, sem efeito na classificação.</summary>
internal static class ConquistasUsuario
{
    private static readonly int[] MarcosSequencia = [12, 8, 4];
    private static readonly int[] MarcosTreinos = [100, 50, 10];
    private const int TreinosMadrugador = 10;

    public static IReadOnlyList<ConquistaResponse> Calcular(EstatisticasConquistas estatisticas)
    {
        var conquistas = new List<ConquistaResponse>();

        foreach (var marco in MarcosSequencia.Where(marco => estatisticas.MelhorSequencia >= marco))
        {
            conquistas.Add(new ConquistaResponse(
                $"Sequencia{marco}",
                $"{marco} semanas seguidas",
                "Meta batida sem falhar em um desafio"));
        }

        foreach (var marco in MarcosTreinos.Where(marco => estatisticas.TotalTreinos >= marco))
        {
            conquistas.Add(new ConquistaResponse(
                $"Treinos{marco}",
                $"{marco} treinos",
                "Treinos verificados pelos amigos"));
        }

        if (estatisticas.TreinosAntesDas7 >= TreinosMadrugador)
        {
            conquistas.Add(new ConquistaResponse(
                "Madrugador",
                "Madrugador",
                $"{TreinosMadrugador} treinos verificados antes das 7h"));
        }

        return conquistas;
    }
}
