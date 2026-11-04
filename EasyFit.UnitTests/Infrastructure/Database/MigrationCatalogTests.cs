using EasyFit.Infrastructure.Database;
using EasyFit.UnitTests.Architecture;

namespace EasyFit.UnitTests.Infrastructure.Database;

public class MigrationCatalogTests
{
    [Fact]
    public void MigrationsDirectory_FollowsNamingAndSequence()
    {
        var files = Directory.GetFiles(RepositoryPaths.MigrationsDirectory)
            .Select(path => MigrationCatalog.Parse(Path.GetFileName(path), File.ReadAllText(path)));

        Assert.NotEmpty(MigrationCatalog.Validate(files));
    }

    [Fact]
    public void LoadEmbedded_IncludesEveryFileInMigrationsDirectory()
    {
        var onDisk = Directory.GetFiles(RepositoryPaths.MigrationsDirectory).Select(Path.GetFileName).Order();
        var embedded = MigrationCatalog.LoadEmbedded().Select(file => file.Name).Order();

        Assert.Equal(onDisk, embedded);
    }

    [Theory]
    [InlineData("1-create-usuarios.sql")]
    [InlineData("001_create_usuarios.sql")]
    [InlineData("001-Create-Usuarios.sql")]
    [InlineData("001-create-usuarios.txt")]
    [InlineData("create-usuarios.sql")]
    public void Parse_RejectsInvalidNames(string fileName)
    {
        Assert.Throws<MigrationException>(() => MigrationCatalog.Parse(fileName, "SELECT 1"));
    }

    [Fact]
    public void Parse_ReadsVersionAndHashesContent()
    {
        var first = MigrationCatalog.Parse("012-create-grupos.sql", "SELECT 1");
        var changed = MigrationCatalog.Parse("012-create-grupos.sql", "SELECT 2");

        Assert.Equal(12, first.Version);
        Assert.NotEqual(first.Checksum, changed.Checksum);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 3)]
    [InlineData(2, 3)]
    public void Validate_RejectsDuplicatesAndGaps(int firstVersion, int secondVersion)
    {
        MigrationFile[] files = [Migration(firstVersion), Migration(secondVersion)];

        Assert.Throws<MigrationException>(() => MigrationCatalog.Validate(files));
    }

    [Fact]
    public void GetPending_ReturnsOnlyMigrationsNotApplied()
    {
        MigrationFile[] files = [Migration(1), Migration(2)];
        AppliedMigration[] applied = [new(1, files[0].Name, files[0].Checksum)];

        var pending = MigrationCatalog.GetPending(files, applied);

        Assert.Equal(2, Assert.Single(pending).Version);
    }

    [Fact]
    public void GetPending_RejectsAppliedMigrationThatChanged()
    {
        MigrationFile[] files = [Migration(1)];
        AppliedMigration[] applied = [new(1, files[0].Name, "checksum-antigo")];

        Assert.Throws<MigrationException>(() => MigrationCatalog.GetPending(files, applied));
    }

    [Fact]
    public void GetPending_RejectsAppliedMigrationThatNoLongerExists()
    {
        AppliedMigration[] applied = [new(1, "001-removida.sql", "checksum")];

        Assert.Throws<MigrationException>(() => MigrationCatalog.GetPending([], applied));
    }

    private static MigrationFile Migration(int version) =>
        MigrationCatalog.Parse($"{version:D3}-teste.sql", $"SELECT {version}");
}
