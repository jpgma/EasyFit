using System.Xml.Linq;

namespace EasyFit.UnitTests.Architecture;

/// <summary>Adicionar um pacote exige editar esta lista de propósito. Ver "Dependências" no AGENTS.md.</summary>
public class PackageAllowlistTests
{
    private static readonly HashSet<string> AllowedPackages =
    [
        // Aplicação
        "Npgsql",

        // Somente testes
        "Microsoft.AspNetCore.Mvc.Testing",
        "xunit.v3",
    ];

    [Fact]
    public void DirectoryPackages_ContainsOnlyAllowedPackages()
    {
        var document = XDocument.Load(Path.Combine(RepositoryPaths.Root, "Directory.Packages.props"));

        var unexpected = document.Descendants("PackageVersion")
            .Select(package => package.Attribute("Include")?.Value ?? string.Empty)
            .Where(package => !AllowedPackages.Contains(package))
            .ToList();

        Assert.True(unexpected.Count == 0, $"Pacotes fora da lista permitida: {string.Join(", ", unexpected)}");
    }
}
