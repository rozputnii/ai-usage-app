using System.Xml.Linq;
using AiUsage.Core.Dashboard;
using Xunit;

namespace AiUsage.Presentation.Tests;

public sealed class DependencyBoundaryTests
{
    [Fact]
    public void CoreHasNoExternalAssemblyOrProjectDependencies()
    {
        var references = typeof(DashboardWorkflow).Assembly.GetReferencedAssemblies();
        Assert.All(references, reference => Assert.StartsWith("System.", reference.Name));
        var project = XDocument.Load(Path.Combine(Root(), "src/windows/AiUsage.Core/AiUsage.Core.csproj"));
        Assert.Empty(project.Descendants("ProjectReference"));
        Assert.Empty(project.Descendants("PackageReference"));
    }

    [Fact]
    public void InfrastructureReferencesOnlyCoreAndPresentationHasNoPlatformAccess()
    {
        var root = Root();
        var project = XDocument.Load(Path.Combine(root, "src/windows/AiUsage.Infrastructure/AiUsage.Infrastructure.csproj"));
        Assert.Equal(["../AiUsage.Core/AiUsage.Core.csproj"], project.Descendants("ProjectReference").Select(node => (string?)node.Attribute("Include")));
        foreach (var path in Directory.EnumerateFiles(Path.Combine(root, "src/windows/AiUsage.Windows/Features"), "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(path);
            Assert.DoesNotContain("AiUsage.Infrastructure", source);
            Assert.DoesNotContain("Microsoft.UI", source);
            Assert.DoesNotContain("Windows.Storage", source);
            Assert.DoesNotContain("App.Resource", source);
        }
        // The real dashboard source also compiles in this assembly, which references Core only.
    }

    private static string Root()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CONTRIBUTING.md")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Run from a repository build output.");
    }
}
