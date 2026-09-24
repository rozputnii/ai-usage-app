using AiUsage.Features.Presentation;

namespace AiUsage.Features.Demo;

public enum DemoEntrySurface { None, AddAccountCli }

/// <summary><paramref name="FixtureId"/> is the docs fixtures.json scenario the seed implements.</summary>
public sealed record DemoScenario(string Id, string FixtureId, string Name, NavigationRequest Entry, DemoEntrySurface Surface = DemoEntrySurface.None);

/// <summary>Deterministic synthetic seeds for docs/specs/AIU-010-ui-ux/fixtures.json F01–F15. Ids use the demo- prefix.</summary>
internal static class DemoScenarioCatalog
{
    public static readonly DateTimeOffset T0 = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan H = TimeSpan.FromHours(1);
    public const string DefaultScenarioId = "F02";
    public const string LongLabel = "A deliberately long synthetic account label for wrapping and focus verification";

    public static IReadOnlyList<DemoScenario> Scenarios { get; } =
    [
        new("F01", "F01", "F01 First run", new(PageKey.Overview)),
        new("F02", "F02", "F02 Four providers, multiple accounts", new(PageKey.Overview)),
        new("F03", "F03", "F03 Measurement semantics", new(PageKey.Overview)),
        new("F04", "F04", "F04 Offline cache", new(PageKey.Overview)),
        new("F05", "F05", "F05 Connection", new(PageKey.Overview)),
        new("F06", "F06", "F06 Reauthentication and rate limit", new(PageKey.Overview)),
        new("F07", "F07", "F07 Contexts and shared pools", new(PageKey.Accounts, "demo-claude-1")),
        new("F08", "F08", "F08 Reset and native amounts", new(PageKey.Accounts, "demo-antigravity-1")),
        new("F09", "F09", "F09 History gaps and reset", new(PageKey.History, "demo-claude-1", WindowId: "cl-a-w1")),
        new("F10", "F10", "F10 Preferences", new(PageKey.Settings, Tab: SettingsTab.Monitoring)),
        new("F11", "F11", "F11 CLI discovery", new(PageKey.Overview), DemoEntrySurface.AddAccountCli),
        new("F12", "F12", "F12 Diagnostics and data", new(PageKey.Settings, Tab: SettingsTab.SystemStatus)),
        new("F13a", "F13", "F13 Recovery · interrupted migration", new(PageKey.Overview)),
        new("F13b", "F13", "F13 Recovery · newer schema", new(PageKey.Overview)),
        new("F13c", "F13", "F13 Recovery · restore failed", new(PageKey.Overview)),
        new("F14", "F14", "F14 Updates", new(PageKey.Settings, Tab: SettingsTab.Updates)),
        new("F14a", "F14", "F14 Compatibility blocked", new(PageKey.Settings, Tab: SettingsTab.Updates)),
        new("F14b", "F14", "F14 Security blocked", new(PageKey.Settings, Tab: SettingsTab.Updates)),
        new("F15", "F15", "F15 Stress · 20 accounts × 12 groups", new(PageKey.Overview)),
    ];

    public static DemoScenario Find(string id) => Scenarios.FirstOrDefault(s => s.Id == id) ?? throw new ArgumentException("Unknown demo scenario.", nameof(id));

