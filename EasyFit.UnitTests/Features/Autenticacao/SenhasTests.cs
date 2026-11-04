using EasyFit.Features.Autenticacao;

namespace EasyFit.UnitTests.Features.Autenticacao;

public class SenhasTests
{
    [Fact]
    public void Verify_AcceptsCorrectPassword_AndRejectsWrongOne()
    {
        var hash = Senhas.Hash("senha-correta");

        Assert.True(Senhas.Verify("senha-correta", hash));
        Assert.False(Senhas.Verify("senha-errada", hash));
    }

    [Fact]
    public void Hash_UsesDifferentSaltEachTime_AndNeverContainsThePassword()
    {
        var first = Senhas.Hash("senha-correta");
        var second = Senhas.Hash("senha-correta");

        Assert.NotEqual(first, second);
        Assert.DoesNotContain("senha-correta", first);
        Assert.StartsWith("pbkdf2-sha256$600000$", first);
    }

    [Theory]
    [InlineData("")]
    [InlineData("texto-qualquer")]
    [InlineData("md5$1000$c2Fs$aGFzaA==")]
    [InlineData("pbkdf2-sha256$abc$c2Fs$aGFzaA==")]
    [InlineData("pbkdf2-sha256$0$c2Fs$aGFzaA==")]
    [InlineData("pbkdf2-sha256$999999999$c2Fs$aGFzaA==")]
    [InlineData("pbkdf2-sha256$1000$nao-e-base64!$aGFzaA==")]
    [InlineData("pbkdf2-sha256$1000$c2Fs$")]
    public void Verify_RejectsMalformedHash_WithoutThrowing(string storedHash)
    {
        Assert.False(Senhas.Verify("senha-correta", storedHash));
    }
}
