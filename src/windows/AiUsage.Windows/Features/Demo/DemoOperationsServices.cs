using System.Runtime.CompilerServices;
using AiUsage.Features.CliImport;
using AiUsage.Features.Presentation;
using AiUsage.Features.Recovery;
using AiUsage.Features.SystemStatusPage;

namespace AiUsage.Features.Demo;

/// <summary>Built-in synthetic CLI candidates (F11). No file system discovery or credential reading.</summary>
internal sealed class DemoCliImportService(DemoState state) : ICliImportService
{
    private static readonly CliCandidate[] Candidates =
    [
        new("demo-new", "codex", "Codex CLI · default profile", "~/.codex/auth.json", true),
        new("demo-duplicate", "claude", "Claude CLI · default profile", "~/.claude/credentials", true),
        new("demo-unsupported", "copilot", "Copilot CLI · token cache", "~/.config/copilot/hosts.json", false),
        new("demo-failure", "codex", "Codex CLI · secondary profile", "~/.codex/profiles/alt.json", true),
    ];

    public const string ImportedAccountId = "demo-cli-codex-1";

    /// <summary>Demo-only: the next scan finds nothing.</summary>
    public bool NextScanEmpty { get; set; }

    public async IAsyncEnumerable<CliCandidate> DiscoverAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (NextScanEmpty)
        {
            NextScanEmpty = false;
            await state.DelayAsync(DemoLatency.CliCandidate, cancellationToken);
            yield break;
        }
        foreach (var candidate in Candidates)
        {
            await state.DelayAsync(DemoLatency.CliCandidate, cancellationToken);
            yield return candidate;
        }
    }

    public async IAsyncEnumerable<CliImportOutcome> ImportAsync(IReadOnlyList<string> candidateIds, bool reimport, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var id in candidateIds)
        {
            await state.DelayAsync(DemoLatency.CliImport, cancellationToken);
            yield return id switch
            {
                "demo-new" => ImportNew(reimport),
                "demo-duplicate" => new(id, CliImportOutcomeKind.Duplicate, "demo-claude-1", state.Account("demo-claude-1")?.Label ?? "Research"),
                "demo-unsupported" => new(id, CliImportOutcomeKind.Unsupported),
                _ => new(id, CliImportOutcomeKind.Failed),
            };
        }
    }

    private CliImportOutcome ImportNew(bool reimport)
    {
        if (reimport || state.Account(ImportedAccountId) is not null)
        {
            // D-089: re-import keeps the same identity, label, order and history.
            return new("demo-new", CliImportOutcomeKind.Reimported, ImportedAccountId);
        }
        var now = state.Clock.UtcNow;
        var account = new DemoAccount(ImportedAccountId, "codex", "Codex CLI",
            [new("cli-ctx", "Account", ContextKind.Account, [new("cli-g", "Usage limits",
            [
                new("cli-w1", "5-hour window", 88, ValueState.Known) { ResetsAt = now + TimeSpan.FromHours(4) },
                new("cli-w2", "Weekly window", 70, ValueState.Known) { ResetsAt = now + TimeSpan.FromHours(60), DurationSeconds = 604800 },
            ])])])
        { FetchedAt = now };
        state.World.Accounts.Add(account);
        state.World.Order.Add(account.Id);
        state.Publish();
        return new("demo-new", CliImportOutcomeKind.Imported, account.Id);
    }
}

internal sealed class DemoDiagnosticsService(DemoState state) : IDiagnosticsService
{
    private static readonly (string Name, HealthCheckResult Result, string? Finding)[] Steps =
    [
        ("Health_StorageReadable", HealthCheckResult.Ok, null),
        ("Health_SchemaMatches", HealthCheckResult.Ok, null),
        ("Health_ProviderSessions", HealthCheckResult.Warning, "Health_FindingSignIn"),
        ("Health_RefreshScheduler", HealthCheckResult.Ok, null),
        ("Health_NotificationChannel", HealthCheckResult.Warning, "Health_FindingNotifications"),
    ];