    public static DemoWorld Build(string id)
    {
        var world = new DemoWorld { ScenarioId = id };
        switch (id)
        {
            case "F01":
                break;
            case "F03":
                world.Accounts.Add(Semantics());
                break;
            case "F04":
                world.Accounts.AddRange(F02());
                var work = world.Accounts[1];
                work.Failure = null;
                Window(work, "c2-w1").Remaining = 42;
                Window(work, "c2-w1").ProviderRemaining = 42;
                break;
            case "F06":
                world.Accounts.AddRange(F02());
                var a = world.Accounts[0];
                a.Connection = ConnectionState.ReauthRequired;
                a.Freshness = Freshness.Stale;
                a.FetchedAt = T0 - 5 * H;
                a.Failure = new(FailureKinds.InvalidGrant, "Failure_InvalidGrant", null, true);
                var b = world.Accounts[1];
                b.Freshness = Freshness.Fresh;
                b.FetchedAt = T0;
                b.Failure = new(FailureKinds.RateLimited, "Failure_RateLimited", T0 + TimeSpan.FromMinutes(2), true);
                b.FailNext = 0;
                break;
            case "F15":
                world.Accounts.AddRange(Stress());
                break;
            default:
                world.Accounts.AddRange(F02());
                break;
        }
        world.Recovery = id switch { "F13a" => RecoveryState.Interrupted, "F13b" => RecoveryState.NewerSchema, "F13c" => RecoveryState.RestoreFailed, _ => RecoveryState.None };
        world.Compatibility = id switch { "F14a" => CompatibilityState.CompatibilityBlocked, "F14b" => CompatibilityState.SecurityBlocked, _ => CompatibilityState.Normal };
        world.LastUpdateCheck = T0 - 2 * H;
        world.Order.AddRange(world.Accounts.Select(account => account.Id));
        return world;
    }

    private static DemoWindow Window(DemoAccount account, string id) => account.AllWindows.First(w => w.Id == id);

    private static DemoWindow Win(string id, string label, double? remaining, DateTimeOffset? reset = null, double duration = 18000, ValueState? state = null, NativeAmount? absolute = null) =>
        new(id, label, remaining, state ?? (remaining is null ? ValueState.Unknown : remaining == 0 ? ValueState.Exhausted : ValueState.Known))
        {
            ResetsAt = reset,
            DurationSeconds = duration,
            Absolute = absolute,
        };

    private static DemoGroup Group(string id, string label, params DemoWindow[] windows) => new(id, label, [.. windows]);

    private static DemoContext AccountContext(string id, params DemoGroup[] groups) => new(id, "Account", ContextKind.Account, [.. groups]);

    private static DemoAccount Account(string id, string providerId, string label, params DemoContext[] contexts) =>
        new(id, providerId, label, [.. contexts]) { FetchedAt = T0 };

    private static DemoGroup Standard(string prefix, double first, double weekly) => Group(prefix + "-g1", "Usage limits",
        Win(prefix + "-w1", "5-hour window", first, T0 + 3 * H),
        Win(prefix + "-w2", "Weekly window", weekly, T0 + 69 * H, 604800));

    private static DemoGroup SharedPool(string prefix) => new(prefix + "-shared", "Shared pool",
        [Win("demo-shared-1-w", "Pool window", 12, T0 + TimeSpan.FromMinutes(1), absolute: new NativeAmount("1250", null, null, "tokens"))], "demo-shared-1");

