namespace EasyFit.UnitTests.Architecture;

internal static class RepositoryPaths
{
    public static string Root { get; } = FindRoot();

    public static string FeaturesDirectory => Path.Combine(Root, "EasyFit", "Features");

    public static string MigrationsDirectory =>
        Path.Combine(Root, "EasyFit", "Infrastructure", "Database", "Migrations");

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "EasyFit.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Raiz do repositório não encontrada.");
    }
}
