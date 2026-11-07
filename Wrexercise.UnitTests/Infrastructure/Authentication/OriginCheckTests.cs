using Wrexercise.Infrastructure.Authentication;

namespace Wrexercise.UnitTests.Infrastructure.Authentication;

public class OriginCheckTests
{
    [Theory]
    [InlineData("https://fit.exemplo.com", null, "fit.exemplo.com")]
    [InlineData("https://FIT.exemplo.com", null, "fit.exemplo.com")]
    [InlineData("http://localhost:5173", null, "localhost:5173")]
    [InlineData(null, "same-origin", "fit.exemplo.com")]
    [InlineData("", "same-origin", "fit.exemplo.com")]
    public void IsSameOrigin_AcceptsSameOrigin(string? origin, string? secFetchSite, string host)
    {
        Assert.True(OriginCheck.IsSameOrigin(origin, secFetchSite, host));
    }

    [Theory]
    [InlineData("https://malicioso.exemplo", null, "fit.exemplo.com")]
    [InlineData("https://fit.exemplo.com.malicioso.exemplo", null, "fit.exemplo.com")]
    [InlineData("http://localhost:3000", null, "localhost:5173")]
    [InlineData("null", null, "fit.exemplo.com")]
    [InlineData("https://malicioso.exemplo", "same-origin", "fit.exemplo.com")]
    [InlineData(null, null, "fit.exemplo.com")]
    [InlineData(null, "cross-site", "fit.exemplo.com")]
    [InlineData(null, "same-site", "fit.exemplo.com")]
    public void IsSameOrigin_RejectsEverythingElse(string? origin, string? secFetchSite, string host)
    {
        Assert.False(OriginCheck.IsSameOrigin(origin, secFetchSite, host));
    }
}
