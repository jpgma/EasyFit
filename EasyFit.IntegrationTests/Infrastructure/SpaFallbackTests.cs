using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EasyFit.IntegrationTests.Infrastructure;

public sealed class SpaFallbackTests : IDisposable
{
    private const string IndexHtml = "<!doctype html><title>EasyFit</title>";

    // Pasta própria para o teste não depender do build do frontend.
    private readonly DirectoryInfo _webRoot = Directory.CreateTempSubdirectory("easyfit-webroot-");

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public SpaFallbackTests() => File.WriteAllText(Path.Combine(_webRoot.FullName, "index.html"), IndexHtml);

    public void Dispose() => _webRoot.Delete(recursive: true);

    [Theory]
    [InlineData("/")]
    [InlineData("/grupos/123/desafios")]
    public async Task NonApiRoutes_ReturnTheSpa(string path)
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path, Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(IndexHtml, await response.Content.ReadAsStringAsync(Token));
    }

    [Theory]
    [InlineData("/api")]
    [InlineData("/api/inexistente")]
    [InlineData("/api/grupos/123")]
    public async Task ApiRoutes_NeverReturnTheSpa(string path)
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path, Token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting(WebHostDefaults.WebRootKey, _webRoot.FullName);
            builder.UseSetting("ConnectionStrings:Database", "Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x");
        });
}
