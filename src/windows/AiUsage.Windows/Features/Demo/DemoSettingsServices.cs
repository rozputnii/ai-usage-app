using AiUsage.Features.Presentation;
using AiUsage.Features.Settings;

namespace AiUsage.Features.Demo;

/// <summary>Memory-only preferences. Nothing is written to disk or production storage.</summary>
internal sealed class DemoPreferenceStore(DemoState state) : IPreferenceStore
{
    public Task<UiCommandResult> SetPreferenceAsync(PreferenceChange change, CancellationToken cancellationToken)
    {
        var world = state.World;
        switch (change.Key, change.Value)
        {
            case (PreferenceKey.Density, Density density): world.Density = density; break;
            case (PreferenceKey.AlwaysOnTop, bool onTop): world.AlwaysOnTop = onTop; break;
            case (PreferenceKey.ShowDisconnected, bool show): world.ShowDisconnected = show; break;
            case (PreferenceKey.ShowHidden, bool showHidden): world.ShowHidden = showHidden; break;
            case (PreferenceKey.HistoryEnabled, bool enabled): world.HistoryEnabled = enabled; break;
            case (PreferenceKey.Retention, HistoryRetention retention): world.Retention = retention; break;
            case (PreferenceKey.UsageDisplay, UsageDisplay display): world.UsageDisplay = display; break;
            case (PreferenceKey.ReduceRefreshOnBatterySaver, bool reduce): world.ReduceRefreshOnBatterySaver = reduce; break;
            default: return Task.FromResult(UiCommandResult.Failed());
        }
        state.Publish();
        return Task.FromResult(UiCommandResult.Succeeded);
    }

    public Task<UiCommandResult> SetNotificationRuleAsync(NotificationRule rule, CancellationToken cancellationToken)
    {
        var world = state.World;
        var valid = rule.RemainingThresholds.All(t => t is >= 0 and <= 100) && rule.RemainingThresholds.Distinct().Count() == rule.RemainingThresholds.Count
            && (rule.Scope == RuleScope.Global) == (rule.TargetId is null) && !(rule.Scope == RuleScope.Global && rule.Inherit)
            && (rule.Inherit || rule.RemainingThresholds.Count > 0);
        if (!valid)
            return Task.FromResult(UiCommandResult.Failed());
        world.Rules.RemoveAll(existing => existing.Scope == rule.Scope && existing.TargetId == rule.TargetId);
        if (!rule.Inherit)
            world.Rules.Add(rule with { RemainingThresholds = rule.RemainingThresholds.OrderByDescending(t => t).ToArray() });
        state.Publish();
        return Task.FromResult(UiCommandResult.Succeeded);
    }

    public Task<UiCommandResult> ResetSettingsAsync(CancellationToken cancellationToken)
    {
        state.World.ResetSettings();
        state.Publish();
        return Task.FromResult(UiCommandResult.Succeeded);
    }
}

internal sealed class DemoNotificationPreview(DemoState state) : INotificationPreview
{
    public async Task<NotificationPreviewResult> PreviewAsync(NotificationTarget? target, CancellationToken cancellationToken)
    {
        await state.DelayAsync(DemoLatency.Preview, cancellationToken);
        var world = state.World;
        var delivery = !world.NotificationsAllowed ? NotificationDelivery.BlockedByWindows
            : world.QuietHours ? NotificationDelivery.HeldByQuietHours
            : NotificationDelivery.WouldAppear;
        return new(delivery, target);
    }
}

internal sealed class DemoDataManagementService(DemoState state) : IDataManagementService
{
    public async Task<ExportPreview> PreviewExportAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        for (var stage = 1; stage <= 3; stage++)
        {
            await state.DelayAsync(DemoLatency.ExportStage, cancellationToken);
            progress.Report(stage / 3.0);
        }
        var world = state.World;
        return new ExportPreview("aiusage-export-2026-09-15.json", world.Accounts.Count, world.HistoryRows, IncludesPreferences: true, CanSave: false);
    }

    public Task<IReadOnlyList<ReplaceCandidate>> ListReplaceCandidatesAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ReplaceCandidate>>(
        [
            new("valid", "sample-bundle-valid.json"),
            new("invalid", "sample-bundle-corrupt.json"),
        ]);

    public async Task<ReplaceValidation> ValidateReplaceImportAsync(string candidateId, CancellationToken cancellationToken)
    {
        await state.DelayAsync(DemoLatency.ReplaceValidate, cancellationToken);
        return candidateId == "valid"
            ? new(true, null, 4, 3, 9120, new DateTimeOffset(2026, 9, 12, 20, 0, 0, TimeSpan.Zero))
            : new(false, "Replace_InvalidChecksum", null, null, null, null);
    }

    public async Task<UiCommandResult> ApplyReplaceImportAsync(string candidateId, CancellationToken cancellationToken)
    {
        if (candidateId != "valid")
            return UiCommandResult.Failed();
        await state.DelayAsync(DemoLatency.ReplaceApply, cancellationToken);
        return UiCommandResult.Succeeded;
    }

    public async Task<UiCommandResult> FactoryResetAsync(CancellationToken cancellationToken)
    {
        await state.DelayAsync(DemoLatency.FactoryReset, cancellationToken);
        state.LoadScenario("F01", keepAppearance: false, skeleton: false);
        return UiCommandResult.Succeeded;
    }

    public async Task<UiCommandResult> DeleteAccountDataAsync(string accountId, CancellationToken cancellationToken)
    {
        if (state.Account(accountId) is not { } account)
            return UiCommandResult.Conflict;
        await state.DelayAsync(DemoLatency.DeleteData, cancellationToken);
        account.HistoryDeleted = true;
        foreach (var window in account.AllWindows.Where(w => w.State is ValueState.Known or ValueState.Exhausted))
        {
            window.ProviderRemaining = window.State == ValueState.Exhausted ? 0 : window.Remaining;
            window.ProviderState = window.State;
            window.Remaining = null;
            window.State = ValueState.Unknown;
        }
        account.FetchedAt = null;
        account.Freshness = Freshness.Unknown;
        state.Publish();
        return UiCommandResult.Succeeded;
    }

    public Task<string> PreviewDataFolderAsync(CancellationToken cancellationToken) =>
        Task.FromResult(@"%LOCALAPPDATA%\AI Usage\data");
}

