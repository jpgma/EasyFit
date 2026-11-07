using Wrexercise.Features.Convites;
using Wrexercise.Features.Convites.Contracts;
using Wrexercise.Features.Grupos.Contracts;

namespace Wrexercise.UnitTests.Features.Convites;

public class ValidacaoConviteTests
{
    [Fact]
    public void Validade_IsSevenDays()
    {
        Assert.Equal(TimeSpan.FromDays(7), ValidacaoConvite.Validade);
    }

    [Theory]
    [InlineData(PapelMembro.Proprietario, true)]
    [InlineData(PapelMembro.Administrador, true)]
    [InlineData(PapelMembro.Membro, false)]
    public void CanGerenciarConvites_ExcludesMembros(string papel, bool expected)
    {
        Assert.Equal(expected, ValidacaoConvite.CanGerenciarConvites(papel));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("abc", true)]
    public void IsPlausibleToken_DiscardsEmptyValues(string? token, bool expected)
    {
        Assert.Equal(expected, ConviteAceite.IsPlausibleToken(token));
    }

    [Fact]
    public void IsPlausibleToken_RejectsAbsurdLengths()
    {
        Assert.False(ConviteAceite.IsPlausibleToken(new string('x', 129)));
    }
}
