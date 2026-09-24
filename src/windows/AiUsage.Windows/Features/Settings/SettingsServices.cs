using AiUsage.Features.Presentation;

namespace AiUsage.Features.Settings;

public enum PreferenceKey { Density, AlwaysOnTop, ShowDisconnected, ShowHidden, HistoryEnabled, Retention, UsageDisplay, ReduceRefreshOnBatterySaver }

/// <summary><paramref name="Value"/> type matches the key: enum for Density/Retention/UsageDisplay, bool otherwise.</summary>
public sealed record PreferenceChange(PreferenceKey Key, object Value);

/// <summary>
/// Adapter boundary for durable preferences (memory-only in the demo). Changes are published through
/// <c>IUsageSource</c> snapshots; invalid values keep the prior state.
/// </summary>
public interface IPreferenceStore
{
    Task<UiCommandResult> SetPreferenceAsync(PreferenceChange change, CancellationToken cancellationToken);

    /// <summary>A rule with <c>Inherit = true</c> removes the override at its scope; the global rule cannot inherit.</summary>
    Task<UiCommandResult> SetNotificationRuleAsync(NotificationRule rule, CancellationToken cancellationToken);

    /// <summary>D-151: resets preferences only; accounts, credentials, labels, order and history are kept.</summary>
    Task<UiCommandResult> ResetSettingsAsync(CancellationToken cancellationToken);
}

public enum NotificationDelivery { WouldAppear, HeldByQuietHours, BlockedByWindows, Unknown }

public sealed record NotificationTarget(string AccountId, string? GroupId, string? WindowId);

public sealed record NotificationPreviewResult(NotificationDelivery Delivery, NotificationTarget? Target);

/// <summary>Adapter boundary for notification previews. The demo shows an in-window preview only; nothing is dispatched to Windows.</summary>
public interface INotificationPreview
{
    Task<NotificationPreviewResult> PreviewAsync(NotificationTarget? target, CancellationToken cancellationToken);
}

public enum DataStage { Idle, Checking, Processing, Completed, Failed, Cancelled }

/// <summary>Credentials, device identifiers and raw provider payloads are always excluded (D-149).</summary>
public sealed record ExportPreview(string FileName, int AccountCount, long HistoryRows, bool IncludesPreferences, bool CanSave);

public sealed record ReplaceCandidate(string Id, string DisplayName);

public sealed record ReplaceValidation(bool Valid, string? FailureKey, int? SchemaVersion, int? Accounts, long? HistoryRows, DateTimeOffset? ExportedAt);

/// <summary>Adapter boundary for portable data and deletion (D-149–D-153). Destructive operations require confirmation in the UI first.</summary>
public interface IDataManagementService
{
    /// <summary>Reports 0..1 progress for each preparation stage; the preview never writes a file.</summary>
    Task<ExportPreview> PreviewExportAsync(IProgress<double> progress, CancellationToken cancellationToken);

    Task<IReadOnlyList<ReplaceCandidate>> ListReplaceCandidatesAsync(CancellationToken cancellationToken);

    Task<ReplaceValidation> ValidateReplaceImportAsync(string candidateId, CancellationToken cancellationToken);

    Task<UiCommandResult> ApplyReplaceImportAsync(string candidateId, CancellationToken cancellationToken);

    Task<UiCommandResult> FactoryResetAsync(CancellationToken cancellationToken);

    Task<UiCommandResult> DeleteAccountDataAsync(string accountId, CancellationToken cancellationToken);

    Task<string> PreviewDataFolderAsync(CancellationToken cancellationToken);
}

/// <summary>Adapter boundary for updates (D-157–D-160, D-168). Never restarts automatically.</summary>
public interface IUpdateService
{
    Task<UiCommandResult> CheckAsync(CancellationToken cancellationToken);

    Task<UiCommandResult> DownloadAsync(CancellationToken cancellationToken);

    Task<UiCommandResult> SetChannelAsync(UpdateChannel channel, CancellationToken cancellationToken);

    Task<UiCommandResult> RestartAndUpdateAsync(CancellationToken cancellationToken);

    /// <summary>D-168: Preview channel only, compatibility (not security) blocks only.</summary>
    Task<UiCommandResult> OverrideCompatibilityBlockAsync(CancellationToken cancellationToken);
}