    public async IAsyncEnumerable<HealthCheckStep> RunHealthCheckAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var i = 0; i < Steps.Length; i++)
        {
            await state.DelayAsync(DemoLatency.HealthStep, cancellationToken);
            yield return new(i + 1, Steps.Length, Steps[i].Name, Steps[i].Result, Steps[i].Finding);
        }
    }

    public async Task<string> PreviewDiagnosticsAsync(CancellationToken cancellationToken)
    {
        await state.DelayAsync(DemoLatency.Preview, cancellationToken);
        var world = state.World;
        var providers = string.Join(", ", world.Accounts.GroupBy(a => a.ProviderId).Select(g => $"{g.Key} {g.Count()}"));
        return string.Join('\n',
            "diagnostics preview (sanitized)",
            $"build 1.0.0-demo · {world.SchemaLabel.ToLowerInvariant()} · theme {world.Theme.ToString().ToLowerInvariant()}",
            $"accounts {world.Accounts.Count} ({providers})",
            $"last refresh 12:00:00 · failures {world.Accounts.Count(a => a.Failure is not null)}",
            $"notifications: channel {(world.NotificationsAllowed ? "allowed" : "blocked")} · rules {world.Rules.Count(r => r.Scope == RuleScope.Global)} global, {world.Rules.Count(r => r.Scope != RuleScope.Global)} overrides",
            "excluded: credentials, tokens, paths, provider payloads");
    }

    public async Task<string> PreviewLogsAsync(CancellationToken cancellationToken)
    {
        await state.DelayAsync(DemoLatency.Preview, cancellationToken);
        return string.Join('\n',
            "12:00:00 info  refresh-all started (5 eligible)",
            "12:00:01 info  demo-codex-1 reading ok · fresh",
            "12:00:01 warn  demo-codex-2 NetworkFailure · cached reading kept",
            "12:00:01 info  demo-claude-1 reading ok · 2 contexts",
            "12:00:01 info  demo-copilot-1 connected · quota unavailable",
            "12:00:02 info  demo-antigravity-1 exhausted · awaiting reset observation");
    }
}

internal sealed class DemoRecoveryService(DemoState state) : IRecoveryService
{
    public RecoveryDetails Details => state.World.Recovery == RecoveryState.NewerSchema
        ? new(RecoveryState.NewerSchema, "Schema 5 (newer than supported Schema 4)", "ckpt-2026-09-14-2300 (Schema 4)", @"%LOCALAPPDATA%\AI Usage\data")
        : new(state.World.Recovery, state.World.SchemaLabel, "ckpt-2026-09-15-1130", @"%LOCALAPPDATA%\AI Usage\data");

    public async Task<UiCommandResult> RetryAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        if (state.World.Recovery == RecoveryState.NewerSchema)
            return UiCommandResult.Unsupported;
        if (state.World.Recovery == RecoveryState.RestoreFailed)
            return await RestoreCheckpointAsync("ckpt-2026-09-15-1130", progress, cancellationToken);
        progress.Report(0.05);
        foreach (var value in new[] { 0.30, 0.60, 0.85 })
        {
            await state.DelayAsync(DemoLatency.RecoveryStep, cancellationToken);
            progress.Report(value);
        }
        await state.DelayAsync(DemoLatency.RecoveryStep + DemoLatency.RecoveryStep, cancellationToken);
        // F13: the synthetic migration retry fails deterministically; data is not modified.
        return UiCommandResult.Failed(new("MigrationFailed", "Recovery_RetryFailed", null, true));
    }

    public Task<IReadOnlyList<RecoveryCheckpoint>> ListCheckpointsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RecoveryCheckpoint>>(
        [
            new("ckpt-2026-09-15-1130", new(2026, 9, 15, 11, 30, 0, TimeSpan.Zero), "Recovery_CheckpointBeforeMigration", 4, 5, 18240),
            new("ckpt-2026-09-14-2300", new(2026, 9, 14, 23, 0, 0, TimeSpan.Zero), "Recovery_CheckpointNightly", 4, 5, 17902),
        ]);

    public async Task<UiCommandResult> RestoreCheckpointAsync(string checkpointId, IProgress<double> progress, CancellationToken cancellationToken)
    {
        progress.Report(0.10);
        foreach (var value in new[] { 0.45, 0.80 })
        {
            await state.DelayAsync(DemoLatency.RestoreStep, cancellationToken);
            progress.Report(value);
        }
        await state.DelayAsync(DemoLatency.RestoreStep, cancellationToken);
        if (state.World.ScenarioId == "F13c")
            return UiCommandResult.Failed(new("RestoreVerificationFailed", "Recovery_RestoreFailed", null, true));
        progress.Report(1);
        state.World.Recovery = RecoveryState.None;
        state.Publish();
        return UiCommandResult.Succeeded;
    }

    public async Task<string> PreviewDiagnosticsAsync(CancellationToken cancellationToken)
    {
        await state.DelayAsync(DemoLatency.Preview, cancellationToken);
        return string.Join('\n',
            "diagnostics (sanitized)",
            $"build 1.0.0-demo · schema 4 · recovery {state.World.Recovery}",
            "migration step 3/5 · last ok 2026-09-15 11:30:02",
            "accounts 5 · history rows 18 240 · checkpoints 2",
            "no credentials, paths or tokens included");
    }
}
