using AiUsage.Controls.Ledger;
using AiUsage.Features.Ledger.Contract;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;

namespace AiUsage.Features.Ledger.Views;

/// <summary>The inline settings panel (S5). Arming Delete stored data moves focus to Cancel (R-12).</summary>
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
        }
    } = null!;

    private void OnChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(LedgerSettingsViewModel.IsDeleteArmed))
            return;
        if (ViewModel.IsDeleteArmed)
            DispatcherQueue.TryEnqueue(() => CancelButton.Focus(FocusState.Keyboard));
        else
            DispatcherQueue.TryEnqueue(() => DeleteButton.Focus(FocusState.Programmatic));
    }

    private void OnUsed(object sender, RoutedEventArgs e) => _ = ViewModel.SetValueModeAsync(ValueMode.Used);
    private void OnLeft(object sender, RoutedEventArgs e) => _ = ViewModel.SetValueModeAsync(ValueMode.Left);
    private void OnCompact(object sender, RoutedEventArgs e) => _ = ViewModel.SetDensityAsync(Density.Compact);
    private void OnComfortable(object sender, RoutedEventArgs e) => _ = ViewModel.SetDensityAsync(Density.Comfortable);
    private void OnUpdatesOff(object sender, RoutedEventArgs e) => _ = ViewModel.SetUpdateModeAsync(UpdateMode.Off);
    private void OnUpdatesOnLaunch(object sender, RoutedEventArgs e) => _ = ViewModel.SetUpdateModeAsync(UpdateMode.OnLaunch);
    private void OnUpdatesAlways(object sender, RoutedEventArgs e) => _ = ViewModel.SetUpdateModeAsync(UpdateMode.Always);

    private Visibility Show(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
    private Visibility Hide(bool value) => value ? Visibility.Collapsed : Visibility.Visible;
    public static Visibility Present(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

    private Brush Segment(bool state, bool match) => LedgerTheme.Solid(state == match ? "ControlOn" : "Transparent");
    private Brush SegmentText(bool state, bool match) => LedgerTheme.Solid(state == match ? "Ink" : "Ink3");
    private string OnOff(bool on) => on ? "On" : "Off";
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
    public static Brush NoteBrush(bool warning) => LedgerTheme.Solid(warning ? "AttText" : "Ink3");
}
