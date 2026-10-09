using AiUsage.ProjectValidation;
using Xunit;

namespace AiUsage.ProjectValidation.Tests;

public sealed class ValidatorTests
{
    [Theory]
    [InlineData("docs/custom/tasks.md\u00ad")]
    [InlineData(".\u00adagents/custom.md")]
    public void PrimaryOwnershipUsesLiteralPathSegments(string path)
    {
        var previousCulture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
            using var root = new Fixture();
            root.Replace(Fixture.Tasks, "src/one/**", path);
            Assert.Empty(ProjectValidator.Validate(root.Path));
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = previousCulture; }
    }

    [Fact]
    public void SharedSkillDoesNotRequireAnOmpCopy()
    {
        using var root = new Fixture();
        root.Put(".agents/skills/example/SKILL.md", "---\nname: example\ndescription: Inspect evidence\n---\n# Example\n");
        Assert.Empty(ProjectValidator.Validate(root.Path));
    }

    [Theory]
    [InlineData("T-NEW")]
    [InlineData("T-NEW-2")]
    public void BranchWorkMayUsePlaceholderIdsThatTheFinalCheckRefuses(string placeholder)
    {
        using var root = new Fixture();
        foreach (var file in new[] { "docs/product/goals.md", "docs/backlog.md", "docs/specs/T-001/spec.md", "docs/specs/T-001/design.md", Fixture.Tasks })
            root.Replace(file, "T-001", placeholder);
        Assert.Empty(ProjectValidator.Validate(root.Path));
        Assert.Contains(ProjectValidator.Validate(root.Path, final: true), d => d.Code == "PLACEHOLDER_ID" && d.File == "docs/backlog.md");
    }

    [Fact]
    public void FinalCheckRefusesAPlaceholderDecisionButNotQuotedRuleText()
    {
        using var root = new Fixture();
        root.Put("docs/decisions/accepted.md", "# Decisions\n\nUse `R-NEW` and `T-NEW` until the merge.\n\n```text\n### R-NEW - Example\n```\n");
        Assert.Empty(ProjectValidator.Validate(root.Path, final: true));
        root.Append("docs/decisions/accepted.md", "\n### R-NEW-1 - Owner direction\nText.\n");
        Assert.Empty(ProjectValidator.Validate(root.Path));
        Assert.Contains(ProjectValidator.Validate(root.Path, final: true), d => d.Code == "PLACEHOLDER_ID" && d.Task == "R-NEW-1");
    }

    [Fact]
    public void DoneIndexRowsAreBacklogItems()
    {
        using var root = new Fixture();
        root.Put("docs/evidence.md", "# Evidence\n");
        root.Replace("docs/product/goals.md", "- scope: T-001", "- scope: T-001, T-002");
        root.Replace("docs/backlog.md", "- depends_on: []", "- depends_on: [T-002]");
        root.Append("docs/backlog.md", "\n## Done index\n\n| Item | Title | Status | Goal | Evidence |\n| --- | --- | --- | --- | --- |\n| T-002 | Earlier work | done | G-001 | docs/evidence.md |\n");
        Assert.Empty(ProjectValidator.Validate(root.Path));
        root.Replace("docs/backlog.md", "| done | G-001 | docs/evidence.md |", "| done | G-001 | docs/missing.md |");
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.Task == "T-002" && d.Code == "DONE_WITHOUT_EVIDENCE");
        root.Replace("docs/backlog.md", "| done | G-001 | docs/missing.md |", "| ready | G-001 | docs/evidence.md |");
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.Task == "T-002" && d.Code == "INVALID_STATUS");
    }

    [Fact]
    public void AcceptanceResultsMustCoverEverySpecCriterion()
    {
        using var root = new Fixture();
        root.Append("docs/specs/T-001/spec.md", "- AC-02: Second behavior.\n");
        root.Put("docs/specs/T-001/verification.md", "# Verification\n\nFree-form history mentioning AC-01 only.\n");
        Assert.Empty(ProjectValidator.Validate(root.Path));
        root.Append("docs/specs/T-001/verification.md", "\n## Acceptance results\n\n| AC | Verdict | Evidence |\n| --- | --- | --- |\n| AC-01 | PASS | C4 |\n");
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.Task == "AC-02" && d.Code == "AC_COVERAGE");
        root.Append("docs/specs/T-001/verification.md", "| AC-02 | NOT_RUN (post-deploy owner check) | R-190 |\n");
        Assert.Empty(ProjectValidator.Validate(root.Path));
        root.Append("docs/specs/T-001/verification.md", "| AC-03 | maybe | none |\n");
        var errors = ProjectValidator.Validate(root.Path);
        Assert.Contains(errors, d => d.Task == "AC-03" && d.Code == "AC_REFERENCE");
        Assert.Contains(errors, d => d.Task == "AC-03" && d.Code == "INVALID_STATUS");
    }

    [Fact]
    public void DecisionIdsAreUniqueAndAmendmentPointersResolve()
    {
        using var root = new Fixture();
        root.Put("docs/decisions/accepted.md", "# Decisions\n\n### R-001 - First\nText.\n\nAmended by R-002 (2026-10-09): changed.\n\n### R-002 - Second\nText.\n");
        Assert.Empty(ProjectValidator.Validate(root.Path));
        root.Append("docs/decisions/accepted.md", "\n### R-002 - Again\nText.\n");
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.Task == "R-002" && d.Code == "DUPLICATE_ID");
        root.Put("docs/decisions/accepted.md", "# Decisions\n\n### R-001 - First\nSuperseded by R-009 (2026-10-09): gone.\n");
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.Task == "R-009" && d.Code == "MISSING_REFERENCE");
    }

    [Fact]
    public void LegacyPlaceholdersAndHeadingsAreRefused()
    {
        using var root = new Fixture();
        root.Put("docs/decisions/accepted.md", "# Decisions\n\n### R-001 - First\nText.\n");
        root.Append("docs/backlog.md", "\n## AIU-NEW - Old-style item\n- goal: G-001\n");
        root.Append("docs/decisions/accepted.md", "\n### D-NEW - Old-style decision\nText.\n");
        var errors = ProjectValidator.Validate(root.Path);
        Assert.Contains(errors, d => d.Task == "AIU-NEW" && d.Code == "INVALID_ID");
        Assert.Contains(errors, d => d.Task == "D-NEW" && d.Code == "INVALID_ID");
        var final = ProjectValidator.Validate(root.Path, final: true);
        Assert.Contains(final, d => d.Task == "AIU-NEW" && d.Code == "PLACEHOLDER_ID");
        Assert.Contains(final, d => d.Task == "D-NEW" && d.Code == "PLACEHOLDER_ID");
    }

    [Fact]
    public void DecisionChecksCoverBothRegistersAndThreeDigitIds()
    {
        using var root = new Fixture();
        root.Put("docs/decisions/superseded.md", "# Superseded\n\n### R-01 - Two digits\nText.\nAmended by R-404 (2026-10-09): x.\n");
        var errors = ProjectValidator.Validate(root.Path);
        Assert.Contains(errors, d => d.File == "docs/decisions/superseded.md" && d.Task == "R-01" && d.Code == "INVALID_ID");
        Assert.Contains(errors, d => d.File == "docs/decisions/superseded.md" && d.Task == "R-404" && d.Code == "MISSING_REFERENCE");
    }

    [Fact]
    public void DoneIndexRowMayNotRepeatALiveItem()
    {
        using var root = new Fixture();
        root.Put("docs/evidence.md", "# Evidence\n");
        root.Append("docs/backlog.md", "\n## Done index\n\n| Item | Title | Status | Goal | Evidence |\n| --- | --- | --- | --- | --- |\n| T-001 | Feature | done | G-001 | docs/evidence.md |\n");
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.Task == "T-001" && d.Code == "DUPLICATE_ID");
    }

    [Theory]
    [InlineData("T-NEW")]
    [InlineData("T-NEW-2")]
    public void DoneIndexMayHoldAPlaceholderRowThatTheFinalCheckRefuses(string placeholder)
    {
        using var root = new Fixture();
        root.Put("docs/evidence.md", "# Evidence\n");
        root.Replace("docs/product/goals.md", "- scope: T-001", $"- scope: T-001, {placeholder}");
        root.Append("docs/backlog.md", $"\n## Done index\n\n| Item | Title | Status | Goal | Evidence |\n| --- | --- | --- | --- | --- |\n| {placeholder} | Branch work | done | G-001 | docs/evidence.md |\n");
        Assert.Empty(ProjectValidator.Validate(root.Path));
        Assert.Contains(ProjectValidator.Validate(root.Path, final: true), d => d.Task == placeholder && d.Code == "PLACEHOLDER_ID");
    }

    [Theory]
    [InlineData("| T-02 | Two digits | done | G-001 | docs/evidence.md |", "T-02", "INVALID_ID")]
    [InlineData("| [T-002](docs/evidence.md) | Linked | done | G-001 | docs/evidence.md |", "[T-002](docs/evidence.md)", "REQUIRED_METADATA")]
    [InlineData("| T-002 | Extra | column | done | G-001 | docs/evidence.md |", "T-002", "REQUIRED_METADATA")]
    [InlineData("| T-002 | Missing evidence | done | G-001 |", "T-002", "REQUIRED_METADATA")]
    public void MalformedDoneIndexRowsAreReported(string row, string task, string code)
    {
        using var root = new Fixture();
        root.Put("docs/evidence.md", "# Evidence\n");
        root.Append("docs/backlog.md", $"\n## Done index\n\n| Item | Title | Status | Goal | Evidence |\n| --- | --- | --- | --- | --- |\n{row}\n");
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.File == "docs/backlog.md" && d.Task == task && d.Code == code);
    }

    [Fact]
    public void DuplicateDoneIndexRowsAreReported()
    {
        using var root = new Fixture();
        root.Put("docs/evidence.md", "# Evidence\n");
        root.Replace("docs/product/goals.md", "- scope: T-001", "- scope: T-001, T-002");
        root.Append("docs/backlog.md", "\n## Done index\n\n| Item | Title | Status | Goal | Evidence |\n| --- | --- | --- | --- | --- |\n| T-002 | One | done | G-001 | docs/evidence.md |\n| T-002 | Two | done | G-001 | docs/evidence.md |\n");
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.Task == "T-002" && d.Code == "DUPLICATE_ID");
    }

    [Fact]
    public void AcceptanceResultsAcceptAnOwnerReportedVerdict()
    {
        using var root = new Fixture();
        root.Put("docs/specs/T-001/verification.md", "# Verification\n\n## Acceptance results\n\n| AC | Verdict | Evidence |\n| --- | --- | --- |\n| AC-01 | owner-reported PASS (2026-10-10) | installed Preview |\n");
        Assert.Empty(ProjectValidator.Validate(root.Path));
        root.Replace("docs/specs/T-001/verification.md", "owner-reported PASS", "owner-reported maybe");
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.Task == "AC-01" && d.Code == "INVALID_STATUS");
    }

    [Fact]
    public void DecisionPointersResolveAcrossBothRegisters()
    {
        using var root = new Fixture();
        root.Put("docs/decisions/accepted.md", "# Decisions\n\n### R-002 - Current\nText.\n\nAmended by R-003 (2026-10-09): narrowed.\n");
        root.Put("docs/decisions/superseded.md", "# Superseded\n\n### R-001 - Old\nText.\n\nSuperseded by R-002 (2026-10-09): replaced.\n\n### R-003 - Later retired\nText.\n");
        Assert.Empty(ProjectValidator.Validate(root.Path));
        root.Append("docs/decisions/superseded.md", "\nSuperseded by R-009 (2026-10-09): missing.\n");
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.File == "docs/decisions/superseded.md" && d.Task == "R-009" && d.Code == "MISSING_REFERENCE");
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
        root.Put(Fixture.Tasks, "---\nid: T-001\nschema_version: 1\n---\n# Tasks\n### T-001.1 - Implement behavior\n- status: pending\n- depends_on: []\n- acceptance: [AC-01]\n- evidence: not-run\n");
        Assert.Empty(ProjectValidator.Validate(root.Path));
    }

    [Fact]
    public void MinimalFullRootAcceptsSharedFeatureIdsDeferredWorkAndPlannedPaths()
    {
        using var root = new Fixture();
        root.Append("docs/specs/T-001/spec.md", "\nPlanned output: `future/not-created.md`.\n");
        Assert.Empty(ProjectValidator.Validate(root.Path));
    }


    [Theory]
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
    [InlineData("docs/backlog.md", "T-001 -", "T-1 -", "INVALID_ID")]
    [InlineData("docs/specs/T-001/tasks.md", "T-001.1 -", "T-1 -", "INVALID_ID")]
    [InlineData("docs/backlog.md", "status: selected", "status: magic", "INVALID_STATUS")]
    [InlineData("docs/specs/T-001/tasks.md", "status: pending", "status: magic", "INVALID_STATUS")]
    [InlineData("docs/specs/T-001/spec.md", "status: draft", "status: done", "INVALID_STATUS")]
    [InlineData("docs/backlog.md", "goal: G-001", "goal: G-999", "MISSING_GOAL")]
    [InlineData("docs/product/goals.md", "scope: T-001", "scope: T-999", "MISSING_REFERENCE")]
    [InlineData("docs/product/goals.md", "scope: T-001", "scope: []", "GOAL_SCOPE")]
    [InlineData("docs/backlog.md", "depends_on: []", "depends_on: [T-999]", "MISSING_DEPENDENCY")]
    [InlineData("docs/specs/T-001/tasks.md", "depends_on: []", "depends_on: [T-001.99]", "MISSING_DEPENDENCY")]
    [InlineData("docs/specs/T-001/tasks.md", "depends_on: []", "depends_on: [T-001.1]", "DEPENDENCY_CYCLE")]
    [InlineData("docs/backlog.md", "depends_on: []", "depends_on: [T-001]", "DEPENDENCY_CYCLE")]
    [InlineData("docs/specs/T-001/tasks.md", "AC-01", "AC-99", "AC_REFERENCE")]
    [InlineData("docs/specs/T-001/spec.md", "scope_version: 1\n", "", "REQUIRED_METADATA")]
    [InlineData("docs/specs/T-001/tasks.md", "status: pending", "status: done", "DONE_WITHOUT_EVIDENCE")]
    [InlineData("docs/specs/T-001/tasks.md", "isolation: required", "isolation: none", "WORKER_ISOLATION")]
    [InlineData("docs/specs/T-001/tasks.md", "shared: []", "shared: [src/contracts]", "PRIMARY_SHARED")]
    [InlineData("docs/specs/T-001/spec.md", "status: draft", "status: implemented", "LIFECYCLE_STATUS")]
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
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.Code == "UNSAFE_PATH" && d.Task == "T-001.1");
    }

    [Theory]
    [InlineData("SRC\\ONE\\file.cs", "other")]
    [InlineData("src/*/file.cs", "other")]
    [InlineData("different/**", "domain-one")]
    public void ConcurrentOwnershipIsConservative(string writes, string ownership)
    {
        using var root = new Fixture();
        root.Append(Fixture.Tasks, Fixture.Task("T-001.2", writes, ownership));
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.Code == "PARALLEL_OVERLAP");
    }

    [Fact]
    public void DependencyOrderingSerializesOverlappingWorkers()
    {
        using var root = new Fixture();
        root.Append(Fixture.Tasks, Fixture.Task("T-001.2", "src/one/**", "domain-one").Replace("depends_on: []", "depends_on: [T-001.1]"));
        Assert.Empty(ProjectValidator.Validate(root.Path));
    }

    [Theory]
    [InlineData("docs/backlog.md", "\n## T-001 - Duplicate\n", "DUPLICATE_ID")]
    [InlineData("docs/product/goals.md", "\n## G-001 - Duplicate\n", "DUPLICATE_ID")]
    [InlineData("docs/specs/T-001/spec.md", "\n- AC-01: Duplicate.\n", "DUPLICATE_ID")]
    [InlineData("docs/specs/T-001/tasks.md", "\n### T-001.1 - Duplicate\n", "DUPLICATE_ID")]
    [InlineData("docs/specs/T-001/spec.md", "\n[Missing](missing.md)\n", "BROKEN_LINK")]
    [InlineData("docs/specs/T-001/spec.md", "\n[Missing][ref]\n[ref]: missing.md\n", "BROKEN_LINK")]
    public void DocumentCorruptionsAreRejected(string file, string content, string code)
    {
        using var root = new Fixture();
        root.Append(file, content);
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.File == file && d.Code == code);
    }

    [Fact]
    public void LinkIntoIgnoredLocalRootIsBrokenAndNamesTheTarget()
    {
        using var root = new Fixture();
        root.Put(".ai-usage-local/evidence/a.png", "Synthetic local evidence.\n");
        root.Put("docs/x.md", "[Evidence](../.ai-usage-local/evidence/a.png)\n");
        var broken = Assert.Single(ProjectValidator.Validate(root.Path), d => d.Code == "BROKEN_LINK");
        Assert.Equal("docs/x.md", broken.File);
        Assert.Contains(".ai-usage-local/evidence/a.png", broken.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LinkToAnExistingRepositoryDocumentIsAccepted()
    {
        using var root = new Fixture();
        root.Put("docs/y.md", "# Y\n");
        root.Put("docs/x.md", "[Y](y.md)\n");
        Assert.Empty(ProjectValidator.Validate(root.Path));
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
        root.Replace("docs/specs/T-001/spec.md", "status: draft", "status: implemented");
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
        const string header = "---\nid: T-001\nschema_version: 1\n---\n# Tasks\n";
        const string first = "### T-001.1 - First\n- status: pending\n- depends_on: []\n- acceptance: [AC-01]\n- evidence: docs/check.txt\n";
        const string second = "### T-001.2 - Second\n- status: done\n- depends_on: [T-001.1]\n- acceptance: [AC-01]\n- evidence: not-run\n";
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
        root.Replace("docs/specs/T-001/design.md", "id: T-001", "id: T-999");
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
        root.Put(Fixture.Tasks, "---\nid: T-001\nschema_version: 1\n---\n# Tasks\n### T-001.1 - Work\n- status: pending\n- depends_on: []\n- acceptance: [AC-01]\n- evidence: not-run\n- " + field + ": " + value + "\n");
        Assert.Contains(ProjectValidator.Validate(root.Path), d => d.Code == "INVALID_STATUS");
    }

    private sealed class Fixture : IDisposable
    {
        public const string Tasks = "docs/specs/T-001/tasks.md";
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "aiu-validator-" + Guid.NewGuid().ToString("N"));
        public Fixture()
        {
            Put("docs/product/goals.md", "---\nschema_version: 1\nactive_goal: G-001\n---\n# Goals\n## G-001 - Goal\n- status: selected\n- scope: T-001\n- outcome: Valid outcome.\n- success: Observable success.\n");
            Put("docs/backlog.md", "---\nschema_version: 1\n---\n# Backlog\n## T-001 - Feature\n- goal: G-001\n- status: selected\n- depends_on: []\n- trigger: now\n- outcome: Valid outcome.\n");
            var metadata = "---\nid: T-001\ntype: infrastructure\nstatus: draft\ngoal: G-001\nscope_version: 1\napproval_basis: derived-within-authorized-goal\n---\n";
            Put("docs/specs/T-001/spec.md", metadata + "# Feature\n## Acceptance criteria\n- AC-01: Observable behavior.\n");
            Put("docs/specs/T-001/design.md", metadata + "# Design\nSimple implementation.\n");
            Put(Tasks, "---\nid: T-001\nschema_version: 1\n---\n# Tasks\n" + Task("T-001.1", "src/one/**", "domain-one"));
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
