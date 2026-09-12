using AiUsage.ProjectValidation;
using Xunit;

namespace AiUsage.ProjectValidation.Tests;

public sealed class ValidatorTests
{
    [Fact]
    public void MinimalFullRootAcceptsSharedFeatureIdsDeferredWorkAndPlannedPaths()
    {
        using var root = new Fixture();
        root.Append("docs/specs/AIU-001/spec.md", "\nPlanned output: `future/not-created.md`.\n");
        Assert.Empty(ProjectValidator.Validate(root.Path));
    }

    [Fact]
    public void LanguagePolicyChecksAuthoredRulesButNeverProfileCredentials()
    {
        using var root = new Fixture();
        root.Put(".omp/RULES.md", "# Rules\nEnglish-only project policy.\n");
        root.Put(".omp/auth.json", "\u041d\u0435\u0430\u043d\u0433\u043b\u0456\u0439\u0441\u044c\u043a\u0438\u0439");
        Assert.Empty(ProjectValidator.Validate(root.Path));
        root.Append(".omp/RULES.md", "\u041d\u0435\u0430\u043d\u0433\u043b\u0456\u0439\u0441\u044c\u043a\u0438\u0439");
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.File == ".omp/RULES.md" && d.Code == "NON_ENGLISH_SCRIPT");
    }

    [Fact]
    public void AuthoredScanRefusesAReparseAncestor()
    {
        using var root = new Fixture();
        using var outside = new Fixture();
        outside.Put("skills/example/SKILL.md", "# Outside data\n");
        var link = System.IO.Path.Combine(root.Path, ".omp");
        try { Directory.CreateSymbolicLink(link, outside.Path); }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or PlatformNotSupportedException)
        { Assert.Skip("Symbolic link creation unavailable: " + ex.GetType().Name); }
        try { Assert.Contains(ProjectValidator.Validate(root.Path), d => d.Code == "UNSAFE_PATH"); }
        finally { Directory.Delete(link); }
    }

    [Theory]
    [InlineData("docs/product/goals.md", "G-001 -", "G-1 -", "INVALID_ID")]
    [InlineData("docs/backlog.md", "AIU-001 -", "AIU-1 -", "INVALID_ID")]
    [InlineData("docs/specs/AIU-001/tasks.md", "T-01 -", "T-1 -", "INVALID_ID")]
    [InlineData("docs/backlog.md", "status: selected", "status: magic", "INVALID_STATUS")]
    [InlineData("docs/specs/AIU-001/tasks.md", "status: pending", "status: magic", "INVALID_STATUS")]
    [InlineData("docs/specs/AIU-001/spec.md", "status: draft", "status: done", "INVALID_STATUS")]
    [InlineData("docs/backlog.md", "goal: G-001", "goal: G-999", "MISSING_GOAL")]
    [InlineData("docs/product/goals.md", "scope: AIU-001", "scope: AIU-999", "MISSING_REFERENCE")]
    [InlineData("docs/product/goals.md", "scope: AIU-001", "scope: []", "GOAL_SCOPE")]
    [InlineData("docs/backlog.md", "depends_on: []", "depends_on: [AIU-999]", "MISSING_DEPENDENCY")]
    [InlineData("docs/specs/AIU-001/tasks.md", "depends_on: []", "depends_on: [T-99]", "MISSING_DEPENDENCY")]
    [InlineData("docs/specs/AIU-001/tasks.md", "depends_on: []", "depends_on: [T-01]", "DEPENDENCY_CYCLE")]
    [InlineData("docs/backlog.md", "depends_on: []", "depends_on: [AIU-001]", "DEPENDENCY_CYCLE")]
    [InlineData("docs/specs/AIU-001/tasks.md", "AC-01", "AC-99", "AC_REFERENCE")]
    [InlineData("docs/specs/AIU-001/spec.md", "scope_version: 1\n", "", "REQUIRED_METADATA")]
    [InlineData("docs/specs/AIU-001/tasks.md", "status: pending", "status: done", "DONE_WITHOUT_EVIDENCE")]
    [InlineData("docs/specs/AIU-001/tasks.md", "isolation: required", "isolation: none", "WORKER_ISOLATION")]
    [InlineData("docs/specs/AIU-001/tasks.md", "shared: []", "shared: [src/contracts]", "PRIMARY_SHARED")]
    [InlineData("docs/specs/AIU-001/spec.md", "status: draft", "status: implemented", "LIFECYCLE_STATUS")]
    public void CorruptionReportsStableContract(string file, string oldValue, string newValue, string code)
    {
        using var root = new Fixture();
        root.Replace(file, oldValue, newValue);
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.Code == code && d.File == file && d.Message.Length > 0);
    }

    [Theory]
    [InlineData("../outside")]
    [InlineData("C:\\outside")]
    [InlineData("/outside")]
    [InlineData("~/outside")]
    [InlineData("\\\\server\\share")]
    [InlineData("src/../outside")]
    [InlineData("src/**/../outside")]
    [InlineData("src/**/.git/config")]
    [InlineData(".GIT/config")]
    [InlineData("src/file:stream")]
    [InlineData("src./file")]
    [InlineData("src/CON.txt")]
    public void UnsafeWindowsPathsAreRejected(string path)
    {
        using var root = new Fixture();
        root.Replace(Fixture.Tasks, "src/one/**", path);
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.Code == "UNSAFE_PATH" && d.Task == "T-01");
    }

    [Theory]
    [InlineData("SRC\\ONE\\file.cs", "other")]
    [InlineData("src/*/file.cs", "other")]
    [InlineData("different/**", "domain-one")]
    public void ConcurrentOwnershipIsConservative(string writes, string ownership)
    {
        using var root = new Fixture();
        root.Append(Fixture.Tasks, Fixture.Task("T-02", writes, ownership));
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.Code == "PARALLEL_OVERLAP");
    }

    [Fact]
    public void DependencyOrderingSerializesOverlappingWorkers()
    {
        using var root = new Fixture();
        root.Append(Fixture.Tasks, Fixture.Task("T-02", "src/one/**", "domain-one").Replace("depends_on: []", "depends_on: [T-01]"));
        Assert.Empty(ProjectValidator.Validate(root.Path));
    }

    [Theory]
    [InlineData("docs/backlog.md", "\n## AIU-001 - Duplicate\n", "DUPLICATE_ID")]
    [InlineData("docs/product/goals.md", "\n## G-001 - Duplicate\n", "DUPLICATE_ID")]
    [InlineData("docs/specs/AIU-001/spec.md", "\n- AC-01: Duplicate.\n", "DUPLICATE_ID")]
    [InlineData("docs/specs/AIU-001/tasks.md", "\n### T-01 - Duplicate\n", "DUPLICATE_ID")]
    [InlineData("docs/specs/AIU-001/spec.md", "\n[Missing](missing.md)\n", "BROKEN_LINK")]
    [InlineData("docs/specs/AIU-001/spec.md", "\n[Missing][ref]\n[ref]: missing.md\n", "BROKEN_LINK")]
    [InlineData("docs/specs/AIU-001/spec.md", "\n\u041d\u0435\u0430\u043d\u0433\u043b\u0456\u0439\u0441\u044c\u043a\u0438\u0439 \u0442\u0435\u043a\u0441\u0442.\n", "NON_ENGLISH_SCRIPT")]
    [InlineData("docs/specs/AIU-001/spec.md", "\n```text\n\u041d\u0435\u0430\u043d\u0433\u043b\u0456\u0439\u0441\u044c\u043a\u0438\u0439 \u0442\u0435\u043a\u0441\u0442.\n```\n", "NON_ENGLISH_SCRIPT")]
    public void DocumentCorruptionsAreRejected(string file, string content, string code)
    {
        using var root = new Fixture();
        root.Append(file, content);
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.File == file && d.Code == code);
    }

    [Fact]
    public void ExplicitOpaqueFenceExemptionDoesNotExemptFollowingProse()
    {
        using var root = new Fixture();
        root.Append("docs/specs/AIU-001/spec.md", "\n```opaque-user-data\n\u041d\u0435\u0430\u043d\u0433\u043b\u0456\u0439\u0441\u044c\u043a\u0438\u0439 \u0442\u0435\u043a\u0441\u0442.\n```\n");
        Assert.Empty(ProjectValidator.Validate(root.Path));
        root.Append("docs/specs/AIU-001/spec.md", "\u041d\u0435\u0430\u043d\u0433\u043b\u0456\u0439\u0441\u044c\u043a\u0438\u0439 \u0442\u0435\u043a\u0441\u0442.\n");
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.Code == "NON_ENGLISH_SCRIPT");
    }

    [Fact]
    public void EvidenceNeedsAnExistingArtifactAndWorkerIntegration()
    {
        using var root = new Fixture();
        root.Replace(Fixture.Tasks, "status: pending", "status: done");
        root.Replace(Fixture.Tasks, "evidence: not-run", "evidence: checks passed; integrated");
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.Code == "DONE_WITHOUT_EVIDENCE");
        root.Replace(Fixture.Tasks, "checks passed; integrated", "docs/check.txt; checks passed; integrated");
        root.Put("docs/check.txt", "PASS: disposable smoke command returned expected result.");
        Assert.Empty(ProjectValidator.Validate(root.Path));
        root.Replace(Fixture.Tasks, "; integrated", "");
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.Code == "DONE_WITHOUT_INTEGRATION");
    }

    [Fact]
    public void ReparseWriteRootCannotEscape()
    {
        using var root = new Fixture();
        using var outside = new Fixture();
        // Windows developer mode or symlink privilege is needed; an unavailable capability
        // is a visible skipped test, never an apparent passing security check.
        try { Directory.CreateSymbolicLink(System.IO.Path.Combine(root.Path, "escape"), outside.Path); }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or PlatformNotSupportedException)
        { Assert.Skip("Symbolic link creation unavailable: " + ex.GetType().Name); }
        root.Replace(Fixture.Tasks, "src/one/**", "escape/future/**");
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.Code == "UNSAFE_PATH");
    }

    private sealed class Fixture : IDisposable
    {
        public const string Tasks = "docs/specs/AIU-001/tasks.md";
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "aiu-validator-" + Guid.NewGuid().ToString("N"));
        public Fixture()
        {
            Put("docs/product/goals.md", "---\nschema_version: 1\nactive_goal: G-001\n---\n# Goals\n## G-001 - Goal\n- status: selected\n- scope: AIU-001\n- outcome: Valid outcome.\n- success: Observable success.\n");
            Put("docs/backlog.md", "---\nschema_version: 1\n---\n# Backlog\n## AIU-001 - Feature\n- goal: G-001\n- status: selected\n- depends_on: []\n- trigger: now\n- outcome: Valid outcome.\n");
            var metadata = "---\nid: AIU-001\ntype: infrastructure\nstatus: draft\ngoal: G-001\nscope_version: 1\napproval_basis: derived-within-authorized-goal\n---\n";
            Put("docs/specs/AIU-001/spec.md", metadata + "# Feature\n## Acceptance criteria\n- AC-01: Observable behavior.\n");
            Put("docs/specs/AIU-001/design.md", metadata + "# Design\nSimple implementation.\n");
            Put(Tasks, "---\nid: AIU-001\nschema_version: 1\n---\n# Tasks\n" + Task("T-01", "src/one/**", "domain-one"));
        }
        public static string Task(string id, string writes, string ownership) => $"\n### {id} - Work\n- status: pending\n- depends_on: []\n- ownership: {ownership}\n- writes: [\"{writes}\"]\n- shared: []\n- parallel: true\n- isolation: required\n- agent: worker\n- acceptance: [AC-01]\n- evidence: not-run\n";
        public void Put(string file, string content)
        {
            var target = System.IO.Path.Combine(Path, file);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(target)!);
            File.WriteAllText(target, content);
        }
        public void Replace(string file, string before, string after) => Put(file, File.ReadAllText(System.IO.Path.Combine(Path, file)).Replace(before, after, StringComparison.Ordinal));
        public void Append(string file, string content) => File.AppendAllText(System.IO.Path.Combine(Path, file), content);
        public void Dispose()
        {
            var link = System.IO.Path.Combine(Path, "escape");
            if (Directory.Exists(link)) Directory.Delete(link);
            Directory.Delete(Path, true);
        }
    }
}
