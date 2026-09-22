using AiUsage.Platform;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.Graphics;

namespace AiUsage.Features.Tray;

/// <summary>Borderless always-on-top mini-dashboard near the notification area. Closes on Esc, deactivation or navigation.</summary>
internal sealed partial class TrayPopupWindow : Window
{
    private const int PopupWidth = 360;
    private const int PopupHeight = 440;
    private readonly ThemeService theme;
    private readonly DispatcherQueueTimer clock;
    private bool showing;
    private bool closing;

    public TrayPopupWindow(TrayViewModel viewModel, ThemeService theme, string title)
    {
        ViewModel = viewModel;
        this.theme = theme;
        InitializeComponent();
        clock = DispatcherQueue.CreateTimer();
        clock.Interval = TimeSpan.FromSeconds(30);
        clock.Tick += OnClockTick;
        Title = title;
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = true;
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.SetBorderAndTitleBar(true, false);
        }
        AppWindow.IsShownInSwitchers = false;
        theme.Attach(PopupRoot, null);
        viewModel.ClosePopupRequested += OnCloseRequested;
        Activated += (_, args) =>
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated)
                HidePopup();
        };
        AppWindow.Closing += (_, args) =>
        {
            if (closing)
                return;
            args.Cancel = true;
            HidePopup();
        };
    }

    public TrayViewModel ViewModel { get; }

    public void ShowNearTray()
    {
        if (closing)
            return;
        ViewModel.RefreshTime();
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        var scale = PopupRoot.XamlRoot?.RasterizationScale ?? 1;
        var width = (int)(PopupWidth * scale);
        var height = (int)(PopupHeight * scale);
        AppWindow.MoveAndResize(new RectInt32(area.X + area.Width - width - (int)(12 * scale), area.Y + area.Height - height - (int)(12 * scale), width, height));
        AppWindow.Show();
        showing = true;
        clock.Start();
        Activate();
    }

    public void HidePopup()
    {
        showing = false;
        clock.Stop();
        AppWindow.Hide();
    }

    private void OnClockTick(DispatcherQueueTimer sender, object args)
    {
        if (showing && !closing)
            ViewModel.RefreshTime();
    }

    public void CloseForExit()
    {
        closing = true;
        showing = false;
        clock.Stop();
        clock.Tick -= OnClockTick;
        ViewModel.ClosePopupRequested -= OnCloseRequested;
        theme.Detach(PopupRoot);
        Close();
    }

    private void OnCloseRequested(object? sender, EventArgs e) => HidePopup();

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            e.Handled = true;
            HidePopup();
        }
    }
}
