using System.Text.RegularExpressions;

namespace EasyFit.UnitTests.Architecture;

/// <summary>Uma fatia só pode referenciar outra pelo namespace EasyFit.Features.X.Contracts.</summary>
public partial class FeatureBoundaryTests
{
    [GeneratedRegex(@"EasyFit\.Features\.(?<feature>\w+)(?:\.(?<child>\w+))?")]
    private static partial Regex FeatureReference();

    [Fact]
    public void Features_DoNotReferenceInternalsOfOtherFeatures()
    {
        var violations = new List<string>();

        foreach (var featureDirectory in Directory.GetDirectories(RepositoryPaths.FeaturesDirectory))
        {
            var feature = Path.GetFileName(featureDirectory);
            foreach (var file in Directory.EnumerateFiles(featureDirectory, "*.cs", SearchOption.AllDirectories))
            {
                violations.AddRange(FindViolations(feature, File.ReadAllText(file))
                    .Select(reference => $"{Path.GetRelativePath(RepositoryPaths.Root, file)}: {reference}"));
            }
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Theory]
    [InlineData("using EasyFit.Features.Grupos;")]
    [InlineData("using EasyFit.Features.Grupos.Queries;")]
    [InlineData("var total = EasyFit.Features.Grupos.GruposQueries.Count();")]
    public void FindViolations_RejectsReferencesToOtherFeatures(string source)
    {
        Assert.NotEmpty(FindViolations("Reacoes", source));
    }

    [Theory]
    [InlineData("namespace EasyFit.Features.Reacoes;")]
    [InlineData("using EasyFit.Features.Reacoes.Rules;")]
    [InlineData("using EasyFit.Features.Grupos.Contracts;")]
    [InlineData("using EasyFit.Infrastructure.Errors;")]
    public void FindViolations_AllowsOwnFeatureAndContracts(string source)
    {
        Assert.Empty(FindViolations("Reacoes", source));
    }

    private static IEnumerable<string> FindViolations(string feature, string source) =>
        FeatureReference().Matches(source)
            .Where(match => match.Groups["feature"].Value != feature && match.Groups["child"].Value != "Contracts")
            .Select(match => match.Value);
}
