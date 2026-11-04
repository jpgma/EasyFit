using System.Text.Encodings.Web;
using System.Text.Json;
using EasyFit.Features.Autenticacao;
using EasyFit.Features.Convites;
using EasyFit.Features.Desafios;
using EasyFit.Features.Grupos;
using EasyFit.Features.Health;
using EasyFit.Infrastructure.Authentication;
using EasyFit.Infrastructure.Database;
using EasyFit.Infrastructure.Errors;
using EasyFit.Infrastructure.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Npgsql;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Logging.ClearProviders();
// Mantém os acentos legíveis nos logs, que vão só para stdout.
builder.Logging.AddJsonConsole(options =>
    options.JsonWriterOptions = new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

// A API só recebe JSON pequeno. O upload de evidências abrirá sua própria exceção.
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 1024 * 1024);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default));
builder.Services.AddSingleton<NpgsqlDataSource>(DataSourceFactory.Create);

// JSON malformado responde 400 em qualquer ambiente, em vez de virar exceção em Development.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);

// Só o Caddy alcança a aplicação (ver deploy/compose.yaml), então o cabeçalho que ele define é confiável.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddAutenticacaoRateLimiter();

var app = builder.Build();

if (args is ["migrate"])
{
    return await MigrationCommand.RunAsync(app.Services);
}

app.UseForwardedHeaders();
app.UseExceptionHandler(handler => handler.Run(ApiProblem.WriteInternalErrorAsync));
app.UseStaticFiles();
app.UseOriginCheck();
app.UseRateLimiter();

app.MapHealthRoutes();
app.MapAutenticacaoRoutes();
app.MapGruposRoutes();
app.MapConvitesRoutes();
app.MapDesafiosRoutes();

// Rota de API inexistente responde 404 em JSON, nunca com o index.html da SPA.
app.Map("/api/{**rest}", () =>
    ApiProblem.Create(StatusCodes.Status404NotFound, "ROTA_NAO_ENCONTRADA", "Rota não encontrada."));
app.MapFallbackToFile("index.html");

await app.RunAsync();
return 0;

// Exposto para o WebApplicationFactory dos testes de integração.
public partial class Program;
