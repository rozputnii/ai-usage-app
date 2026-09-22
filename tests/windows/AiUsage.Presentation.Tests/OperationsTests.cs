using AiUsage.Features.CliImport;
using AiUsage.Features.Presentation;
using AiUsage.Features.Settings.DataPrivacy;
using AiUsage.Features.SystemStatusPage;
using Xunit;

namespace AiUsage.Presentation.Tests;

/// <summary>F11 CLI import, F12 diagnostics and data, F13 recovery and F14 updates.</summary>
public sealed class OperationsTests
{
    [Fact]
    public async Task F11DiscoveryIsExplicitAndProgressive()
    {
        using var host = new TestHost(autoDelays: false);
        var cli = host.CliImport();
        cli.Open();
        Assert.True(cli.IsIdle);
        Assert.Empty(cli.Candidates);
        var scan = cli.ScanCommand.ExecuteAsync(null);
        Assert.True(cli.IsScanning);
        Assert.Equal("Scanning… 0 found", cli.ScanningText);
        await host.Delays.Advance();
        Assert.Single(cli.Candidates);
        Assert.Equal("Scanning… 1 found", cli.ScanningText);
        await host.Delays.Drain();
        await scan;
        Assert.Equal(["demo-new", "demo-duplicate", "demo-unsupported", "demo-failure"], cli.Candidates.Select(c => c.Id));
        Assert.False(cli.Candidates.Single(c => c.Id == "demo-unsupported").IsSelected);
        Assert.Equal("Import 3 selected", cli.ImportLabel);
    }

    [Fact]
    public async Task F11PerItemOutcomesAndReimportMapsToTheSameIdentity()
    {
        using var host = new TestHost();
        var cli = host.CliImport();
        await cli.ScanCommand.ExecuteAsync(null);
        cli.Candidates.Single(c => c.Id == "demo-unsupported").IsSelected = true;
        await cli.ImportSelectedCommand.ExecuteAsync(null);
        Assert.True(cli.IsDone);
        string Outcome(string id) => cli.Candidates.Single(c => c.Id == id).OutcomeText;
        Assert.Equal("Imported", Outcome("demo-new"));
        Assert.Equal("Duplicate of “Research” · skipped", Outcome("demo-duplicate"));
        Assert.Equal("Unsupported credential format", Outcome("demo-unsupported"));
        Assert.Equal(OutcomeTone.Critical, cli.Candidates.Single(c => c.Id == "demo-failure").Tone);
        Assert.Equal(6, host.Usage.Current.Accounts.Count);

        await cli.ReimportSelectedCommand.ExecuteAsync(null);
        Assert.Equal("Re-imported · same account identity", Outcome("demo-new"));
        Assert.Equal(6, host.Usage.Current.Accounts.Count);
        Assert.Single(host.Usage.Current.Accounts, a => a.Id == DemoIds.CliAccount);
    }

    [Fact]
    public async Task F11CancelKeepsCompletedItemsAndMarksTheRestCancelled()
    {
        using var host = new TestHost(autoDelays: false);
        var cli = host.CliImport();
        var scan = cli.ScanCommand.ExecuteAsync(null);
        await host.Delays.Drain();
        await scan;
        var import = cli.ImportSelectedCommand.ExecuteAsync(null);
        Assert.True(cli.IsImporting);
        await host.Delays.Advance();
        Assert.Equal("Imported", cli.Candidates[0].OutcomeText);
        cli.CancelImportCommand.Execute(null);
        await host.Delays.Drain();
        await import;
        Assert.Equal("Imported", cli.Candidates[0].OutcomeText);
        Assert.Equal("Cancelled · not imported", cli.Candidates[1].OutcomeText);
        Assert.Equal("Cancelled · not imported", cli.Candidates[3].OutcomeText);
        Assert.Contains(host.Usage.Current.Accounts, a => a.Id == DemoIds.CliAccount);
    }

    [Fact]
    public async Task F11NoCandidatesShowsEmptyOutcome()
    {
        using var host = new TestHost();
        host.Controller.NextCliScanEmpty = true;
        var cli = host.CliImport();
        await cli.ScanCommand.ExecuteAsync(null);
        Assert.True(cli.NoCandidates);
        Assert.False(cli.ImportSelectedCommand.CanExecute(null));
    }

