using AiUsage.Features.Presentation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AiUsage.Features.Settings;

internal sealed partial class SettingsPage : Page
{
    private SettingsTab? pendingSection;

    public SettingsPage()
    {
        InitializeComponent();
        Loaded += (_, _) => ScrollPending();
    }

    public SettingsViewModel ViewModel { get; private set; } = null!;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        var viewModel = (SettingsViewModel)e.Parameter;
        if (!ReferenceEquals(ViewModel, viewModel))
        {
            if (ViewModel is not null)
                ViewModel.SectionRequested -= OnSectionRequested;
            ViewModel = viewModel;
            ViewModel.SectionRequested += OnSectionRequested;
            Bindings.Update();
        }
        base.OnNavigatedTo(e);
        pendingSection = ViewModel.RequestedSection;
        if (IsLoaded)
            ScrollPending();
    }

    private Thickness Gutter(bool compact) => compact ? new Thickness(18, 0, 18, 18) : new Thickness(40, 0, 40, 24);

    /// <summary>A request while the page is off screen waits for it to load; the frame shows it right after.</summary>
    private void OnSectionRequested(object? sender, SettingsTab section)
    {
        pendingSection = section;
        if (IsLoaded)
            ScrollPending();
    }

    private void ScrollPending()
    {
        // Low priority runs after layout, so sections have their final offsets.
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            if (pendingSection is not { } section || !IsLoaded)
                return;
            pendingSection = null;
            FrameworkElement target = section switch
            {
                SettingsTab.Monitoring => MonitoringSection,
                SettingsTab.DataPrivacy => DataPrivacySection,
                SettingsTab.Updates => UpdatesSection,
                SettingsTab.SystemStatus => SystemStatusSection,
                _ => AppearanceSection,
            };
            var offset = section == SettingsTab.Appearance ? 0 : target.TransformToVisual((UIElement)Scroller.Content).TransformPoint(default).Y;
            Scroller.ChangeView(null, offset, null, disableAnimation: true);
        });
    }
}
