using EasyFit.Infrastructure.Errors;
using Npgsql;

namespace EasyFit.Features.Health;

internal sealed record HealthResponse(string Status);

internal static class HealthRoutes
{
    public static void MapHealthRoutes(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/health", () => Results.Ok(new HealthResponse("ok")));
        routes.MapGet("/ready", GetReadyAsync);
    }

    private static async Task<IResult> GetReadyAsync(
        NpgsqlDataSource dataSource,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var command = dataSource.CreateCommand("SELECT 1");
            await command.ExecuteScalarAsync(cancellationToken);
            return Results.Ok(new HealthResponse("ok"));
        }
        catch (Exception exception) when (exception is NpgsqlException or TimeoutException)
        {
            loggerFactory.CreateLogger("EasyFit.Health").LogError(exception, "Banco de dados indisponível.");
            return ApiProblem.Create(
                StatusCodes.Status503ServiceUnavailable,
                "BANCO_INDISPONIVEL",
                "Serviço temporariamente indisponível.");
        }
    }
}
