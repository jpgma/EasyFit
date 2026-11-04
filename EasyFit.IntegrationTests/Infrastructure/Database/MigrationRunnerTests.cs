using EasyFit.Infrastructure.Database;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace EasyFit.IntegrationTests.Infrastructure.Database;

public class MigrationRunnerTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Run_AppliesEmbeddedMigrations_AndSecondRunIsNoOp()
    {
        await using var database = await TestDatabase.CreateAsync();
        var files = MigrationCatalog.LoadEmbedded();

        var first = await RunAsync(database, files);
        var second = await RunAsync(database, files);

        Assert.Equal(files.Count, first);
        Assert.Equal(0, second);
        Assert.True(await TableExistsAsync(database, "usuarios"));
    }

    [Fact]
    public async Task Run_RefusesMigrationChangedAfterBeingApplied()
    {
        await using var database = await TestDatabase.CreateAsync();
        await RunAsync(database, [MigrationCatalog.Parse("001-teste.sql", "CREATE TABLE teste (id integer)")]);

        MigrationFile[] changed = [MigrationCatalog.Parse("001-teste.sql", "CREATE TABLE teste (id bigint)")];

        await Assert.ThrowsAsync<MigrationException>(() => RunAsync(database, changed));
    }

    [Fact]
    public async Task Run_RollsBackFailedMigration_AndKeepsPreviousOnes()
    {
        await using var database = await TestDatabase.CreateAsync();
        MigrationFile[] files =
        [
            MigrationCatalog.Parse("001-valida.sql", "CREATE TABLE valida (id integer)"),
            MigrationCatalog.Parse("002-falha.sql", "CREATE TABLE parcial (id integer); SELECT * FROM inexistente;"),
        ];

        await Assert.ThrowsAsync<PostgresException>(() => RunAsync(database, files));

        Assert.True(await TableExistsAsync(database, "valida"));
        Assert.False(await TableExistsAsync(database, "parcial"));

        // Corrigido o arquivo, a execução continua de onde parou.
        MigrationFile[] fixedFiles =
        [
            files[0],
            MigrationCatalog.Parse("002-falha.sql", "CREATE TABLE parcial (id integer)"),
        ];
        Assert.Equal(1, await RunAsync(database, fixedFiles));
    }

    private static Task<int> RunAsync(TestDatabase database, IReadOnlyList<MigrationFile> files) =>
        MigrationRunner.RunAsync(database.DataSource, files, NullLogger.Instance, Token);

    private static async Task<bool> TableExistsAsync(TestDatabase database, string table)
    {
        await using var command = database.DataSource.CreateCommand("SELECT to_regclass($1) IS NOT NULL");
        command.Parameters.AddWithValue(table);
        return (bool)(await command.ExecuteScalarAsync(Token))!;
    }
}
