using System.Text.Json.Nodes;
using FlaUI.Core.Definitions;
using Xunit;

namespace AiUsage.Windows.Tests;

public sealed partial class AuditWindows
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HeldStartupDisablesAccountActionsAndCanReleaseOrExit(bool release)
    {
        DesktopTestEnvironment.RequireUnlockedDesktop();
        var fixture = OverviewFixture(); fixture["BlockInitialization"] = true;
        using var session = Session.FromFixture(fixture, "startup-" + release);
        Assert.NotNull(session.Find(e => e.Properties.Name.ValueOrDefault == "Opening local data…"));
        Assert.False(session.Find(e => e.Properties.Name.ValueOrDefault == "Add account").IsEnabled);
        foreach (var name in new[] { "Sign in to Claude", "Sign in to Codex", "Sign in to GitHub Copilot", "Sign in to Antigravity" })
            Assert.DoesNotContain(session.Window.FindAllDescendants(), e => e.Properties.Name.ValueOrDefault == name && e.Properties.IsEnabled.ValueOrDefault);
        session.Click("Settings");
        Assert.NotNull(session.Find(e => e.Properties.Name.ValueOrDefault == "Opening local data…"));
        session.Capture("startup-held-" + release);
        Assert.DoesNotContain("Connect:", session.Receipts, StringComparison.Ordinal);
        if (release)
        {
            File.WriteAllText(Path.Combine(session.Root, "initialize.release"), "synthetic release");
            Assert.True(Session.Wait(() => session.Receipts.Contains("InitializeCompleted", StringComparison.Ordinal)));
            Assert.True(Session.Wait(() => session.Find(e => e.Properties.Name.ValueOrDefault == "Add account").IsEnabled));
            Assert.True(Session.Wait(() => session.Window.FindAllDescendants().Any(e => e.Properties.Name.ValueOrDefault == "Claude · SYNTHETIC")));
            session.Capture("startup-released");
        }
        session.Exit();
        if (!release) Assert.Contains("InitializeCancelled", session.Receipts, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void MockRecoveryActionsExposeRestrictionsFailureAndSuccess(bool retry, bool restore)
    {
        DesktopTestEnvironment.RequireUnlockedDesktop();
        var fixture = OverviewFixture();
        fixture["Recovery"] = new JsonObject { ["Message"] = "SYNTHETIC local data needs recovery", ["CanRetry"] = retry, ["CanRestorePreferences"] = restore };
        fixture["RecoveryFailures"] = 1;
        using var session = Session.FromFixture(fixture, "recovery-" + retry + "-" + restore);
        Assert.NotNull(session.Find(e => e.Properties.Name.ValueOrDefault == "SYNTHETIC local data needs recovery"));
        Assert.False(session.Find(e => e.Properties.Name.ValueOrDefault == "Add account").IsEnabled);
        session.Click("Recovery and diagnostics");
        var retryButton = session.Find(e => e.Properties.Name.ValueOrDefault == "Retry recovery" && e.Properties.ControlType.ValueOrDefault == ControlType.Button);
        var restoreButton = session.Find(e => e.Properties.Name.ValueOrDefault == "Restore legacy preferences" && e.Properties.ControlType.ValueOrDefault == ControlType.Button);
        Assert.Equal(retry, retryButton.IsEnabled); Assert.Equal(restore, restoreButton.IsEnabled);
        session.Click("Preview diagnostics");
        Assert.Contains("PreviewDiagnostics", session.Receipts, StringComparison.Ordinal);
        Assert.NotNull(session.Find(e => (e.Properties.Name.ValueOrDefault ?? "").Contains("provider transport disabled", StringComparison.Ordinal)));
        session.Click("Export recovery summary");
        Assert.True(Session.Wait(() => File.Exists(Path.Combine(session.Root, "recovery-diagnostics.txt"))));
        Assert.Contains("SYNTHETIC AUDIT", File.ReadAllText(Path.Combine(session.Root, "recovery-diagnostics.txt")), StringComparison.Ordinal);
        session.Capture("recovery-restrictions-" + retry + "-" + restore);
        if (retry)
        {
            session.Click(retryButton);
            Assert.True(Session.Wait(() => session.Receipts.Contains("RecoveryFailed:synthetic", StringComparison.Ordinal)));
            Assert.NotNull(session.Find(e => e.Properties.Name.ValueOrDefault == "Action unavailable; local data is preserved"));
            Assert.False(session.Find(e => e.Properties.Name.ValueOrDefault == "Add account").IsEnabled);
            session.Capture("recovery-failed-" + restore);
            if (restore) session.Click(restoreButton);
            else session.Click(retryButton);
            Assert.True(Session.Wait(() => session.Find(e => e.Properties.Name.ValueOrDefault == "Add account").IsEnabled));
            session.Capture("recovery-completed-" + restore);
        }
        Assert.DoesNotContain("Connect:", session.Receipts, StringComparison.Ordinal);
        session.Exit();
    }
}
