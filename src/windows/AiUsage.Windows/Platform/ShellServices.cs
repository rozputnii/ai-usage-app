using AiUsage.Features.Demo;
using AiUsage.Features.Presentation;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace AiUsage.Platform;

/// <summary>Polite UI Automation notifications for completed operations, never animation frames.</summary>
internal sealed class Announcer : IAnnouncer
{
    private TextBlock? region;

    public string Last { get; private set; } = string.Empty;

    public void Attach(TextBlock liveRegion) => region = liveRegion;

    public void Announce(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;
        Last = message;
        if (region is null)
            return;
        region.Text = message;
        var peer = FrameworkElementAutomationPeer.FromElement(region) ?? FrameworkElementAutomationPeer.CreatePeerForElement(region);
        peer?.RaiseNotificationEvent(AutomationNotificationKind.ActionCompleted, AutomationNotificationProcessing.MostRecent, message, "AiUsageStatus");
    }
}

/// <summary>Window lifetime: close hides to the tray, the tray restores, Exit is explicit and confirmed by the shell.</summary>
internal sealed class AppLifetime(MotionSettings motion) : IAppLifetime
{
    private MainWindow? window;
    private Func<Task>? shutdown;
    private Action? showPopup;

    public bool IsHiddenToTray { get; private set; }
    public event EventHandler? VisibilityChanged;

    public void Attach(MainWindow mainWindow, Func<Task> exit, Action popup)
    {
        window = mainWindow;
        shutdown = exit;
        showPopup = popup;
    }

    public void ShowMainWindow()
    {
        if (window is null)
            return;
        window.AppWindow.Show();
        if (window.AppWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Minimized } presenter)
            presenter.Restore();
        window.Activate();
        SetHidden(false);
    }

    public void HideToTray()
    {
        if (window is null)
            return;
        window.AppWindow.Hide();
        SetHidden(true);
    }

    private void SetHidden(bool hidden)
    {
        motion.WindowVisible = !hidden;
        if (IsHiddenToTray == hidden)
            return;
        IsHiddenToTray = hidden;
        VisibilityChanged?.Invoke(this, EventArgs.Empty);
    }

    public Task ExitAsync() => shutdown?.Invoke() ?? Task.CompletedTask;

    public void SetAlwaysOnTop(bool value)
    {
        if (window?.AppWindow.Presenter is OverlappedPresenter presenter)
            presenter.IsAlwaysOnTop = value;
    }

    public void ShowTrayPopup() => showPopup?.Invoke();
}

/// <summary>Demo-only display simulation: Windows mode, contrast, motion, content scale, provider hues and window size.</summary>
internal sealed class DisplaySimulation(ThemeService theme, MotionSettings motion) : IDisplaySimulation
{
    private MainWindow? window;

    public event EventHandler? ContentScaleChanged;

    public void Attach(MainWindow mainWindow) => window = mainWindow;

    public EffectiveTheme? SimulatedSystemTheme
    {
        get => theme.SimulatedSystemTheme;
        set => theme.SimulatedSystemTheme = value;
    }

    public bool SimulatedHighContrast
    {
        get => theme.SimulatedHighContrast;
        set => theme.SimulatedHighContrast = value;
    }

    public bool? ReducedMotionOverride
    {
        get => motion.Override;
        set => motion.Override = value;
    }

    public double ContentScale
    {
        get;
        set
        {
            field = value is 1.5 or 2 ? value : 1;
            ContentScaleChanged?.Invoke(this, EventArgs.Empty);
        }
    } = 1;

    public bool ProviderHues
    {
        get => AppLayout.Current.ProviderHues;
        set => AppLayout.Current.ProviderHues = value;
    }

    public void ResizeWindow(int effectiveWidth, int effectiveHeight)
    {
        if (window is null)
            return;
        var scale = window.Content?.XamlRoot?.RasterizationScale ?? 1;
        if (window.AppWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Maximized } presenter)
            presenter.Restore();
        window.AppWindow.Resize(new((int)(effectiveWidth * scale), (int)(effectiveHeight * scale)));
    }
}
