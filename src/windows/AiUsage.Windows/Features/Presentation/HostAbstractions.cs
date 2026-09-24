using System.ComponentModel;

namespace AiUsage.Features.Presentation;

public interface IProductLifecycle
{
    Task InitializeAsync();
    Task StopAsync();
}

// Host abstractions implemented by Platform/ (WinUI) and faked by tests. View models depend only on these.

public interface IUiDispatcher
{
    bool HasThreadAccess { get; }
    void Post(Action action);
}

public interface IClock
{
    DateTimeOffset UtcNow { get; }
    TimeZoneInfo TimeZone { get; }
    /// <summary>Raised when the clock moves (demo advance or reset); time-relative text must be recomputed.</summary>
    event EventHandler? Changed;
    /// <summary>Operation latency. Tests complete it deterministically; nothing waits on real wall time in tests.</summary>
    Task Delay(TimeSpan duration, CancellationToken cancellationToken);
}

/// <summary>Overview is the single main view; the others open in its place from the header or an account row, without tabs.</summary>
public enum PageKey { Overview, Accounts, History, Settings }

/// <summary>Sections of the single settings view, in display order.</summary>
public enum SettingsTab { Appearance, Monitoring, DataPrivacy, Updates, SystemStatus }

public sealed record NavigationRequest(PageKey Page, string? AccountId = null, SettingsTab? Tab = null, string? WindowId = null);

public interface INavigationService
{
    PageKey Current { get; }
    bool CanGoBack { get; }
    event EventHandler<NavigationRequest>? Navigated;
    void Navigate(NavigationRequest request);
    void GoBack();
}

public interface IDialogService
{
    /// <summary>Shows a confirmation; the returned task completes when the dialog closes.</summary>
    Task<ConfirmOutcome> ConfirmAsync(ConfirmRequest request);
    bool IsDialogOpen { get; }
}

public enum ConfirmOutcome { Cancelled, Confirmed, Alternate }

/// <summary>
/// <paramref name="ConfirmAction"/> runs while the dialog stays open with a busy label; returning a message keeps the
/// dialog open with that error text, returning null closes it.
/// </summary>
public sealed record ConfirmRequest(
    string Title,
    string Body,
    string ConfirmLabel,
    bool Destructive = false,
    string? AlternateLabel = null,
    string? TypedConfirmation = null,
    string? BusyLabel = null,
    Func<CancellationToken, Task<string?>>? ConfirmAction = null,
    Func<CancellationToken, Task<string?>>? AlternateAction = null);

public enum EffectiveTheme { Light, Dark }

public interface IThemeService : INotifyPropertyChanged
{
    ThemePreference Preference { get; }
    EffectiveTheme SystemTheme { get; }
    EffectiveTheme Effective { get; }
    bool HighContrast { get; }
    void Apply(ThemePreference preference);
}

public interface IMotionSettings : INotifyPropertyChanged
{
    bool ReducedMotion { get; }
    /// <summary>False while the main window is hidden to the tray; decorative animation and timers stop.</summary>
    bool WindowVisible { get; }
    bool AnimationsAllowed { get; }
}

public interface IAnnouncer
{
    /// <summary>Announces a completed operation state politely; never animation frames.</summary>
    void Announce(string message);
}

public interface ITextResources
{
    string Get(string key);
}

public interface IAppLifetime
{
    void ShowMainWindow();
    void HideToTray();
    bool IsHiddenToTray { get; }
    event EventHandler? VisibilityChanged;
    Task ExitAsync();
    void SetAlwaysOnTop(bool value);
    void ShowTrayPopup();
}
