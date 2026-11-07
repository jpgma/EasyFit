using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Wrexercise.Infrastructure.Database;

internal sealed record MigrationFile(int Version, string Name, string Sql, string Checksum);

internal sealed record AppliedMigration(int Version, string Name, string Checksum);

internal sealed class MigrationException(string message) : Exception(message);

/// <summary>Lógica pura das migrações: leitura, validação e cálculo do que falta aplicar.</summary>
internal static partial class MigrationCatalog
{
    private const string ResourcePrefix = "Migrations/";

    [GeneratedRegex(@"^(\d{3})-[a-z0-9]+(-[a-z0-9]+)*\.sql$")]
    private static partial Regex FileNamePattern();

    public static IReadOnlyList<MigrationFile> LoadEmbedded()
    {
        var assembly = typeof(MigrationCatalog).Assembly;
        var files = new List<MigrationFile>();

        foreach (var resource in assembly.GetManifestResourceNames())
        {
            if (resource.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            {
                files.Add(Parse(resource[ResourcePrefix.Length..], ReadResource(assembly, resource)));
            }
        }

        return Validate(files);
    }

    public static MigrationFile Parse(string fileName, string sql)
    {
        var match = FileNamePattern().Match(fileName);
        if (!match.Success)
        {
            throw new MigrationException($"Nome de migração inválido: {fileName}. Use o formato NNN-kebab-case.sql.");
        }

        var checksum = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(sql)));
        return new MigrationFile(int.Parse(match.Groups[1].Value), fileName, sql, checksum);
    }

    /// <summary>Ordena as migrações e exige numeração sequencial a partir de 1, sem lacunas.</summary>
    public static IReadOnlyList<MigrationFile> Validate(IEnumerable<MigrationFile> files)
    {
        var ordered = files.OrderBy(file => file.Version).ToList();

        for (var index = 0; index < ordered.Count; index++)
        {
            if (ordered[index].Version != index + 1)
            {
                throw new MigrationException(
                    $"Numeração de migrações inválida em {ordered[index].Name}: esperado {index + 1:D3}.");
            }
        }

        return ordered;
    }

    public static IReadOnlyList<MigrationFile> GetPending(
        IReadOnlyList<MigrationFile> files,
        IReadOnlyList<AppliedMigration> applied)
    {
        var filesByVersion = files.ToDictionary(file => file.Version);

        foreach (var migration in applied)
        {
            if (!filesByVersion.TryGetValue(migration.Version, out var file))
            {
                throw new MigrationException($"A migração aplicada {migration.Name} não existe mais nesta versão.");
            }

            if (file.Checksum != migration.Checksum)
            {
                throw new MigrationException(
                    $"A migração {migration.Name} foi alterada depois de aplicada. Crie uma nova migração.");
            }
        }

        var appliedVersions = applied.Select(migration => migration.Version).ToHashSet();
        return files.Where(file => !appliedVersions.Contains(file.Version)).ToList();
    }

    private static string ReadResource(Assembly assembly, string resource)
    {
        using var stream = assembly.GetManifestResourceStream(resource)
            ?? throw new MigrationException($"Recurso de migração não encontrado: {resource}.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
