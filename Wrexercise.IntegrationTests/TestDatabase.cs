using Npgsql;

namespace Wrexercise.IntegrationTests;

/// <summary>Banco descartável criado no PostgreSQL de desenvolvimento (scripts/dev-db.sh).</summary>
internal sealed class TestDatabase : IAsyncDisposable
{
    private const string DefaultServer =
        "Host=127.0.0.1;Port=5433;Database=wrexercise_dev;Username=wrexercise;Password=wrexercise_dev";

    private static string ServerConnectionString =>
        Environment.GetEnvironmentVariable("WREXERCISE_TEST_DB") ?? DefaultServer;

    private readonly string _name;

    private TestDatabase(string name, string connectionString)
    {
        _name = name;
        ConnectionString = connectionString;
        DataSource = new NpgsqlSlimDataSourceBuilder(connectionString).Build();
    }

    public string ConnectionString { get; }

    public NpgsqlDataSource DataSource { get; }

    public static async Task<TestDatabase> CreateAsync()
    {
        var name = $"wrexercise_test_{Guid.NewGuid():N}";
        await ExecuteOnServerAsync($"CREATE DATABASE {name}");

        var connectionString = new NpgsqlConnectionStringBuilder(ServerConnectionString) { Database = name }.ToString();
        return new TestDatabase(name, connectionString);
    }

    public async ValueTask DisposeAsync()
    {
        await DataSource.DisposeAsync();
        await ExecuteOnServerAsync($"DROP DATABASE {_name} WITH (FORCE)");
    }

    private static async Task ExecuteOnServerAsync(string sql)
    {
        await using var dataSource = new NpgsqlSlimDataSourceBuilder(ServerConnectionString).Build();
        await using var command = dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync();
    }
}
