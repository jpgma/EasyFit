using EasyFit.Infrastructure.Authentication;

namespace EasyFit.UnitTests.Infrastructure.Authentication;

public class SessaoPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(30, false)]
    [InlineData(29.5, false)]
    [InlineData(28.9, true)]
    [InlineData(1, true)]
    public void ShouldRenew_OnlyAfterAboutOneDayOfUse(double daysRemaining, bool expected)
    {
        Assert.Equal(expected, SessaoPolicy.ShouldRenew(Now.AddDays(daysRemaining), Now));
    }

    [Fact]
    public void Hash_IsStable_AndDiffersFromTheToken()
    {
        var token = SessaoToken.Generate();

        Assert.Equal(SessaoToken.Hash(token), SessaoToken.Hash(token));
        Assert.Equal(32, SessaoToken.Hash(token).Length);
        Assert.NotEqual(token, Convert.ToBase64String(SessaoToken.Hash(token)));
    }

    [Fact]
    public void Generate_ProducesDifferentUrlSafeTokens()
    {
        var first = SessaoToken.Generate();

        Assert.NotEqual(first, SessaoToken.Generate());
        Assert.Equal(43, first.Length);
        Assert.DoesNotContain(first, character => character is '+' or '/' or '=');
    }
}
