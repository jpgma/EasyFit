using EasyFit.Features.Autenticacao;

namespace EasyFit.UnitTests.Features.Autenticacao;

public class RulesTests
{
    [Fact]
    public void NormalizeEmail_TrimsAndLowercases()
    {
        Assert.Equal("ana@exemplo.com", AutenticacaoRules.NormalizeEmail("  Ana@Exemplo.COM "));
    }

    [Theory]
    [InlineData("ana@exemplo.com")]
    [InlineData(" ana@exemplo.com ")]
    [InlineData("a@b")]
    public void IsValidEmail_AcceptsPlausibleAddresses(string email)
    {
        Assert.True(AutenticacaoRules.IsValidEmail(email));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sem-arroba")]
    [InlineData("@exemplo.com")]
    [InlineData("ana@")]
    [InlineData("ana@@exemplo.com")]
    [InlineData("a@b@c")]
    [InlineData("ana silva@exemplo.com")]
    public void IsValidEmail_RejectsInvalidAddresses(string? email)
    {
        Assert.False(AutenticacaoRules.IsValidEmail(email));
    }

    [Fact]
    public void IsValidEmail_RejectsTooLongAddress()
    {
        Assert.False(AutenticacaoRules.IsValidEmail(new string('a', 250) + "@b.co"));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("Ana", true)]
    public void IsValidNomeExibicao_RequiresVisibleText(string? nome, bool expected)
    {
        Assert.Equal(expected, AutenticacaoRules.IsValidNomeExibicao(nome));
    }

    [Theory]
    [InlineData(80, true)]
    [InlineData(81, false)]
    public void IsValidNomeExibicao_LimitsLength(int length, bool expected)
    {
        Assert.Equal(expected, AutenticacaoRules.IsValidNomeExibicao(new string('a', length)));
    }

    [Theory]
    [InlineData(7, false)]
    [InlineData(8, true)]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void IsValidSenha_EnforcesLengthLimits(int length, bool expected)
    {
        Assert.Equal(expected, AutenticacaoRules.IsValidSenha(new string('a', length)));
    }

    [Fact]
    public void IsValidSenha_RejectsMissingValue()
    {
        Assert.False(AutenticacaoRules.IsValidSenha(null));
    }
}
