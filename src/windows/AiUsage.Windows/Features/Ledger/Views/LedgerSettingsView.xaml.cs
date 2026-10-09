using AiUsage.Controls.Ledger;
using AiUsage.Features.Ledger.Contract;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using Windows.UI.Text;

namespace AiUsage.Features.Ledger.Views;

/// <summary>The settings sheet (S5, R-193). Arming Delete stored data moves focus to Cancel (R-12); disarming returns it to ⋯.</summary>
internal sealed partial class LedgerSettingsView : UserControl
{
    public LedgerSettingsView() => InitializeComponent();

    public LedgerSettingsViewModel ViewModel
    {
        get;
        set
        {
            if (field is not null)
                field.PropertyChanged -= OnChanged;
            field = value;
            field.PropertyChanged += OnChanged;
            Bindings.Update();
            UpdateInstallDots();
        }
    } = null!;

    private void OnChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LedgerSettingsViewModel.IsInstallingUpdate))
            UpdateInstallDots();
        if (e.PropertyName != nameof(LedgerSettingsViewModel.IsDeleteArmed))
            return;
        if (ViewModel.IsDeleteArmed)
            DispatcherQueue.TryEnqueue(() => CancelButton.Focus(FocusState.Programmatic));
        else
            DispatcherQueue.TryEnqueue(() => MoreButton.Focus(FocusState.Programmatic));
    }

    private void OnCompact(object sender, RoutedEventArgs e) => _ = ViewModel.SetDensityAsync(Density.Compact);
    private void OnComfortable(object sender, RoutedEventArgs e) => _ = ViewModel.SetDensityAsync(Density.Comfortable);
    private void OnUpdatesOff(object sender, RoutedEventArgs e) => _ = ViewModel.SetUpdateModeAsync(UpdateMode.Off);
    private void OnUpdatesOnLaunch(object sender, RoutedEventArgs e) => _ = ViewModel.SetUpdateModeAsync(UpdateMode.OnLaunch);
    private void OnUpdatesAlways(object sender, RoutedEventArgs e) => _ = ViewModel.SetUpdateModeAsync(UpdateMode.Always);

    // R-11: digits only, saved on Enter or when focus leaves the box; a value outside 1 to 60 returns the box to the saved one.
    private void OnRefreshBeforeChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs e) => e.Cancel = !ViewModel.AcceptsRefreshText(e.NewText);

    private void OnRefreshKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter)
            return;
        e.Handled = true;
        _ = ViewModel.CommitRefreshTextAsync(RefreshBox.Text);
    }

    // Focus leaving the box saves, except into the box's own context menu: a paste after clearing the box must find it empty.
    private void OnRefreshLostFocus(object sender, RoutedEventArgs e)
    {
        if (RefreshBox.ContextFlyout is not { IsOpen: true })
            _ = ViewModel.CommitRefreshTextAsync(RefreshBox.Text);
    }

    // Without animations the dots stay a static ellipsis.
    private void UpdateInstallDots()
    {
        if (ViewModel.IsInstallingUpdate && LedgerTheme.AnimationsEnabled)
            Composition.ApplicationDiagnostics.RunAnimation(InstallDotsMotion.Begin);
        else
            InstallDotsMotion.Stop();
    }

    private bool Not(bool value) => !value;
    private Visibility Show(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
    private Visibility ShowText(string value) => Show(value.Length > 0);
    public static Visibility Present(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

    private Brush Segment(bool state, bool match) => LedgerTheme.Solid(state == match ? "ControlOn" : "Transparent");
    private Brush SegmentText(bool state, bool match) => LedgerTheme.Solid(state == match ? "Ink" : "Ink3");
    private string SwitchName(string label, bool on) => label + ", " + (on ? "on" : "off");
    private Brush SwitchTrack(bool on) => LedgerTheme.Solid(on ? "OkP" : "NeutralP");
    private Brush SwitchFill(bool on) => LedgerTheme.Solid(on ? "OkP" : "Transparent");
    private Brush SwitchKnob(bool on) => LedgerTheme.Solid(on ? "Ink" : "Ink2");
    private HorizontalAlignment KnobSide(bool on) => on ? HorizontalAlignment.Right : HorizontalAlignment.Left;

    public static Brush DayBackground(bool on) => LedgerTheme.Solid(on ? "ControlOn" : "Transparent");
    public static Brush DayBorder(bool on) => LedgerTheme.Solid(on ? "ControlOn" : "LineMuted");
    public static Brush DayForeground(bool on) => LedgerTheme.Solid(on ? "Ink" : "Ink3");
    public static FontWeight DayWeight(bool on) => on ? FontWeights.SemiBold : FontWeights.Normal;
    public static FontFamily DayFont(bool on) => (FontFamily)LedgerTheme.Find(on ? "LedgerSansSemiboldFont" : "LedgerSansFont")!;
    public static Brush AmountBrush(bool applied) => LedgerTheme.Solid(applied ? "Ink" : "Ink2");
    // Edit (pencil) for a cap that still matches a limit, Remove (bin) for an unmatched one.
    public static string ActionGlyph(string action) => action == "Remove" ? "" : "";
}
