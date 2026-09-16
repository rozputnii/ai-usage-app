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
        var sources = WindowsSources("*.cs").Concat(WindowsSources("*.xaml")).ToArray();
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
    public void DefaultCompositionRegistersOnlyDemoAdapters()
    {
        var app = File.ReadAllText(Path.Combine(Windows, "App.xaml.cs"));
        Assert.Contains("AddDemoServices", app);
        Assert.DoesNotContain("AddLiveServices", app);
        var registration = string.Join('\n', Directory.EnumerateFiles(Path.Combine(Windows, "Composition"), "*.cs").Select(File.ReadAllText));
        foreach (var adapter in new[] { "IUsageSource", "IConnectionFlow", "IHistorySource", "IPreferenceStore", "INotificationPreview", "ICliImportService", "IDiagnosticsService", "IDataManagementService", "IRecoveryService", "IUpdateService" })
            Assert.Matches(new Regex(adapter + @">\(\s*services\s*=>\s*services\.GetRequiredService<Demo"), registration);
    }

    /// <summary>Every resource key a view model or view requests exists in the English resources.</summary>
    [Fact]
    public void ReferencedResourceKeysExist()
    {
        var keys = TestText.All.Keys.ToHashSet(StringComparer.Ordinal);
        var missing = new List<string>();
        foreach (var path in Directory.EnumerateFiles(Path.Combine(Windows, "Features"), "*.cs", SearchOption.AllDirectories))
            foreach (Match match in Regex.Matches(File.ReadAllText(path), @"\b(?:T|F|Get)\(""([A-Za-z0-9_]+)""\s*[,)]"))
                if (!keys.Contains(match.Groups[1].Value))
                    missing.Add($"{Path.GetFileName(path)}: {match.Groups[1].Value}");
        foreach (var path in WindowsSources("*.xaml"))
            foreach (Match match in Regex.Matches(File.ReadAllText(path), @"x:Uid=""([A-Za-z0-9_]+)"""))
                if (!keys.Any(key => key.StartsWith(match.Groups[1].Value + ".", StringComparison.Ordinal)))
                    missing.Add($"{Path.GetFileName(path)}: x:Uid {match.Groups[1].Value}");
        Assert.Empty(missing);
    }

    [Fact]
    public void DynamicResourceKeyFamiliesAreComplete()
    {
        var text = new TestText();
        foreach (var value in Enum.GetNames<AiUsage.Features.Presentation.ConnectionState>()) _ = text.Get("Connection_" + value);
        foreach (var value in Enum.GetNames<AiUsage.Features.Presentation.Freshness>()) _ = text.Get("Freshness_" + value);
        foreach (var value in Enum.GetNames<AiUsage.Features.Presentation.AccountOperation>()) _ = text.Get("Operation_" + value);
        foreach (var value in Enum.GetNames<AiUsage.Features.Presentation.ContextKind>()) _ = text.Get("ContextKind_" + value);
        foreach (var value in Enum.GetNames<AiUsage.Features.Presentation.RuleScope>()) _ = text.Get("Scope_" + value);
        foreach (var value in Enum.GetNames<AiUsage.Features.Presentation.AttentionLevel>()) _ = text.Get("Attention_" + value);
        foreach (var value in Enum.GetNames<AiUsage.Features.Presentation.HistoryRetention>()) _ = text.Get("Retention_" + value);
        foreach (var value in Enum.GetNames<AiUsage.Features.Presentation.ThemePreference>()) { _ = text.Get("Theme_" + value); _ = text.Get("Theme_" + value + "Description"); }
        foreach (var value in Enum.GetNames<AiUsage.Features.Connection.ConnectionMethod>()) _ = text.Get("Method_" + value);
        foreach (var value in new[] { "Interrupted", "NewerSchema", "RestoreFailed" }) { _ = text.Get("Recovery_Title_" + value); _ = text.Get("Recovery_Body_" + value); }
        foreach (var value in new[] { "NetworkFailure", "RateLimited", "InvalidGrant", "SchemaMismatch" }) _ = text.Get("Failure_Short_" + value);
    }
}
