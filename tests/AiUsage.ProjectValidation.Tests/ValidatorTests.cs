using AiUsage.ProjectValidation;
using Xunit;

namespace AiUsage.ProjectValidation.Tests;

public sealed class ValidatorTests
{
    [Fact]
    public void SharedSkillDoesNotRequireAnOmpCopy()
    {
        using var root = new Fixture();
        root.Put(".agents/skills/example/SKILL.md", "---\nname: example\ndescription: Inspect evidence\n---\n# Example\n");
        Assert.Empty(ProjectValidator.Validate(root.Path));
    }

    [Fact]
    public void FeatureMayOmitInternalTasks()
    {
        using var root = new Fixture();
        File.Delete(Path.Combine(root.Path, Fixture.Tasks));
        Assert.Empty(ProjectValidator.Validate(root.Path));
    }

    [Fact]
    public void SequentialTaskNeedsNoRuntimeMetadata()
    {
        using var root = new Fixture();
        root.Put(Fixture.Tasks, "---\nid: AIU-001\nschema_version: 1\n---\n# Tasks\n### T-01 - Implement behavior\n- status: pending\n- depends_on: []\n- acceptance: [AC-01]\n- evidence: not-run\n");
        Assert.Empty(ProjectValidator.Validate(root.Path));
    }

    [Fact]
    public void MinimalFullRootAcceptsSharedFeatureIdsDeferredWorkAndPlannedPaths()
    {
        using var root = new Fixture();
        root.Append("docs/specs/AIU-001/spec.md", "\nPlanned output: `future/not-created.md`.\n");
        Assert.Empty(ProjectValidator.Validate(root.Path));
    }


    [Theory]
    [InlineData(".omp")]
    [InlineData(".agents")]
    [InlineData("docs/archive")]
    public void AuthoredScanRefusesAReparseAncestor(string directory)
    {
        using var root = new Fixture();
        using var outside = new Fixture();
        outside.Put("skills/example/SKILL.md", "# Outside data\n");
        outside.Put("AGENTS.md", "[Missing](missing.md)\n");
        var link = System.IO.Path.Combine(root.Path, directory);
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
    public void DocumentCorruptionsAreRejected(string file, string content, string code)
    {
        using var root = new Fixture();
        root.Append(file, content);
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.File == file && d.Code == code);
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

    [Theory]
    [InlineData("---\nname: example\n---\n", "SKILL_METADATA")]
    [InlineData("# No metadata\n", "SKILL_METADATA")]
    [InlineData("---\nname: example\ndescription:\n---\n", "SKILL_METADATA")]
    public void SkillMetadataIsRequired(string content, string code)
    {
        using var root = new Fixture();
        root.Put(".agents/skills/example/SKILL.md", content);
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.Code == code);
    }