    [Fact]
    public async Task F12HealthCheckReportsProgressiveWarningsWithoutRepairAndCancelKeepsPartialResults()
    {
        using var host = new TestHost(autoDelays: false);
        var status = host.SystemStatus();
        var before = System.Text.Json.JsonSerializer.Serialize(host.Usage.Current);
        var run = status.RunHealthCheckCommand.ExecuteAsync(null);
        Assert.True(status.IsHealthChecking);
        await host.Delays.Advance(3);
        Assert.Equal(3, status.HealthSteps.Count);
        Assert.Equal("Checking 3 of 5", status.HealthCheckingText);
        await host.Delays.Drain();
        await run;
        Assert.Equal("Warning · 2 findings. Nothing was repaired; review the items above.", status.HealthResult);
        Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(host.Usage.Current));

        var again = status.RunHealthCheckCommand.ExecuteAsync(null);
        await host.Delays.Advance(2);
        status.CancelHealthCheckCommand.Execute(null);
        await host.Delays.Drain();
        await again;
        Assert.Equal(2, status.HealthSteps.Count);
        Assert.Equal("Cancelled · partial results kept", status.HealthResult);
    }

    [Fact]
    public async Task F12DiagnosticsAndLogPreviewsAreSanitizedToggles()
    {
        using var host = new TestHost();
        var status = host.SystemStatus();
        await status.ToggleDiagnosticsCommand.ExecuteAsync(null);
        Assert.Contains("excluded: credentials, tokens, paths, provider payloads", status.DiagnosticsText);
        await status.ToggleLogCommand.ExecuteAsync(null);
        Assert.True(status.HasLog);
        await status.ToggleDiagnosticsCommand.ExecuteAsync(null);
        Assert.False(status.HasDiagnostics);
        Assert.Equal("Sign-in required · Stale", new TestHost("F06").SystemStatus().ProviderStatuses[0].StatusText);
    }

    [Fact]
    public async Task F12ExportPreviewStagesAndCancel()
    {
        using var host = new TestHost(autoDelays: false);
        var data = host.DataPrivacy();
        var export = data.PrepareExportCommand.ExecuteAsync(null);
        Assert.True(data.IsExportPreparing);
        await host.Delays.Advance();
        Assert.Equal(1 / 3.0, data.ExportProgress, 3);
        data.CancelExportCommand.Execute(null);
        await host.Delays.Drain();
        await export;
        Assert.True(data.IsExportIdle);

        host.Delays.Auto = true;
        await data.PrepareExportCommand.ExecuteAsync(null);
        Assert.True(data.IsExportReady);
        Assert.Contains(data.ExportItems, i => i.StartsWith("Excluded: credentials", StringComparison.Ordinal));
        Assert.StartsWith("Live only", data.SaveDisabledReason);
    }

    [Fact]
    public async Task F12InvalidReplaceLeavesStateUnchangedValidRequiresConfirmation()
    {
        using var host = new TestHost();
        var data = host.DataPrivacy();
        data.SelectedCandidate = data.ReplaceCandidates.Single(c => c.Id == "invalid");
        await data.ValidateReplaceCommand.ExecuteAsync(null);
        Assert.True(data.IsReplaceInvalid);
        Assert.Equal("Validation failed: the bundle checksum does not match. Nothing was changed.", data.ReplaceMessage);

        data.SelectedCandidate = data.ReplaceCandidates.Single(c => c.Id == "valid");
        Assert.True(data.IsReplaceIdle);
        await data.ValidateReplaceCommand.ExecuteAsync(null);
        Assert.True(data.IsReplaceValid);
        Assert.StartsWith("Valid bundle · Schema 4 · 3 accounts · 9,120 history rows", data.ReplaceMessage);
        host.Dialogs.Next(ConfirmOutcome.Cancelled);
        await data.ConfirmReplaceCommand.ExecuteAsync(null);
        Assert.True(data.IsReplaceValid);
        host.Dialogs.Next(ConfirmOutcome.Confirmed);
        await data.ConfirmReplaceCommand.ExecuteAsync(null);
        Assert.True(data.IsReplaceDone);
        Assert.True(host.Dialogs.Requests.Last().Destructive);
    }

    [Fact]
    public async Task F12SettingsResetKeepsAccountsLabelsOrderAndHistoryFactoryResetNeedsTypedConfirmation()
    {
        using var host = new TestHost();
        await host.Usage.ExecuteAsync(host.Context.Command(UiCommandKind.Rename, "demo-codex-1", new RenamePayload("Home")), CancellationToken.None);
        await host.Usage.ExecuteAsync(host.Context.Command(UiCommandKind.Reorder, null, new ReorderPayload(["demo-codex-2", "demo-codex-1", "demo-claude-1", "demo-copilot-1", "demo-antigravity-1"])), CancellationToken.None);
        await host.Preferences.SetPreferenceAsync(new(AiUsage.Features.Settings.PreferenceKey.Theme, ThemePreference.Dark), CancellationToken.None);
        await host.Preferences.SetNotificationRuleAsync(new(RuleScope.Provider, "codex", false, [50], true), CancellationToken.None);
        var data = host.DataPrivacy();
        host.Dialogs.Next(ConfirmOutcome.Confirmed);
        await data.ResetSettingsCommand.ExecuteAsync(null);
        var prefs = host.Usage.Current.Preferences;
        Assert.Equal(ThemePreference.System, prefs.Theme);
        Assert.Single(prefs.NotificationRules);
        Assert.Equal("Home", host.Account("demo-codex-1").Label);
        Assert.Equal("demo-codex-2", prefs.AccountOrder[0]);
        Assert.Equal(5, host.Usage.Current.Accounts.Count);

        host.Dialogs.Next(ConfirmOutcome.Confirmed, typed: "reset");
        await data.FactoryResetCommand.ExecuteAsync(null);
        Assert.Equal(5, host.Usage.Current.Accounts.Count);
        Assert.Equal("RESET", host.Dialogs.Requests.Last().TypedConfirmation);

        host.Dialogs.Next(ConfirmOutcome.Confirmed, typed: "RESET");
        await data.FactoryResetCommand.ExecuteAsync(null);
        Assert.Empty(host.Usage.Current.Accounts);
        Assert.True(host.Overview().IsFirstRun);
        Assert.Equal(PageKey.Overview, host.Navigation.Current);
    }

    [Theory]
    [InlineData("F13a", "A data migration did not finish", true)]
    [InlineData("F13b", "This data was written by a newer version", false)]
    [InlineData("F13c", "The last restore did not complete", true)]
    public void F13RecoveryStatesBlockTheShell(string scenario, string title, bool retryAvailable)
    {
        using var host = new TestHost(scenario);
        var shell = host.Shell();
        var recovery = host.RecoveryPage();
        Assert.True(shell.IsRecovery);
        Assert.True(recovery.IsActive);
        Assert.Equal(title, recovery.Title);
        Assert.Equal(retryAvailable, recovery.RetryCommand.CanExecute(null));
        Assert.Equal(!retryAvailable, recovery.HasDisabledNote);
    }

    [Fact]
    public async Task F13RetryFailureStaysInRecoveryAndValidCheckpointRestoresTheDashboard()
    {
        using var host = new TestHost("F13a");
        var shell = host.Shell();
        var recovery = host.RecoveryPage();
        await recovery.RetryCommand.ExecuteAsync(null);
        Assert.True(shell.IsRecovery);
        Assert.True(recovery.MessageIsCritical);
        Assert.StartsWith("Recovery could not complete.", recovery.Message);

        await recovery.ToggleCheckpointsCommand.ExecuteAsync(null);
        Assert.Equal(2, recovery.Checkpoints.Count);
        Assert.Equal("Before migration · Sep 15, 11:30 AM", recovery.Checkpoints[0].Label);
        host.Dialogs.Next(ConfirmOutcome.Cancelled);
        await recovery.RestoreCheckpointCommand.ExecuteAsync(recovery.Checkpoints[0]);
        Assert.True(shell.IsRecovery);
        host.Dialogs.Next(ConfirmOutcome.Confirmed);
        await recovery.RestoreCheckpointCommand.ExecuteAsync(recovery.Checkpoints[0]);
        Assert.False(shell.IsRecovery);
        Assert.Equal("Checkpoint restored. Dashboard available.", host.Announcer.Last);
    }

    [Fact]
    public async Task F13cRestoreFailureKeepsRecoveryAndNewerSchemaKeepsDiagnosticsAvailable()
    {
        using var host = new TestHost("F13c");
        var recovery = host.RecoveryPage();
        host.Dialogs.Next(ConfirmOutcome.Confirmed);
        await recovery.RetryCommand.ExecuteAsync(null);
        Assert.True(recovery.IsActive);
        Assert.Equal("Restore could not complete. Normal operation remains paused; inspect diagnostics or retry.", recovery.Message);

        using var newer = new TestHost("F13b");
        var blocked = newer.RecoveryPage();
        Assert.False(blocked.RetryCommand.CanExecute(null));
        await blocked.ToggleDiagnosticsCommand.ExecuteAsync(null);
        Assert.Contains("recovery NewerSchema", blocked.DiagnosticsText);
        blocked.PreviewDataFolderCommand.Execute(null);
        Assert.Contains("No action in demo", blocked.Message);
    }

    [Fact]
    public async Task F14CheckAvailableDownloadReadyAndSimulatedRestart()
    {
        using var host = new TestHost(autoDelays: false);
        var updates = host.UpdatesPage();
        Assert.True(updates.ShowCheck);
        var check = updates.CheckCommand.ExecuteAsync(null);
        Assert.True(updates.IsChecking);
        await host.Delays.Drain();
        await check;
        Assert.Equal("Version 1.1.0 (synthetic) is available.", updates.StatusText);
        var download = updates.DownloadCommand.ExecuteAsync(null);
        await host.Delays.Advance(2);
        Assert.True(updates.IsDownloading);
        Assert.Equal(0.45, updates.DownloadProgress, 3);
        await host.Delays.Drain();
        await download;
        Assert.True(updates.ShowRestart);
        host.Dialogs.Next(ConfirmOutcome.Confirmed);
        var restart = updates.RestartAndUpdateCommand.ExecuteAsync(null);
        await host.Delays.Drain();
        await restart;
        Assert.Equal("You’re up to date · 1.1.0 (synthetic) installed.", updates.StatusText);
        Assert.Equal(0, host.Lifetime.Exits);
    }

    [Fact]
    public async Task F14CheckFailureAndCancelKeepCurrentBuildAndPreviewToStableWaits()
    {
        using var host = new TestHost();
        var updates = host.UpdatesPage();
        var overview = host.Overview();
        await updates.SimulateCheckFailureCommand.ExecuteAsync(null);
        Assert.True(updates.StatusIsFailure);
        Assert.Equal("Could not reach the update feed. Your current version keeps working.", updates.StatusText);
        Assert.True(overview.HasRows);

        using var manual = new TestHost(autoDelays: false);
        var cancelling = manual.UpdatesPage();
        var check = cancelling.CheckCommand.ExecuteAsync(null);
        cancelling.CancelCheckCommand.Execute(null);
        await manual.Delays.Drain();
        await check;
        Assert.Equal("You’re up to date.", cancelling.StatusText);

        updates.ChannelIndex = (int)UpdateChannel.Preview;
        updates.ChannelIndex = (int)UpdateChannel.Stable;
        Assert.Equal(UpdateState.WaitingForStable, host.Usage.Current.System.Update);
        Assert.Equal("Waiting for the next Stable release. Your Preview build keeps working until then.", updates.StatusText);
    }

    [Fact]
    public async Task F14BlocksKeepCacheReadableSecurityCannotBeOverriddenCompatibilityCanOnPreview()
    {
        using var host = new TestHost("F14b");
        var shell = host.Shell();
        var updates = host.UpdatesPage();
        var overview = host.Overview();
        Assert.True(shell.ShowCompatibilityBanner);
        Assert.True(shell.CompatibilityIsSecurity);
        Assert.False(shell.RefreshAllCommand.CanExecute(null));
        Assert.True(overview.HasRows);
        Assert.Equal("72 %", overview.FindRow("demo-codex-1")!.Windows[0].ValueText);
        updates.ChannelIndex = (int)UpdateChannel.Preview;
        Assert.False(updates.CanOverrideCompatibility);

        using var compat = new TestHost("F14a");
        var compatShell = compat.Shell();
        var compatUpdates = compat.UpdatesPage();
        Assert.False(compatUpdates.CanOverrideCompatibility);
        compatUpdates.ChannelIndex = (int)UpdateChannel.Preview;
        Assert.True(compatUpdates.CanOverrideCompatibility);
        compat.Dialogs.Next(ConfirmOutcome.Confirmed);
        await compatUpdates.OverrideCompatibilityCommand.ExecuteAsync(null);
        Assert.True(compatShell.RefreshAllCommand.CanExecute(null));
        Assert.Contains("Overridden on this device", compatShell.CompatibilityBody);
    }
}

internal static class DemoIds
{
    public const string CliAccount = AiUsage.Features.Demo.DemoCliImportService.ImportedAccountId;
}
