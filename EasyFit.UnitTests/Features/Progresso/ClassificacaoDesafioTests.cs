using EasyFit.Features.Progresso;

namespace EasyFit.UnitTests.Features.Progresso;

public class ClassificacaoDesafioTests
{
    private static readonly DateOnly Inicio = new(2026, 3, 2);
    private static readonly DateOnly Fim = Inicio.AddDays(27);
    private const int Meta = 4;

    [Fact]
    public void CalcularRanking_PriorizaSemanasCumpridasDepoisSessoesDepoisSequencia()
    {
        var ana = Guid.NewGuid();
        var bob = Guid.NewGuid();
        var participantes = new List<(Guid, string)>
        {
            (ana, "Ana"),
            (bob, "Bob"),
        };

        var matriz = new Dictionary<(Guid, int), (int, int)>
        {
            [(ana, 0)] = (4, 0),
            [(ana, 1)] = (4, 0),
            [(bob, 0)] = (8, 0),
            [(bob, 1)] = (0, 0),
        };

        var ranking = ClassificacaoDesafio.CalcularRanking(
            participantes,
            matriz,
            Inicio,
            Fim,
            Inicio.AddDays(14),
            Meta);

        Assert.Equal(["Ana", "Bob"], ranking.Select(r => r.NomeExibicao));
        Assert.Equal(2, ranking[0].SemanasCumpridas);
        Assert.Equal(1, ranking[1].SemanasCumpridas);
    }

    [Fact]
    public void CalcularRanking_SequenciaContaDaSemanaMaisRecenteParaTras()
    {
        var ana = Guid.NewGuid();
        var participantes = new List<(Guid, string)> { (ana, "Ana") };
        var matriz = new Dictionary<(Guid, int), (int, int)>
        {
            [(ana, 0)] = (4, 0),
            [(ana, 1)] = (0, 0),
            [(ana, 2)] = (4, 0),
            [(ana, 3)] = (4, 0),
        };

        var ranking = ClassificacaoDesafio.CalcularRanking(
            participantes,
            matriz,
            Inicio,
            Fim,
            Inicio.AddDays(27),
            Meta);

        Assert.Equal(3, Assert.Single(ranking).SemanasCumpridas);
        Assert.Equal(2, Assert.Single(ranking).SequenciaSemanas);
    }

    [Fact]
    public void CalcularRanking_DesempataPorNome()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var participantes = new List<(Guid, string)>
        {
            (b, "Zeca"),
            (a, "Ana"),
        };

        var ranking = ClassificacaoDesafio.CalcularRanking(
            participantes,
            [],
            Inicio,
            Fim,
            Inicio.AddDays(7),
            Meta);

        Assert.Equal(["Ana", "Zeca"], ranking.Select(r => r.NomeExibicao));
    }
}
