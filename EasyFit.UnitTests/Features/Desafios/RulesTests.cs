using EasyFit.Features.Desafios;
using EasyFit.Features.Grupos.Contracts;

namespace EasyFit.UnitTests.Features.Desafios;

public class RulesTests
{
    // 2026-11-02 é uma segunda-feira; 2026-11-29, um domingo.
    private static readonly DateOnly Segunda = new(2026, 11, 2);
    private static readonly DateOnly Domingo = new(2026, 11, 29);

    [Theory]
    [InlineData("Consistência", true)]
    [InlineData("  Consistência  ", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void IsValidNome_RequiresText(string? nome, bool expected)
    {
        Assert.Equal(expected, DesafiosRules.IsValidNome(nome));
    }

    [Fact]
    public void IsValidNome_LimitsTheLength_AfterTrimming()
    {
        Assert.True(DesafiosRules.IsValidNome($"  {new string('a', 80)}  "));
        Assert.False(DesafiosRules.IsValidNome(new string('a', 81)));
    }

    [Fact]
    public void Descricao_IsOptional_AndLimited()
    {
        Assert.True(DesafiosRules.IsValidDescricao(null));
        Assert.True(DesafiosRules.IsValidDescricao(new string('a', 500)));
        Assert.False(DesafiosRules.IsValidDescricao(new string('a', 501)));
        Assert.Null(DesafiosRules.NormalizeDescricao("   "));
        Assert.Equal("Bora", DesafiosRules.NormalizeDescricao("  Bora  "));
    }

    [Theory]
    [InlineData(60, true)]
    [InlineData(3600, true)]
    [InlineData(86400, true)]
    [InlineData(59, false)]
    [InlineData(86401, false)]
    [InlineData(0, false)]
    [InlineData(-3600, false)]
    [InlineData(null, false)]
    public void IsValidDuracaoMinima_RequiresAReasonableSession(int? segundos, bool expected)
    {
        Assert.Equal(expected, DesafiosRules.IsValidDuracaoMinima(segundos));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(7, true)]
    [InlineData(0, false)]
    [InlineData(8, false)]
    [InlineData(null, false)]
    public void IsValidSessoesPorSemana_AllowsOneToSeven(int? sessoes, bool expected)
    {
        Assert.Equal(expected, DesafiosRules.IsValidSessoesPorSemana(sessoes));
    }

    [Fact]
    public void IsValidPeriodo_AcceptsWholeWeeks_StartingTodayOrLater()
    {
        Assert.True(DesafiosRules.IsValidPeriodo(Segunda, Domingo, hoje: Segunda));
        Assert.True(DesafiosRules.IsValidPeriodo(Segunda, Segunda.AddDays(6), hoje: Segunda.AddDays(-1)));
    }

    [Fact]
    public void IsValidPeriodo_RejectsPartialWeeks()
    {
        var hoje = Segunda.AddDays(-30);

        Assert.False(DesafiosRules.IsValidPeriodo(Segunda.AddDays(1), Domingo, hoje));
        Assert.False(DesafiosRules.IsValidPeriodo(Segunda, Domingo.AddDays(-1), hoje));
    }

    [Fact]
    public void IsValidPeriodo_RejectsAnEndBeforeTheStart()
    {
        Assert.False(DesafiosRules.IsValidPeriodo(Segunda, Segunda.AddDays(-1), Segunda.AddDays(-30)));
    }

    [Fact]
    public void IsValidPeriodo_RejectsAStartInThePast()
    {
        Assert.False(DesafiosRules.IsValidPeriodo(Segunda, Domingo, hoje: Segunda.AddDays(1)));
    }

    [Fact]
    public void IsValidPeriodo_LimitsTheNumberOfWeeks()
    {
        Assert.True(DesafiosRules.IsValidPeriodo(Segunda, Segunda.AddDays((52 * 7) - 1), Segunda));
        Assert.False(DesafiosRules.IsValidPeriodo(Segunda, Segunda.AddDays((53 * 7) - 1), Segunda));
    }

    [Fact]
    public void CountSemanas_CountsWholeWeeks()
    {
        Assert.Equal(1, DesafiosRules.CountSemanas(Segunda, Segunda.AddDays(6)));
        Assert.Equal(4, DesafiosRules.CountSemanas(Segunda, Domingo));
    }

    [Theory]
    [InlineData(PapelMembro.Proprietario, true)]
    [InlineData(PapelMembro.Administrador, true)]
    [InlineData(PapelMembro.Membro, false)]
    public void CanGerenciar_AllowsProprietarioAndAdministrador(string papel, bool expected)
    {
        Assert.Equal(expected, DesafiosRules.CanGerenciar(papel));
    }

    [Theory]
    [InlineData(-1, SituacaoDesafio.Agendado)]
    [InlineData(0, SituacaoDesafio.Ativo)]
    [InlineData(27, SituacaoDesafio.Ativo)]
    [InlineData(28, SituacaoDesafio.Encerrado)]
    public void GetSituacao_FollowsTheCalendar_IncludingBothEnds(int diasDesdeOInicio, string expected)
    {
        var hoje = Segunda.AddDays(diasDesdeOInicio);

        Assert.Equal(expected, DesafiosRules.GetSituacao(Segunda, Domingo, cancelado: false, hoje));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(28)]
    public void GetSituacao_CanceladoWinsOverTheCalendar(int diasDesdeOInicio)
    {
        var hoje = Segunda.AddDays(diasDesdeOInicio);

        Assert.Equal(SituacaoDesafio.Cancelado, DesafiosRules.GetSituacao(Segunda, Domingo, cancelado: true, hoje));
    }

    [Theory]
    [InlineData(0, 1, 0)]
    [InlineData(6, 1, 0)]
    [InlineData(7, 2, 7)]
    [InlineData(27, 4, 21)]
    public void GetSemana_ReturnsTheWeekContainingToday(int diasDesdeOInicio, int numero, int diasAteOInicioDaSemana)
    {
        var semana = DesafiosRules.GetSemana(Segunda, Domingo, Segunda.AddDays(diasDesdeOInicio));

        var esperado = Segunda.AddDays(diasAteOInicioDaSemana);
        Assert.Equal(new SemanaResponse(numero, esperado, esperado.AddDays(6)), semana);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(28)]
    public void GetSemana_IsEmptyOutsideThePeriodo(int diasDesdeOInicio)
    {
        Assert.Null(DesafiosRules.GetSemana(Segunda, Domingo, Segunda.AddDays(diasDesdeOInicio)));
    }

    [Fact]
    public void CalculateProgresso_MatchesTheExampleInTheArchitecture()
    {
        // arquitetura.md §8.5: meta de 4, com 2 verificadas e 1 pendente; a rejeitada não entra.
        Assert.Equal(new ProgressoSemanal(2, 3, false), DesafiosRules.CalculateProgresso(2, 1, 4));
    }

    [Fact]
    public void CalculateProgresso_OnlyVerificadasMeetTheMeta()
    {
        Assert.False(DesafiosRules.CalculateProgresso(3, 5, 4).MetaCumprida);
        Assert.True(DesafiosRules.CalculateProgresso(4, 0, 4).MetaCumprida);
        Assert.True(DesafiosRules.CalculateProgresso(6, 0, 4).MetaCumprida);
    }
}
