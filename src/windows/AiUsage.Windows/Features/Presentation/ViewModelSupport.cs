using System.Collections.ObjectModel;
using AiUsage.Features.Accounts;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AiUsage.Features.Presentation;

/// <summary>Explicit bundle of the host services most view models need. Not a service locator: members are fixed.</summary>
internal sealed class PresentationContext(
    IUsageSource usage,
    IUiDispatcher dispatcher,
    IClock clock,
    ITextResources text,
    IAnnouncer announcer,
    INavigationService navigation,
    IDialogService dialogs,
    IMotionSettings motion,
    ProviderCatalog? providers = null)
{
    public IUsageSource Usage { get; } = usage;
    public IUiDispatcher Dispatcher { get; } = dispatcher;
    public IClock Clock { get; } = clock;
    public IAnnouncer Announcer { get; } = announcer;
    public INavigationService Navigation { get; } = navigation;
    public IDialogService Dialogs { get; } = dialogs;
    public IMotionSettings Motion { get; } = motion;
    public ProviderCatalog Providers { get; } = providers ?? ProviderCatalog.Default;
    public PresentationFormatter Format { get; } = new(text, clock);

    public UiCommand Command(UiCommandKind kind, string? targetId = null, object? payload = null) =>
        new(kind, targetId, payload, Usage.Current.Revision);
}

/// <summary>Base for view models that render the shared snapshot. Updates are marshalled to the UI dispatcher.</summary>
internal abstract class SnapshotViewModel : ObservableObject, IDisposable
{
    private readonly IDisposable subscription;
    private bool disposed;

    protected SnapshotViewModel(PresentationContext context)
    {
        Context = context;
        subscription = context.Usage.Subscribe(snapshot => Dispatch(() => Apply(snapshot)));
        context.Clock.Changed += OnClockChanged;
    }

    protected PresentationContext Context { get; }
    protected PresentationFormatter Format => Context.Format;
    protected UiSnapshot Snapshot { get; private set; } = null!;

    /// <summary>Call at the end of the derived constructor.</summary>
    protected void Initialize() => Apply(Context.Usage.Current);

    private void Apply(UiSnapshot snapshot)
    {
        if (disposed)
            return;
        Snapshot = snapshot;
        OnSnapshot(snapshot);
    }

    protected abstract void OnSnapshot(UiSnapshot snapshot);

    private void OnClockChanged(object? sender, EventArgs e) => Dispatch(() => Apply(Context.Usage.Current));

    protected void Dispatch(Action action)
    {
        if (Context.Dispatcher.HasThreadAccess)
            action();
        else
            Context.Dispatcher.Post(action);
    }

    public void Dispose()
    {
        disposed = true;
        subscription.Dispose();
        Context.Clock.Changed -= OnClockChanged;
    }
}

internal static class CollectionSync
{
    /// <summary>Reconciles <paramref name="target"/> to <paramref name="source"/> by stable key, reusing view models so focus and animation state survive updates.</summary>
    public static void Sync<TSource, TViewModel>(
        ObservableCollection<TViewModel> target,
        IReadOnlyList<TSource> source,
        Func<TSource, string> sourceKey,
        Func<TViewModel, string> viewModelKey,
        Func<TSource, TViewModel> create,
        Action<TViewModel, TSource> update)
    {
        var keys = new HashSet<string>(source.Select(sourceKey), StringComparer.Ordinal);
        for (var i = target.Count - 1; i >= 0; i--)
            if (!keys.Contains(viewModelKey(target[i])))
                target.RemoveAt(i);
        for (var i = 0; i < source.Count; i++)
        {
            var key = sourceKey(source[i]);
            var existing = -1;
            for (var j = i; j < target.Count; j++)
                if (viewModelKey(target[j]) == key)
                {
                    existing = j;
                    break;
                }
            TViewModel item;
            if (existing < 0)
            {
                item = create(source[i]);
                target.Insert(i, item);
            }
            else
            {
                item = target[existing];
                if (existing != i)
                    target.Move(existing, i);
            }
            update(item, source[i]);
        }
    }
}

public enum PillTone { Neutral, Muted, Warning, Critical }

/// <summary>Connection wins over freshness; "quota unavailable" only when every window is Unavailable.</summary>
internal sealed partial class StatusPillViewModel : ObservableObject
{
    [ObservableProperty] public partial string Text { get; private set; } = string.Empty;
    [ObservableProperty] public partial PillTone Tone { get; private set; }
    /// <summary>A healthy fresh pill that Overview reveals only on hover or focus.</summary>
    [ObservableProperty] public partial bool IsQuietFresh { get; private set; }

