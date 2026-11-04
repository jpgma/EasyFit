using Npgsql;

namespace EasyFit.Infrastructure.Database;

internal static class MigrationRunner
{
    // Identificador arbitrário e fixo do lock que impede execuções simultâneas.
    private const long AdvisoryLockKey = 7_236_401_905;

    /// <summary>Aplica as migrações pendentes, cada uma em sua transação, e retorna quantas aplicou.</summary>
    public static async Task<int> RunAsync(
        NpgsqlDataSource dataSource,
        IReadOnlyList<MigrationFile> files,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await SetAdvisoryLockAsync(connection, "pg_advisory_lock", cancellationToken);

        try
        {
            await EnsureHistoryTableAsync(connection, cancellationToken);

            var applied = await GetAppliedAsync(connection, cancellationToken);
            var pending = MigrationCatalog.GetPending(files, applied);

            foreach (var file in pending)
            {
                await ApplyAsync(connection, file, cancellationToken);
                logger.LogInformation("Migração aplicada: {Migration}", file.Name);
            }

            return pending.Count;
        }
        finally
        {
            await SetAdvisoryLockAsync(connection, "pg_advisory_unlock", CancellationToken.None);
        }
    }

    private static async Task SetAdvisoryLockAsync(
        NpgsqlConnection connection,
        string function,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand($"SELECT {function}($1)", connection);
        command.Parameters.AddWithValue(AdvisoryLockKey);
        await command.ExecuteScalarAsync(cancellationToken);
    }

    private static async Task EnsureHistoryTableAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            CREATE TABLE IF NOT EXISTS schema_migrations (
                version integer PRIMARY KEY,
                name text NOT NULL,
                checksum text NOT NULL,
                applied_at timestamptz NOT NULL DEFAULT now()
            )
            """,
            connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<IReadOnlyList<AppliedMigration>> GetAppliedAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT version, name, checksum FROM schema_migrations ORDER BY version",
            connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var applied = new List<AppliedMigration>();
        while (await reader.ReadAsync(cancellationToken))
        {
            applied.Add(new AppliedMigration(reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
        }

        return applied;
    }

    private static async Task ApplyAsync(
        NpgsqlConnection connection,
        MigrationFile file,
        CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var migration = new NpgsqlCommand(file.Sql, connection, transaction))
        {
            await migration.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var record = new NpgsqlCommand(
            "INSERT INTO schema_migrations (version, name, checksum) VALUES ($1, $2, $3)",
            connection,
            transaction))
        {
            record.Parameters.AddWithValue(file.Version);
            record.Parameters.AddWithValue(file.Name);
            record.Parameters.AddWithValue(file.Checksum);
            await record.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