    [Fact]
    public void DuplicateSkillNamesWithinOneHarnessAreRejected()
    {
        using var root = new Fixture();
        const string skill = "---\nname: example\ndescription: Inspect example evidence\n---\n";
        root.Put(".agents/skills/example/SKILL.md", skill);
        root.Put(".agents/skills/duplicate/SKILL.md", skill);
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.Code == "DUPLICATE_SKILL");
    }




    [Theory]
    [InlineData("AGENTS.md")]
    [InlineData("CLAUDE.md")]
    [InlineData(".github/copilot-instructions.md")]
    [InlineData(".omp/AGENTS.md")]
    public void NamedInstructionsParticipateInLinkValidation(string file)
    {
        using var root = new Fixture();
        root.Put(file, "[Missing](missing.md)\n");
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.File == file && d.Code == "BROKEN_LINK");
    }

    [Fact]
    public void ArchiveAndPrivateRuntimeFilesAreNotActiveDocuments()
    {
        using var root = new Fixture();
        foreach (var file in new[] { "docs/archive/omp/spec.md", "docs/archive/omp/.omp/skills/old/SKILL.md", ".codex/config.toml", ".codex/auth.json", ".omp/auth.json", ".agents/private.json" })
            root.Put(file, "[Missing](missing.md)\n---\nid: obsolete\n---\n");
        Assert.Empty(ProjectValidator.Validate(root.Path));
    }

    [Fact]
    public void SkillNameMustMatchItsDirectory()
    {
        using var root = new Fixture();
        root.Put(".agents/skills/wrong/SKILL.md", "---\nname: example\ndescription: Inspect evidence\n---\n");
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.Code == "SKILL_PATH");
    }

    [Fact]
    public void CompletedFeatureWithoutTasksStillRequiresBacklogEvidence()
    {
        using var root = new Fixture();
        File.Delete(Path.Combine(root.Path, Fixture.Tasks));
        root.Replace("docs/backlog.md", "status: selected", "status: done");
        root.Replace("docs/specs/AIU-001/spec.md", "status: draft", "status: implemented");
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.Code == "DONE_WITHOUT_EVIDENCE");
        root.Append("docs/backlog.md", "- evidence: docs/check.txt\n");
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.Code == "DONE_WITHOUT_EVIDENCE");
        root.Put("docs/check.txt", "PASS: synthetic fixture evidence.\n");
        Assert.Empty(ProjectValidator.Validate(root.Path));
        root.Replace("docs/backlog.md", "docs/check.txt", "docs/../docs/check.txt");
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.Code == "UNSAFE_PATH");
    }

    [Fact]
    public void SequentialCompletionRequiresEvidenceAndCompletedDependencies()
    {
        using var root = new Fixture();
        const string header = "---\nid: AIU-001\nschema_version: 1\n---\n# Tasks\n";
        const string first = "### T-01 - First\n- status: pending\n- depends_on: []\n- acceptance: [AC-01]\n- evidence: docs/check.txt\n";
        const string second = "### T-02 - Second\n- status: done\n- depends_on: [T-01]\n- acceptance: [AC-01]\n- evidence: not-run\n";
        root.Put(Fixture.Tasks, header + first + second);
        var errors = ProjectValidator.Validate(root.Path);
        Assert.Contains(errors, d => d.Code == "DONE_WITHOUT_EVIDENCE");
        Assert.Contains(errors, d => d.Code == "MISSING_DEPENDENCY");
        root.Put("docs/check.txt", "PASS: synthetic fixture evidence.\n");
        root.Replace(Fixture.Tasks, "evidence: not-run", "evidence: docs/check.txt");
        root.Replace(Fixture.Tasks, "status: pending", "status: done");
        Assert.Empty(ProjectValidator.Validate(root.Path));
    }

    [Theory]
    [InlineData("ownership")]
    [InlineData("writes")]
    [InlineData("shared")]
    [InlineData("isolation")]
    [InlineData("agent")]
    public void ParallelWorkRequiresCompleteOwnershipMetadata(string field)
    {
        using var root = new Fixture();
        var text = File.ReadAllText(Path.Combine(root.Path, Fixture.Tasks));
        root.Put(Fixture.Tasks, string.Join('\n', text.Split('\n').Where(line => !line.StartsWith("- " + field + ":", StringComparison.Ordinal))));
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.Code == "REQUIRED_METADATA");
    }

    [Theory]
    [InlineData("AGENTS.md")]
    [InlineData("CONTRIBUTING.md")]
    [InlineData("CLAUDE.md")]
    [InlineData(".agents/skills/example/SKILL.md")]
    public void WriteWorkersCannotOwnCommonInstructions(string file)
    {
        using var root = new Fixture();
        root.Replace(Fixture.Tasks, "src/one/**", file);
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.Code == "PRIMARY_SHARED");
    }

    [Fact]
    public void OptionalTasksDoNotSkipDesignValidation()
    {
        using var root = new Fixture();
        File.Delete(Path.Combine(root.Path, Fixture.Tasks));
        root.Replace("docs/specs/AIU-001/design.md", "id: AIU-001", "id: AIU-999");
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.Code == "MISSING_REFERENCE");
    }

    [Theory]
    [InlineData("C:/outside/docs/check.txt")]
    [InlineData("../docs/check.txt")]
    [InlineData("/docs/check.txt")]
    [InlineData("docs/check.txt:stream")]
    public void EvidenceCannotHideAnUnsafePrefixOrSuffix(string evidence)
    {
        using var root = new Fixture();
        root.Put("docs/check.txt", "Synthetic evidence.\n");
        root.Replace(Fixture.Tasks, "evidence: not-run", "evidence: " + evidence + "; integrated");
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.Code == "UNSAFE_PATH");
    }

    [Theory]
    [InlineData("parallel", "sometimes")]
    [InlineData("isolation", "optional")]
    public void SuppliedSequentialEnumsAreValidated(string field, string value)
    {
        using var root = new Fixture();
        root.Put(Fixture.Tasks, "---\nid: AIU-001\nschema_version: 1\n---\n# Tasks\n### T-01 - Work\n- status: pending\n- depends_on: []\n- acceptance: [AC-01]\n- evidence: not-run\n- " + field + ": " + value + "\n");
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.Code == "INVALID_STATUS");
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
