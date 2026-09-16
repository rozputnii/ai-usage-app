using AiUsage.Features.Presentation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiUsage.Features.Demo;

/// <summary>
/// Demo shell panel: scenario selection, simulated Windows mode, window width, reduced motion, high contrast, content
/// scale, provider hues, clock, forced refresh outcomes, environment simulation, tray previews and demo reset.
/// Exists only in demo composition.
/// </summary>
internal sealed partial class DemoControlViewModel : ObservableObject
{
    private readonly DemoScenarioController controller;
    private readonly IDisplaySimulation display;
    private readonly IAppLifetime lifetime;
    private readonly PresentationFormatter format;
    private bool applying;

    public DemoControlViewModel(DemoScenarioController controller, IDisplaySimulation display, IAppLifetime lifetime, PresentationFormatter format)
    {
        this.controller = controller;
        this.display = display;
        this.lifetime = lifetime;
        this.format = format;
        Scenarios = DemoScenarioCatalog.Scenarios;
        WindowsModeLabels = [format.T("Demo_ModeActual"), format.T("Demo_ModeLight"), format.T("Demo_ModeDark")];
        MotionLabels = [format.T("Demo_MotionWindows"), format.T("Demo_MotionReduced"), format.T("Demo_MotionFull")];
        ScaleLabels = ["100 %", "150 %", "200 %"];
        OutcomeLabels = [format.T("Demo_OutcomeScenario"), format.T("Demo_OutcomeSuccess"), format.T("Demo_OutcomeNetwork"), format.T("Demo_OutcomeRateLimited"), format.T("Demo_OutcomeInvalidGrant")];
        controller.EnvironmentChanged += (_, _) => Sync();
        controller.State.Subscribe(_ => Sync());
        format.Clock.Changed += (_, _) => ClockText = format.FullDateTime(format.Clock.UtcNow);
        lifetime.VisibilityChanged += (_, _) => HideLabel = format.T(lifetime.IsHiddenToTray ? "Demo_RestoreWindow" : "Demo_HideToTray");
        Sync();
    }

    public IReadOnlyList<DemoScenario> Scenarios { get; }
    public IReadOnlyList<string> WindowsModeLabels { get; }
    public IReadOnlyList<string> MotionLabels { get; }
    public IReadOnlyList<string> ScaleLabels { get; }
    public IReadOnlyList<string> OutcomeLabels { get; }

    [ObservableProperty] public partial bool IsPanelOpen { get; set; }
    [ObservableProperty] public partial DemoScenario? SelectedScenario { get; set; }
    [ObservableProperty] public partial int WindowsModeIndex { get; set; }
    [ObservableProperty] public partial int MotionIndex { get; set; }
    [ObservableProperty] public partial int ScaleIndex { get; set; }
    [ObservableProperty] public partial int OutcomeIndex { get; set; }
    [ObservableProperty] public partial bool HighContrast { get; set; }
    [ObservableProperty] public partial bool ProviderHues { get; set; }
    [ObservableProperty] public partial bool NotificationsAllowed { get; set; }
    [ObservableProperty] public partial bool QuietHours { get; set; }
    [ObservableProperty] public partial bool BatterySaver { get; set; }
    [ObservableProperty] public partial bool SecondContextAvailable { get; set; }
    [ObservableProperty] public partial bool HasMultiContextAccount { get; private set; }
    [ObservableProperty] public partial bool NextCliScanEmpty { get; set; }
    [ObservableProperty] public partial string ClockText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string HideLabel { get; private set; } = string.Empty;
    [ObservableProperty] public partial string MarkerText { get; private set; } = string.Empty;

