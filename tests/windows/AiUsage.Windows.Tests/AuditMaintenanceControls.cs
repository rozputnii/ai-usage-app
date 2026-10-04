using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using FlaUI.Core.Definitions;
using FlaUI.Core.WindowsAPI;
using Xunit;

namespace AiUsage.Windows.Tests;

public sealed partial class AuditWindows
{
    private const string LegacyMaintenancePreferences = "{\"Version\":1,\"Theme\":2,\"AlwaysOnTop\":true,\"Labels\":{\"opaque/provider\":\"SYNTHETIC checkpoint\"},\"future\":{\"raw\":[null,7]}}";

    [Fact]
    public void ProductionStorageLeaseBlocksAccountsAndPhysicalRetryRecovers()
    {
        DesktopTestEnvironment.RequireUnlockedDesktop(); Assert.Equal("WDAGUtilityAccount", Environment.UserName);
        var root = MaintenanceRoot();
        using var lease = new FileStream(Path.Combine(root, "state.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        using var session = Session.FromFixture(MaintenanceFixture(), "maintenance-busy", root);
        RequireRecovery(session, "already using these local data");
        Assert.DoesNotContain("InitializeCompleted", session.Receipts, StringComparison.Ordinal);
        AssertSignInUnavailable(session);
        session.Click("Settings");
        Assert.False(session.Find(e => e.Properties.Name.ValueOrDefault == "Restore legacy preferences").IsEnabled);
        session.Click("Retry recovery");
        session.Find(e => e.Properties.Name.ValueOrDefault == "Action unavailable; local data is preserved");
        Assert.DoesNotContain("InitializeCompleted", session.Receipts, StringComparison.Ordinal);
        session.Capture("REC-02-busy-retry-failed");
        lease.Dispose(); session.Click("Retry recovery"); RequireFirstRunReady(session);
        Assert.Contains("InitializeCompleted", session.Receipts, StringComparison.Ordinal);
        session.Capture("REC-02-after-lease-retry"); session.Exit();
    }

    [Fact]
    public void ProductionCheckpointRetryRestoreFailureSuccessAndNewerSchemaUsePhysicalControls()
    {
        DesktopTestEnvironment.RequireUnlockedDesktop(); Assert.Equal("WDAGUtilityAccount", Environment.UserName);
        var root = MaintenanceRoot(); var fixture = MaintenanceFixture();
        var legacy = Path.Combine(root, "appearance.v1.json");
        var target = Path.Combine(root, "preferences", "appearance.v1.json");
        File.WriteAllText(legacy, LegacyMaintenancePreferences);
        using (var barrier = new FileStream(Path.Combine(root, "layout.v1.json.new"), FileMode.CreateNew, FileAccess.Write, FileShare.None))
        using (var interrupted = Session.FromFixture(fixture, "maintenance-interrupted", root))
        {
            RequireRecovery(interrupted, "Local data needs recovery"); AssertSignInUnavailable(interrupted);
            interrupted.Click("Settings"); interrupted.Click("Retry recovery");
            interrupted.Find(e => e.Properties.Name.ValueOrDefault == "Action unavailable; local data is preserved");
            Assert.True(File.Exists(Path.Combine(root, "maintenance", "journal.v1.json")));
            Assert.Equal(LegacyMaintenancePreferences, File.ReadAllText(legacy));
            interrupted.Capture("REC-03-production-retry-failed"); interrupted.Exit();
        }
        var checkpointPath = Path.Combine(root, "maintenance", "checkpoint.v1.bin");
        var checkpoint = File.ReadAllBytes(checkpointPath);
        using (var retry = Session.FromFixture(fixture, "maintenance-retry", root))
        {
            RequireRecovery(retry, "Local data needs recovery"); retry.Click("Settings"); retry.Click("Retry recovery");
            RequireFirstRunReady(retry); Assert.Equal(LegacyMaintenancePreferences, File.ReadAllText(target));
            Assert.Equal(checkpoint, File.ReadAllBytes(checkpointPath)); retry.Capture("REC-03-production-retry-succeeded"); retry.Exit();
        }
        File.WriteAllText(target, "SYNTHETIC corrupt preferences");
        using (var restore = Session.FromFixture(fixture, "maintenance-restore", root))
        {
            RequireRecovery(restore, "Local data needs recovery"); restore.Click("Settings"); restore.Click("Preview diagnostics");
            restore.Find(e => (e.Properties.Name.ValueOrDefault ?? "").Contains("Legacy preferences checkpoint: True", StringComparison.Ordinal));
            restore.Capture("REC-04-production-restore-preview");
            using (var barrier = new FileStream(target + ".new", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                restore.Click("Restore legacy preferences");
                restore.Find(e => e.Properties.Name.ValueOrDefault == "Action unavailable; local data is preserved");
                Assert.Equal("SYNTHETIC corrupt preferences", File.ReadAllText(target));
                Assert.Equal(checkpoint, File.ReadAllBytes(checkpointPath)); restore.Capture("REC-04-production-restore-failed");
            }
            restore.Click("Restore legacy preferences"); RequireFirstRunReady(restore);
            Assert.Equal(LegacyMaintenancePreferences, File.ReadAllText(target));
            Assert.Equal(checkpoint, File.ReadAllBytes(checkpointPath)); restore.Capture("REC-04-production-restore-succeeded"); restore.Exit();
        }
        File.WriteAllText(Path.Combine(root, "layout.v1.json"), "{\"Version\":1,\"Layout\":99}");
        using var newer = Session.FromFixture(fixture, "maintenance-newer", root);
        RequireRecovery(newer, "written by a newer app"); AssertSignInUnavailable(newer); newer.Click("Settings");
        Assert.False(newer.Find(e => e.Properties.Name.ValueOrDefault == "Retry recovery").IsEnabled);
        Assert.False(newer.Find(e => e.Properties.Name.ValueOrDefault == "Restore legacy preferences").IsEnabled);
        newer.Click("Export recovery summary");
        var export = Path.Combine(root, "recovery-diagnostics.txt"); Assert.True(Session.Wait(() => File.Exists(export)));
        var text = File.ReadAllText(export); Assert.Contains("Condition: NewerSchema", text, StringComparison.Ordinal);
        Assert.DoesNotContain(root, text, StringComparison.Ordinal); Assert.DoesNotContain("opaque/provider", text, StringComparison.Ordinal);
        Assert.Equal(LegacyMaintenancePreferences, File.ReadAllText(target));
        newer.Capture("REC-04-production-newer-schema-blocked"); newer.Exit();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ProductionDeletionCancelConfirmAndPendingIntentRestartOnlySyntheticState(bool pending)
    {
        DesktopTestEnvironment.RequireUnlockedDesktop(); Assert.Equal("WDAGUtilityAccount", Environment.UserName);
        var root = MaintenanceRoot(); var fixture = MaintenanceFixture();
        var provider = Path.Combine(root, "providers", "claude.state"); Directory.CreateDirectory(Path.GetDirectoryName(provider)!);
        File.WriteAllText(provider, "SYNTHETIC opaque provider sentinel");
        var preserved = Path.Combine(root, "preserved-export.txt"); File.WriteAllText(preserved, "SYNTHETIC external export");
        var logs = Path.Combine(root, "logs"); Directory.CreateDirectory(logs);
        var logSentinel = Path.Combine(logs, "application-" + DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssfffffffZ", System.Globalization.CultureInfo.InvariantCulture) +
            "-" + Guid.NewGuid().ToString("N") + ".jsonl");
        File.WriteAllText(logSentinel, "SYNTHETIC log deletion sentinel");
        var existing = Process.GetProcessesByName("AiUsage").Select(p => { using (p) return p.Id; }).ToHashSet();
        if (pending) File.WriteAllText(Path.Combine(root, "delete-local-data.v1.json"), "{\"version\":1,\"operation\":\"delete-local-data\"}");
        // A pending-intent launch can restart before any window is ready; launch it directly.
        var input = WriteFixture(fixture, "maintenance-delete-" + pending);
        try
        {
        int originalId;
        if (pending)
        {
            using var launched = LaunchAudit(input, root); originalId = launched.Id;
            RequireLaunchExited(launched);
        }
        else
        {
            using var original = new Session(input, stateDirectory: root); originalId = original.ProcessId;
            RequireFirstRunReady(original); original.Click("Settings"); original.Click("Delete stored data");
            original.Click("Cancel deleting stored data");
            Assert.Equal("SYNTHETIC opaque provider sentinel", File.ReadAllText(provider));
            Assert.True(File.Exists(logSentinel));
            Assert.False(File.Exists(Path.Combine(root, "delete-local-data.v1.json")));
            original.Click("Delete stored data"); original.Capture("REC-05-production-delete-confirmation");
            original.Click("Confirm deleting stored data"); original.AssertExited();
        }
        int restartedId = 0;
        Assert.True(Session.Wait(() =>
        {
            foreach (var candidate in Process.GetProcessesByName("AiUsage"))
            using (candidate)
            {
                if (candidate.Id == originalId || existing.Contains(candidate.Id) || !HasProcessContext(candidate, root, input)) continue;
                restartedId = candidate.Id; return true;
            }
            return false;
        }), "The actual native restart must produce an owned synthetic process");
        using var restarted = new Session(input, stateDirectory: root, attachProcessId: restartedId);
        RequireFirstRunReady(restarted);
        Assert.False(File.Exists(provider)); Assert.False(File.Exists(Path.Combine(root, "delete-local-data.v1.json")));
        Assert.False(File.Exists(logSentinel));
        Assert.False(Directory.Exists(Path.Combine(root, "Demo", "logs")));
        Assert.Equal("SYNTHETIC external export", File.ReadAllText(preserved));
        Assert.DoesNotContain("Connect:", restarted.Receipts, StringComparison.Ordinal);
        Assert.Contains("Process:" + restartedId + ":InitializeCompleted", restarted.Receipts, StringComparison.Ordinal);
        restarted.Capture("REC-05-production-delete-restarted-" + pending); restarted.Exit();
        }
        finally { StopBoundProcesses(root, input, existing); }
    }

    [Fact]
    public void RepeatedSyntheticLaunchRestoresTheExistingWindowAndInflightCtrlQDrains()
    {
        DesktopTestEnvironment.RequireUnlockedDesktop(); Assert.Equal("WDAGUtilityAccount", Environment.UserName);
        var fixture = OverviewFixture(); fixture["BlockRefresh"] = true; fixture["ManualCode"] = true;
        using var original = Session.FromFixture(fixture, "activation-and-inflight");
        var configuration = File.ReadAllBytes(Path.Combine(original.Root, "budget", "configuration.v1.json"));
        original.HideToTray();
        using (var redirected = LaunchAudit(original.Input, original.Root))
        {
            RequireLaunchExited(redirected);
        }
        Assert.True(Session.Wait(() => original.IsVisible)); original.RequireForeground();
        Assert.Equal(1, original.Receipts.Split('\n').Count(line => line.Trim() == "InitializeStarted"));
        Assert.Equal(configuration, File.ReadAllBytes(Path.Combine(original.Root, "budget", "configuration.v1.json")));
        original.Capture("LIFE-02-repeated-launch"); original.Key(VirtualKeyShort.F5);
        var ids = fixture["Accounts"]!.AsArray().Select(a => a!["AccountId"]!.GetValue<string>().Replace("-", "", StringComparison.Ordinal)).ToArray();
        Assert.True(Session.Wait(() => ids.All(id => original.Receipts.Contains("Refresh:" + id, StringComparison.Ordinal))));
        original.Click("Add account"); original.Click("Sign in to Claude");
        original.Find(e => e.Properties.Name.ValueOrDefault == "Cancel" && e.Properties.ControlType.ValueOrDefault == ControlType.Button);
        original.Capture("LIFE-03-before-inflight-CtrlQ"); original.Exit();
        foreach (var id in ids) Assert.Contains("RefreshCancelled:" + id, original.Receipts, StringComparison.Ordinal);
    }

    private static string MaintenanceRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "aiu-ui-audit-maintenance-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root); File.WriteAllText(Path.Combine(root, "synthetic-audit.marker"), "AI Usage synthetic audit v1"); return root;
    }
    private static JsonObject MaintenanceFixture()
    {
        var fixture = OverviewFixture(); fixture["Accounts"] = new JsonArray(); fixture["Observations"] = new JsonArray();
        fixture["Labels"] = new JsonObject(); fixture["ExpectedStates"] = new JsonObject();
        fixture["Configuration"]!["Caps"] = new JsonArray(); fixture["UseProductMaintenance"] = true; return fixture;
    }
    private static string WriteFixture(JsonObject fixture, string name)
    {
        var path = Path.Combine(Required("AIU_AUDIT_PAGE_DIRECTORY"), name + ".fixture"); File.WriteAllText(path, fixture.ToJsonString()); return path;
    }
    private static Process LaunchAudit(string input, string root)
    {
        Assert.Equal("WDAGUtilityAccount", Environment.UserName);
        var start = new ProcessStartInfo(Required("AIU_SMOKE_EXE")) { UseShellExecute = false };
        start.ArgumentList.Add("--demo"); start.ArgumentList.Add("--audit-input=" + input);
        start.Environment["AIU_DEVELOPMENT_STATE_DIRECTORY"] = root; return Process.Start(start)!;
    }
    private static void RequireLaunchExited(Process launched)
    {
        try { Assert.True(launched.WaitForExit(10000)); Assert.Equal(0, launched.ExitCode); }
        finally { if (!launched.HasExited) { launched.Kill(); launched.WaitForExit(5000); } }
    }
    private static bool HasProcessContext(Process process, string root, string input)
    {
        Assert.Equal("WDAGUtilityAccount", Environment.UserName);
        try
        {
            if (process.HasExited || !string.Equals(Path.GetFullPath(Required("AIU_SMOKE_EXE")), process.MainModule?.FileName, StringComparison.OrdinalIgnoreCase)) return false;
            var receipt = JsonNode.Parse(File.ReadAllText(Path.Combine(root, "process-" + process.Id + ".json")))!;
            var fixture = JsonNode.Parse(File.ReadAllText(input))!;
            return receipt["SyntheticMarker"]!.GetValue<string>() == "AI Usage synthetic audit v1" &&
                receipt["ProcessId"]!.GetValue<int>() == process.Id &&
                DateTimeOffset.Parse(receipt["ProcessStartedAt"]!.GetValue<string>(), System.Globalization.CultureInfo.InvariantCulture).UtcTicks == process.StartTime.ToUniversalTime().Ticks &&
                receipt["FixtureSha256"]!.GetValue<string>() == Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(input))) &&
                DateTimeOffset.Parse(receipt["ControlledNow"]!.GetValue<string>(), System.Globalization.CultureInfo.InvariantCulture) ==
                    DateTimeOffset.Parse(fixture["Now"]!.GetValue<string>(), System.Globalization.CultureInfo.InvariantCulture) &&
                receipt["ZoneId"]!.GetValue<string>() == fixture["ZoneId"]!.GetValue<string>();
        }
        catch (Exception error) when (error is IOException or System.Text.Json.JsonException or InvalidOperationException or System.ComponentModel.Win32Exception) { return false; }
    }
    private static void StopBoundProcesses(string root, string input, IReadOnlySet<int> existing)
    {
        foreach (var process in Process.GetProcessesByName("AiUsage"))
        using (process)
            if (!existing.Contains(process.Id) && HasProcessContext(process, root, input)) { process.Kill(); process.WaitForExit(5000); }
    }
    private static void RequireRecovery(Session session, string words) => session.Find(e =>
        (e.Properties.Name.ValueOrDefault ?? "").Contains(words, StringComparison.Ordinal));
    private static void AssertSignInUnavailable(Session session) => Assert.DoesNotContain(session.Window.FindAllDescendants(), e =>
        e.Properties.ControlType.ValueOrDefault == ControlType.Button && (e.Properties.Name.ValueOrDefault ?? "").StartsWith("Sign in to ", StringComparison.Ordinal) &&
        !e.Properties.IsOffscreen.ValueOrDefault && e.IsEnabled);
    private static void RequireFirstRunReady(Session session)
    {
        Assert.True(Session.Wait(() => new[] { "Claude", "Codex", "GitHub Copilot", "Antigravity" }.All(provider =>
            session.Window.FindAllDescendants().Any(e => e.Properties.Name.ValueOrDefault == "Sign in to " + provider && e.IsEnabled))));
        Assert.DoesNotContain(session.Window.FindAllDescendants(), e => (e.Properties.Name.ValueOrDefault ?? "").Contains("needs recovery", StringComparison.Ordinal));
        Assert.Contains("Process:" + session.ProcessId + ":InitializeCompleted", session.Receipts, StringComparison.Ordinal);
        Assert.Contains(session.Window.FindAllDescendants(), e => (e.Properties.Name.ValueOrDefault ?? "").Contains("Wed 7 Oct", StringComparison.Ordinal) &&
            (e.Properties.Name.ValueOrDefault ?? "").Contains("12:00", StringComparison.Ordinal));
    }
}