internal sealed class DemoUpdateService(DemoState state) : IUpdateService
{
    /// <summary>Demo-only: the next check fails.</summary>
    public bool FailNextCheck { get; set; }

    public async Task<UiCommandResult> CheckAsync(CancellationToken cancellationToken)
    {
        var world = state.World;
        var previous = world.Update;
        world.Update = UpdateState.Checking;
        world.UpdateFailureKey = null;
        state.Publish();
        try
        {
            await state.DelayAsync(DemoLatency.UpdateCheck, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (state.World == world)
            {
                world.Update = previous is UpdateState.Failed ? UpdateState.Current : previous;
                state.Publish();
            }
            return UiCommandResult.Cancelled;
        }
        world.LastUpdateCheck = state.Clock.UtcNow;
        if (FailNextCheck)
        {
            FailNextCheck = false;
            world.Update = UpdateState.Failed;
            world.UpdateFailureKey = "Update_Failed";
            state.Publish();
            return UiCommandResult.Failed(new("UpdateFeedUnreachable", "Update_Failed", null, true));
        }
        world.Update = UpdateState.Available;
        world.UpdateVersion = "1.1.0 (synthetic)";
        state.Publish();
        return UiCommandResult.Succeeded;
    }

    public async Task<UiCommandResult> DownloadAsync(CancellationToken cancellationToken)
    {
        var world = state.World;
        if (world.Update != UpdateState.Available)
            return UiCommandResult.Conflict;
        world.Update = UpdateState.Downloading;
        world.UpdateProgress = 0;
        state.Publish();
        foreach (var progress in new[] { 20, 45, 70, 90, 100 })
        {
            try
            {
                await state.DelayAsync(DemoLatency.DownloadStep, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (state.World == world)
                {
                    world.Update = UpdateState.Available;
                    world.UpdateProgress = null;
                    state.Publish();
                }
                return UiCommandResult.Cancelled;
            }
            world.UpdateProgress = progress;
            state.Publish();
        }
        world.Update = UpdateState.Ready;
        world.UpdateProgress = null;
        state.Publish();
        return UiCommandResult.Succeeded;
    }

    public Task<UiCommandResult> SetChannelAsync(UpdateChannel channel, CancellationToken cancellationToken)
    {
        var world = state.World;
        if (channel == world.Channel)
            return Task.FromResult(UiCommandResult.Succeeded);
        if (channel == UpdateChannel.Stable && world.Channel == UpdateChannel.Preview)
            world.Update = UpdateState.WaitingForStable;
        else if (world.Update == UpdateState.WaitingForStable)
            world.Update = UpdateState.Current;
        world.Channel = channel;
        if (channel == UpdateChannel.Stable)
            world.CompatibilityOverridden = false;
        state.Publish();
        return Task.FromResult(UiCommandResult.Succeeded);
    }

    public async Task<UiCommandResult> RestartAndUpdateAsync(CancellationToken cancellationToken)
    {
        var world = state.World;
        if (world.Update != UpdateState.Ready)
            return UiCommandResult.Conflict;
        await state.DelayAsync(DemoLatency.Restart, cancellationToken);
        world.Update = UpdateState.Current;
        world.InstalledUpdateVersion = world.UpdateVersion;
        world.UpdateVersion = null;
        state.Publish();
        return UiCommandResult.Succeeded;
    }

    public Task<UiCommandResult> OverrideCompatibilityBlockAsync(CancellationToken cancellationToken)
    {
        var world = state.World;
        if (world.Compatibility != CompatibilityState.CompatibilityBlocked || world.Channel != UpdateChannel.Preview)
            return Task.FromResult(UiCommandResult.Unsupported);
        world.CompatibilityOverridden = true;
        state.Publish();
        return Task.FromResult(UiCommandResult.Succeeded);
    }
}
