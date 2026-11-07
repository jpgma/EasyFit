using Wrexercise.Features.Desafios.Contracts;

namespace Wrexercise.Features.Progresso;

internal static class HistoricoSemanalDesafio
{
    public static IReadOnlyList<HistoricoSemanaResponse> MontarHistorico(
        Guid usuarioId,
        Dictionary<(Guid UsuarioId, int IndiceSemana), (int Verificadas, int Pendentes)> matriz,
        DateOnly inicio,
        DateOnly fim,
        int sessoesPorSemana,
        DateOnly entradaUsuario)
    {
        var totalSemanas = CalculoSemanaDesafio.CountSemanas(inicio, fim);
        var semanas = new List<HistoricoSemanaResponse>(totalSemanas);

        for (var indice = 0; indice < totalSemanas; indice++)
        {
            matriz.TryGetValue((usuarioId, indice), out var contagem);
            var progresso = CalculoSemanaDesafio.CalculateProgresso(
                contagem.Verificadas,
                contagem.Pendentes,
                sessoesPorSemana);
            var inicioSemana = inicio.AddDays(indice * 7);
            semanas.Add(new HistoricoSemanaResponse(
                indice + 1,
                inicioSemana,
                inicioSemana.AddDays(6),
                progresso.Verificadas,
                progresso.Provisorias,
                progresso.MetaCumprida,
                CalculoSemanaDesafio.ParticipaDaSemana(inicioSemana, entradaUsuario)));
        }

        return semanas;
    }
}