    internal static List<DemoAccount> F02()
    {
        var personal = Account("demo-codex-1", "codex", "Personal", AccountContext("demo-codex-1-ctx", Standard("c1", 72, 61)));
        var work = Account("demo-codex-2", "codex", "Work", AccountContext("demo-codex-2-ctx", Standard("c2", 20, 44)));
        work.Freshness = Freshness.Stale;
        work.FetchedAt = T0 - 18 * H;
        work.Failure = new(FailureKinds.NetworkFailure, "Failure_NetworkFailure", T0 + TimeSpan.FromSeconds(100), true);
        work.FailNext = 1;
        Window(work, "c2-w1").NextObservation = 39;
        Window(work, "c2-w2").NextObservation = 44;

        var research = Account("demo-claude-1", "claude", "Research",
            new DemoContext("demo-workspace-a", "Workspace A", ContextKind.Workspace,
            [
                Group("cl-a-g1", "Usage limits",
                    Win("cl-a-w1", "Session window", 8, T0 + 2 * H + TimeSpan.FromMinutes(14)),
                    Win("cl-a-w2", "Weekly window", 34, T0 + 69 * H, 604800)),
                SharedPool("cl-a"),
                Group("cl-a-g3", "Model-specific",
                    Win("cl-a-m1", "Model A · 7d", 41, T0 + 69 * H, 604800),
                    Win("cl-a-m2", "Model B · 7d", 77, T0 + 69 * H, 604800),
                    Win("cl-a-m3", "Model C · 7d", 90, T0 + 69 * H, 604800)),
            ]),
            new DemoContext("demo-workspace-b", "Workspace B", ContextKind.Workspace,
            [
                Group("cl-b-g1", "Usage limits",
                    Win("cl-b-w1", "Session window", 55, T0 + 2 * H + TimeSpan.FromMinutes(14)),
                    Win("cl-b-w2", "Weekly window", 60, T0 + 69 * H, 604800)),
                SharedPool("cl-b"),
            ]));
        research.Extensions.Add(new(ExtensionKind.ExtraUsage, "Extra usage", true, "1250", 2, "USD", "1250", null, null) { HasExplicitNullLimit = true });
        research.Extensions.Add(new(ExtensionKind.Credits, "Credits", null, "1250", null, null, null, null, null));

        var development = Account("demo-copilot-1", "copilot", "Development",
            AccountContext("demo-copilot-1-ctx", Group("cp-g1", "Usage", Win("cp-w1", "Remaining", null, state: ValueState.Unavailable))));
        development.Plan = null;

        var experiments = Account("demo-antigravity-1", "antigravity", "Experiments",
            AccountContext("demo-antigravity-1-ctx", Group("ag-g1", "Usage limits", Win("ag-w1", "Session window", 0, T0 + TimeSpan.FromMinutes(1)))));
        experiments.FetchedAt = T0 - TimeSpan.FromMinutes(2);

        return [personal, work, research, development, experiments];
    }

    private static DemoAccount Semantics()
    {
        var account = Account("demo-sem-1", "codex", "Semantics sample", AccountContext("sem-ctx", Group("sem-g", "Every value state",
            Win("sem-1", "Known", 72, T0 + 3 * H),
            Win("sem-2", "Unknown", null),
            Win("sem-3", "Unlimited (explicit)", null, state: ValueState.Unlimited),
            Win("sem-4", "Exhausted", 0, T0 + TimeSpan.FromMinutes(50)),
            Win("sem-5", "Unavailable", null, state: ValueState.Unavailable))));
        account.Freshness = Freshness.Stale;
        account.FetchedAt = T0 - 6 * H;
        return account;
    }

    private static IEnumerable<DemoAccount> Stress()
    {
        string[] providers = ["codex", "claude", "copilot", "antigravity"];
        for (var i = 0; i < 20; i++)
        {
            var prefix = "st" + i;
            var groups = new List<DemoGroup>();
            for (var g = 0; g < 12; g++)
            {
                var label = g == 0 ? "Usage limits" : $"Model group {g}";
                var windows = new List<DemoWindow>();
                for (var w = 0; w < 2; w++)
                {
                    var value = (i * 37 + g * 23 + w * 11) % 101;
                    var name = g == 0 ? (w == 0 ? "Session window" : "Weekly window") : $"Model {g} · {(w == 0 ? "5h" : "7d")}";
                    windows.Add(Win($"{prefix}-g{g}-w{w}", name, value, T0 + (1 + w * 6) * H, w == 0 ? 18000 : 604800));
                }
                groups.Add(new DemoGroup($"{prefix}-g{g}", label, windows));
            }
            yield return Account("demo-stress-" + i, providers[i % 4], i == 3 ? LongLabel : $"Account {i + 1}",
                new DemoContext(prefix + "-ctx", "Account", ContextKind.Account, groups));
        }
    }

    public static IReadOnlyList<HistoryPoint> F09Points(double currentRemaining) =>
    [
        new(T0 - 4 * H, 30, HistoryCoverage.Observed, "a"),
        new(T0 - 3 * H, null, HistoryCoverage.Gap, "a"),
        new(T0 - 2 * H, 0, HistoryCoverage.Observed, "a"),
        new(T0 - H, 100, HistoryCoverage.Observed, "b"),
        new(T0, currentRemaining, HistoryCoverage.Observed, "b"),
    ];
}
