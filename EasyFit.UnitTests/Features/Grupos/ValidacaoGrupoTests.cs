using EasyFit.Features.Grupos;
using EasyFit.Features.Grupos.Contracts;

namespace EasyFit.UnitTests.Features.Grupos;

public class ValidacaoGrupoTests
{
    private const string P = PapelMembro.Proprietario;
    private const string A = PapelMembro.Administrador;
    private const string M = PapelMembro.Membro;

    [Theory]
    [InlineData("Amigos", true)]
    [InlineData("  Amigos  ", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void IsValidNome_RequiresText(string? nome, bool expected)
    {
        Assert.Equal(expected, ValidacaoGrupo.IsValidNome(nome));
    }

    [Fact]
    public void IsValidNome_LimitsTheLength_AfterTrimming()
    {
        Assert.True(ValidacaoGrupo.IsValidNome($"  {new string('a', 80)}  "));
        Assert.False(ValidacaoGrupo.IsValidNome(new string('a', 81)));
    }

    [Fact]
    public void NormalizeNome_Trims()
    {
        Assert.Equal("Amigos", ValidacaoGrupo.NormalizeNome("  Amigos "));
    }

    [Theory]
    [InlineData("America/Sao_Paulo", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsPlausibleFusoHorario_DiscardsWhatIsNotWorthAQuery(string? fuso, bool expected)
    {
        Assert.Equal(expected, ValidacaoGrupo.IsPlausibleFusoHorario(fuso));
    }

    [Fact]
    public void IsPlausibleFusoHorario_RejectsAbsurdLengths()
    {
        Assert.False(ValidacaoGrupo.IsPlausibleFusoHorario(new string('a', 65)));
    }

    [Theory]
    [InlineData(A, true)]
    [InlineData(M, true)]
    [InlineData(P, false)]
    [InlineData("Dono", false)]
    [InlineData(null, false)]
    public void IsPapelAtribuivel_NeverGrantsOwnership(string? papel, bool expected)
    {
        Assert.Equal(expected, ValidacaoGrupo.IsPapelAtribuivel(papel));
    }

    [Theory]
    [InlineData(P, P, false)]
    [InlineData(P, A, true)]
    [InlineData(P, M, true)]
    [InlineData(A, P, false)]
    [InlineData(A, A, false)]
    [InlineData(A, M, false)]
    [InlineData(M, P, false)]
    [InlineData(M, A, false)]
    [InlineData(M, M, false)]
    public void CanAlterarPapel_IsOnlyForTheProprietario(string ator, string alvo, bool expected)
    {
        Assert.Equal(expected, ValidacaoGrupo.CanAlterarPapel(ator, alvo));
    }

    [Theory]
    [InlineData(P, P, false)]
    [InlineData(P, A, true)]
    [InlineData(P, M, true)]
    [InlineData(A, P, false)]
    [InlineData(A, A, false)]
    [InlineData(A, M, true)]
    [InlineData(M, P, false)]
    [InlineData(M, A, false)]
    [InlineData(M, M, false)]
    public void CanRemoverMembro_FollowsTheHierarchy(string ator, string alvo, bool expected)
    {
        Assert.Equal(expected, ValidacaoGrupo.CanRemoverMembro(ator, alvo));
    }

    [Theory]
    [InlineData(P, false)]
    [InlineData(A, true)]
    [InlineData(M, true)]
    public void CanSair_RequiresTransferringOwnershipFirst(string papel, bool expected)
    {
        Assert.Equal(expected, ValidacaoGrupo.CanSair(papel));
    }

    [Theory]
    [InlineData(P, true)]
    [InlineData(A, false)]
    [InlineData(M, false)]
    public void CanTransferir_IsOnlyForTheProprietario(string papel, bool expected)
    {
        Assert.Equal(expected, ValidacaoGrupo.CanTransferir(papel));
    }
}
