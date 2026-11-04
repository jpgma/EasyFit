using System.Text.Encodings.Web;
using System.Text.Json;
using EasyFit.Features.Health;
using EasyFit.Infrastructure.Database;
using EasyFit.Infrastructure.Errors;
using EasyFit.Infrastructure.Json;
using Npgsql;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Logging.ClearProviders();
// Mantém os acentos legíveis nos logs, que vão só para stdout.
builder.Logging.AddJsonConsole(options =>
    options.JsonWriterOptions = new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default));
builder.Services.AddSingleton<NpgsqlDataSource>(DataSourceFactory.Create);

var app = builder.Build();

if (args is ["migrate"])
{
    return await MigrationCommand.RunAsync(app.Services);
}

app.UseExceptionHandler(handler => handler.Run(ApiProblem.WriteInternalErrorAsync));
app.UseStaticFiles();

app.MapHealthRoutes();

// Rota de API inexistente responde 404 em JSON, nunca com o index.html da SPA.
app.Map("/api/{**rest}", () =>
    ApiProblem.Create(StatusCodes.Status404NotFound, "ROTA_NAO_ENCONTRADA", "Rota não encontrada."));
app.MapFallbackToFile("index.html");

await app.RunAsync();
return 0;

// Exposto para o WebApplicationFactory dos testes de integração.
public partial class Program;