    private void Sync()
    {
        applying = true;
        try
        {
            MarkerText = format.T("Demo_Marker");
            SelectedScenario = Scenarios.FirstOrDefault(s => s.Id == controller.CurrentScenarioId);
            WindowsModeIndex = display.SimulatedSystemTheme switch { EffectiveTheme.Light => 1, EffectiveTheme.Dark => 2, _ => 0 };
            MotionIndex = display.ReducedMotionOverride switch { true => 1, false => 2, _ => 0 };
            ScaleIndex = display.ContentScale switch { >= 2 => 2, >= 1.5 => 1, _ => 0 };
            OutcomeIndex = (int)controller.ForcedRefreshOutcome;
            HighContrast = display.SimulatedHighContrast;
            ProviderHues = display.ProviderHues;
            NotificationsAllowed = controller.NotificationsAllowed;
            QuietHours = controller.QuietHours;
            BatterySaver = controller.BatterySaver;
            SecondContextAvailable = controller.SecondContextAvailable;
            HasMultiContextAccount = controller.HasMultiContextAccount;
            NextCliScanEmpty = controller.NextCliScanEmpty;
            ClockText = format.FullDateTime(format.Clock.UtcNow);
            HideLabel = format.T(lifetime.IsHiddenToTray ? "Demo_RestoreWindow" : "Demo_HideToTray");
        }
        finally { applying = false; }
    }

    partial void OnSelectedScenarioChanged(DemoScenario? value)
    {
        if (!applying && value is not null && value.Id != controller.CurrentScenarioId)
            _ = controller.LoadScenarioAsync(value.Id);
    }

    partial void OnWindowsModeIndexChanged(int value)
    {
        if (!applying)
            display.SimulatedSystemTheme = value switch { 1 => EffectiveTheme.Light, 2 => EffectiveTheme.Dark, _ => null };
    }

    partial void OnMotionIndexChanged(int value)
    {
        if (!applying)
            display.ReducedMotionOverride = value switch { 1 => true, 2 => false, _ => null };
    }

    partial void OnScaleIndexChanged(int value)
    {
        if (!applying)
            display.ContentScale = value switch { 2 => 2, 1 => 1.5, _ => 1 };
    }

    partial void OnOutcomeIndexChanged(int value)
    {
        if (!applying && value >= 0)
            controller.ForcedRefreshOutcome = (DemoRefreshOutcome)value;
    }

    partial void OnHighContrastChanged(bool value)
    {
        if (!applying)
            display.SimulatedHighContrast = value;
    }

    partial void OnProviderHuesChanged(bool value)
    {
        if (!applying)
            display.ProviderHues = value;
    }

    partial void OnNotificationsAllowedChanged(bool value)
    {
        if (!applying)
            controller.NotificationsAllowed = value;
    }

    partial void OnQuietHoursChanged(bool value)
    {
        if (!applying)
            controller.QuietHours = value;
    }

    partial void OnBatterySaverChanged(bool value)
    {
        if (!applying)
            controller.BatterySaver = value;
    }

    partial void OnSecondContextAvailableChanged(bool value)
    {
        if (!applying)
            controller.SecondContextAvailable = value;
    }

    partial void OnNextCliScanEmptyChanged(bool value)
    {
        if (!applying)
            controller.NextCliScanEmpty = value;
    }

    [RelayCommand]
    private void TogglePanel() => IsPanelOpen = !IsPanelOpen;

    [RelayCommand]
    private void AdvanceClock() => controller.AdvanceClock(TimeSpan.FromHours(1));

    [RelayCommand]
    private Task ReloadAsync() => controller.ReloadWithSkeletonAsync();

    [RelayCommand]
    private Task ResetAsync() => controller.ResetAsync();

    [RelayCommand]
    private void ShowTrayPopup() => lifetime.ShowTrayPopup();

    [RelayCommand]
    private void ToggleHideToTray()
    {
        if (lifetime.IsHiddenToTray)
            lifetime.ShowMainWindow();
        else
            lifetime.HideToTray();
    }

    [RelayCommand]
    private void ResizeWindow(string? width)
    {
        switch (width)
        {
            case "560": display.ResizeWindow(560, 720); break;
            case "960": display.ResizeWindow(960, 720); break;
            case "1440": display.ResizeWindow(1440, 900); break;
        }
    }
}
