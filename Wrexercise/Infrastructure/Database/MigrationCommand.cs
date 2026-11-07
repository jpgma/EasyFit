using Npgsql;

namespace Wrexercise.Infrastructure.Database;

/// <summary>Subcomando "migrate": aplica as migrações e encerra, sem iniciar o servidor.</summary>
internal static class MigrationCommand
{
    public static async Task<int> RunAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Wrexercise.Migrations");

        try
        {
            var dataSource = services.GetRequiredService<NpgsqlDataSource>();
            var applied = await MigrationRunner.RunAsync(dataSource, MigrationCatalog.LoadEmbedded(), logger);
            logger.LogInformation("Migrações concluídas. Aplicadas nesta execução: {Count}", applied);
            return 0;
        }
        catch (Exception exception) when (exception is MigrationException or NpgsqlException or InvalidOperationException)
        {
            logger.LogError(exception, "Falha ao aplicar as migrações.");
            return 1;
        }
    }
}
