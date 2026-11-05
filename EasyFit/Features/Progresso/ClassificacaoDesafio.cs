using EasyFit.Features.Desafios.Contracts;

namespace EasyFit.Features.Progresso;

internal sealed record EstatisticasParticipante(
    Guid UsuarioId,
    string NomeExibicao,
    int SemanasCumpridas,
    int SessoesVerificadas,
    int SequenciaSemanas);

internal static class ClassificacaoDesafio
{
    public static IReadOnlyList<EstatisticasParticipante> CalcularRanking(
        IReadOnlyList<(Guid UsuarioId, string NomeExibicao)> participantes,
        Dictionary<(Guid UsuarioId, int IndiceSemana), (int Verificadas, int Pendentes)> matriz,
        DateOnly inicio,
        DateOnly fim,
        DateOnly hoje,
        int sessoesPorSemana)
    {
        var totalSemanas = CalculoSemanaDesafio.CountSemanas(inicio, fim);
        var indiceRecente = GetIndiceSemanaMaisRecente(inicio, fim, hoje, totalSemanas);

        var stats = participantes.Select(p =>
        {
            var verificadasTotal = 0;
            var semanasCumpridas = 0;
            var porSemana = new bool[totalSemanas];

            for (var indice = 0; indice < totalSemanas; indice++)
            {
                matriz.TryGetValue((p.UsuarioId, indice), out var contagem);
                verificadasTotal += contagem.Verificadas;
                var progresso = CalculoSemanaDesafio.CalculateProgresso(
                    contagem.Verificadas,
                    contagem.Pendentes,
                    sessoesPorSemana);
                porSemana[indice] = progresso.MetaCumprida;
                if (progresso.MetaCumprida)
                {
                    semanasCumpridas++;
                }
            }

            var sequencia = 0;
            for (var indice = indiceRecente; indice >= 0; indice--)
            {
                if (!porSemana[indice])
                {
                    break;
                }

                sequencia++;
            }

            return new EstatisticasParticipante(
                p.UsuarioId,
                p.NomeExibicao,
                semanasCumpridas,
                verificadasTotal,
                sequencia);
        }).ToList();

        return stats
            .OrderByDescending(s => s.SemanasCumpridas)
            .ThenByDescending(s => s.SessoesVerificadas)
            .ThenByDescending(s => s.SequenciaSemanas)
            .ThenBy(s => s.NomeExibicao, StringComparer.Ordinal)
            .ThenBy(s => s.UsuarioId)
            .ToList();
    }

    private static int GetIndiceSemanaMaisRecente(
        DateOnly inicio,
        DateOnly fim,
        DateOnly hoje,
        int totalSemanas)
    {
        if (hoje < inicio)
        {
            return -1;
        }

        var dia = hoje > fim ? fim : hoje;
        return CalculoSemanaDesafio.GetIndiceSemana(inicio, dia);
    }
}