    public void Update(AccountItem account, PresentationFormatter format)
    {
        IsQuietFresh = false;
        var windows = account.Contexts.SelectMany(c => c.Groups).SelectMany(g => g.Windows).ToArray();
        (Text, Tone) = account.Connection switch
        {
            ConnectionState.NotConnected => (format.T("Pill_Disconnected"), PillTone.Muted),
            ConnectionState.Connecting => (format.T("Pill_Connecting"), PillTone.Neutral),
            ConnectionState.ReauthRequired => (format.T("Pill_SignInRequired"), PillTone.Critical),
            ConnectionState.RecoveryRequired => (format.T("Pill_RecoveryRequired"), PillTone.Warning),
            _ when windows.Length > 0 && windows.All(w => w.ValueState == ValueState.Unavailable) => (format.T("Pill_QuotaUnavailable"), PillTone.Neutral),
            _ => account.Freshness switch
            {
                Freshness.Stale => (format.F("Pill_Stale", account.FetchedAt is { } s ? format.DateTime(s) : format.T("Value_Unknown")), PillTone.Warning),
                Freshness.Cached => (format.F("Pill_Cached", account.FetchedAt is { } c ? format.DateTime(c) : format.T("Value_Unknown")), PillTone.Neutral),
                Freshness.Unknown => (format.T("Pill_FreshnessUnknown"), PillTone.Neutral),
                _ => (format.F("Pill_Fresh", account.FetchedAt is { } f ? format.Time(f) : format.T("Value_Unknown")), PillTone.Neutral),
            },
        };
        IsQuietFresh = account.Connection == ConnectionState.Connected && account.Freshness == Freshness.Fresh && Tone == PillTone.Neutral
            && !(windows.Length > 0 && windows.All(w => w.ValueState == ValueState.Unavailable));
    }
}

public enum FailureAction { None, Retry, Reconnect }

/// <summary>Scoped failure line. Retry stays available unless the provider's retry time forbids it (D5 resolution).</summary>
internal sealed partial class FailureViewModel : ObservableObject
{
    [ObservableProperty] public partial bool IsVisible { get; private set; }
    [ObservableProperty] public partial string Message { get; private set; } = string.Empty;
    [ObservableProperty] public partial FailureAction Action { get; private set; }
    [ObservableProperty] public partial string ActionLabel { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool ActionEnabled { get; private set; }
    [ObservableProperty] public partial string WaitText { get; private set; } = string.Empty;

    public bool HasAction => Action != FailureAction.None;
    public bool HasWait => WaitText.Length > 0;

    partial void OnActionChanged(FailureAction value) => OnPropertyChanged(nameof(HasAction));
    partial void OnWaitTextChanged(string value) => OnPropertyChanged(nameof(HasWait));

    public void Update(AccountItem account, PresentationFormatter format)
    {
        var failure = account.Failure;
        ActionEnabled = false;
        IsVisible = failure is not null;
        if (failure is null)
        {
            Message = string.Empty;
            Action = FailureAction.None;
            ActionLabel = string.Empty;
            WaitText = string.Empty;
            return;
        }
        Message = format.T(failure.MessageKey);
        var now = format.Clock.UtcNow;
        var retryPending = failure.RetryAt is { } at && at > now;
        if (failure.Kind == FailureKinds.InternalError)
        {
            Action = FailureAction.None;
            ActionLabel = string.Empty;
            WaitText = string.Empty;
        }
        else if (account.Connection == ConnectionState.ReauthRequired)
        {
            Action = FailureAction.Reconnect;
            ActionLabel = format.T("Action_Reconnect");
            ActionEnabled = account.Operation == AccountOperation.Idle;
            WaitText = string.Empty;
        }
        else if (failure.Kind == FailureKinds.RateLimited && retryPending)
        {
            Action = FailureAction.Retry;
            ActionLabel = format.T("Action_Retry");
            ActionEnabled = false;
            WaitText = format.F("Failure_RetryAvailableAt", format.Time(failure.RetryAt!.Value));
        }
        else if (failure.Recoverable)
        {
            Action = FailureAction.Retry;
            ActionLabel = format.T("Action_Retry");
            ActionEnabled = account.Operation == AccountOperation.Idle;
            WaitText = retryPending ? format.F("Failure_AutoRetryAt", format.Time(failure.RetryAt!.Value)) : string.Empty;
        }
        else
        {
            Action = FailureAction.None;
            ActionLabel = string.Empty;
            WaitText = string.Empty;
        }
    }
}
