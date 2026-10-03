using System.Text.RegularExpressions;
using System.Xml.Linq;
using AiUsage.Core.Dashboard;
using Xunit;

namespace AiUsage.Presentation.Tests;

public sealed class DependencyBoundaryTests
{
    private static string Windows => Path.Combine(Repository.Root(), "src/windows/AiUsage.Windows");

    private static IEnumerable<string> WindowsSources(string pattern) =>
        Directory.EnumerateFiles(Windows, pattern, SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));

    [Fact]
    public void CoreHasNoExternalAssemblyOrProjectDependencies()
    {
        var references = typeof(DashboardWorkflow).Assembly.GetReferencedAssemblies();
        Assert.All(references, reference => Assert.StartsWith("System.", reference.Name));
        var project = XDocument.Load(Path.Combine(Repository.Root(), "src/windows/AiUsage.Core/AiUsage.Core.csproj"));
        Assert.Empty(project.Descendants("ProjectReference"));
        Assert.Empty(project.Descendants("PackageReference"));
    }

    [Fact]
    public void InfrastructureReferencesOnlyCore()
    {
        var project = XDocument.Load(Path.Combine(Repository.Root(), "src/windows/AiUsage.Infrastructure/AiUsage.Infrastructure.csproj"));
        Assert.Equal(["../AiUsage.Core/AiUsage.Core.csproj"], project.Descendants("ProjectReference").Select(node => (string?)node.Attribute("Include")));
    }

    /// <summary>View models, contracts and mocks compile without WinUI so they stay testable here.</summary>
    [Fact]
    public void FeatureSourcesOutsideCodeBehindHaveNoPlatformAccess()
    {
        var sources = Directory.EnumerateFiles(Path.Combine(Windows, "Features"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(".xaml.cs", StringComparison.Ordinal)).ToArray();
        Assert.NotEmpty(sources);
        foreach (var path in sources)
        {
            var source = File.ReadAllText(path);
            Assert.DoesNotContain("Microsoft.UI", source);
            Assert.DoesNotContain("Windows.Storage", source);
            Assert.DoesNotContain("Windows.UI", source);
            Assert.DoesNotContain("System.IO.File", source);
            Assert.DoesNotContain("HttpClient", source);
            Assert.DoesNotContain("Process.Start", source);
        }
    }

    /// <summary>AC-10: the mock frontend neither references nor initializes Core/Infrastructure workflows, sessions or storage.</summary>
    [Fact]
    public void WindowsPresentationDoesNotUseBackendProjects()
    {
        var sources = WindowsSources("*.cs").Concat(WindowsSources("*.xaml"))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}Adapters{Path.DirectorySeparatorChar}Live{Path.DirectorySeparatorChar}")).ToArray();
        Assert.NotEmpty(sources);
        foreach (var path in sources)
        {
            var source = File.ReadAllText(path);
            Assert.False(Regex.IsMatch(source, @"\bAiUsage\.(Core|Infrastructure)\b"), $"{Path.GetFileName(path)} uses a backend namespace.");
            Assert.DoesNotContain("AddCodexProductSession", source);
            Assert.DoesNotContain("AddClaudeProductSession", source);
            Assert.DoesNotContain("\"providers\"", source);
        }
    }

    [Fact]
    public void DemoCompositionIsExplicitAndKeepsItsOwnAdapters()
    {
        var app = File.ReadAllText(Path.Combine(Windows, "App.xaml.cs"));
        Assert.Contains("AddLedgerDemo", app);
        Assert.Contains("else builder.Services.AddLiveLedgerServices()", app);
        Assert.Contains("\"--demo\"", app);
        var registration = string.Join('\n', Directory.EnumerateFiles(Path.Combine(Windows, "Composition"), "*.cs").Select(File.ReadAllText));
        Assert.Contains("AddSingleton<ILedgerSource>(provider => provider.GetRequiredService<DemoLedgerSource>())", registration);
    }

}

