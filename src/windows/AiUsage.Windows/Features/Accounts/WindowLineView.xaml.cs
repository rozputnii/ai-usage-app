using System.ComponentModel;
using AiUsage.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace AiUsage.Features.Accounts;

public enum WindowLineVariant { Overview, OverviewExpanded, Detail }

/// <summary>
/// Quota window line used by Overview rows (three columns, 22 px light numerals) and account detail rows (15 px values,
/// native amount under the meter). Below 720 px it stacks: label + value, then full-width meter, then reset.
/// Exposes one UI Automation text element whose name carries value, state word and reset.
/// </summary>
internal sealed partial class WindowLineView : UserControl
{
    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(nameof(Variant), typeof(WindowLineVariant), typeof(WindowLineView), new PropertyMetadata(WindowLineVariant.Overview, (d, _) => ((WindowLineView)d).Arrange()));

    private QuotaWindowViewModel? window;

    public WindowLineView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            AppLayout.Current.PropertyChanged += OnLayoutChanged;
            Arrange();
        };
        Unloaded += (_, _) => AppLayout.Current.PropertyChanged -= OnLayoutChanged;
    }

    public QuotaWindowViewModel Window
    {
        get => window!;
        set
        {
            if (window is not null)
                window.PropertyChanged -= OnWindowChanged;
            window = value;
            if (window is not null)
                window.PropertyChanged += OnWindowChanged;
            Bindings.Update();
            UpdateName();
            Arrange();
        }
    }

    public WindowLineVariant Variant { get => (WindowLineVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    private string MutedText(bool muted) => muted ? Controls.Res.T("Window_MutedTag") : string.Empty;
    private string ResetKey(bool critical) => critical ? "CritBrush" : "Text2Brush";

    protected override AutomationPeer OnCreateAutomationPeer() => new LinePeer(this);

    private void OnWindowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(QuotaWindowViewModel.AccessibleName))
            UpdateName();
        if (e.PropertyName is nameof(QuotaWindowViewModel.AbsoluteText) or nameof(QuotaWindowViewModel.Kind))
            Arrange();
    }

    private void UpdateName() => AutomationProperties.SetName(this, window?.AccessibleName ?? string.Empty);

    private void OnLayoutChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppLayout.IsCompact))
            Arrange();
    }

    private void Arrange()
    {
        if (!IsLoaded || window is null)
            return;
        var compact = AppLayout.Current.IsCompact;
        var detail = Variant == WindowLineVariant.Detail;
        var hasAbsolute = window.HasAbsolute;

        LabelText.FontSize = detail ? 13 : 12;
        LabelText.Foreground = Controls.Bind.Token(this, detail ? "TextBrush" : "Text2Brush");
        ValueText.FontSize = detail ? 15 : window.Kind == MeterKind.Bar ? 22 : 15;
        ValueText.FontWeight = detail ? Microsoft.UI.Text.FontWeights.Normal : Microsoft.UI.Text.FontWeights.Light;
        ResetRelativeText.FontSize = detail ? 12 : 11;
        ResetExactText.FontSize = detail ? 12 : 11;
        AbsoluteUnderMeter.Visibility = detail && hasAbsolute ? Visibility.Visible : Visibility.Collapsed;
        AbsoluteUnderReset.Visibility = Variant == WindowLineVariant.OverviewExpanded && hasAbsolute ? Visibility.Visible : Visibility.Collapsed;

        if (compact)
        {
            LabelColumn.Width = new GridLength(1, GridUnitType.Star);
            LabelColumn.MaxWidth = double.PositiveInfinity;
            MeterColumn.Width = new GridLength(0);
            MeterColumn.MinWidth = 0;
            ValueColumn.Width = GridLength.Auto;
            ResetColumn.Width = new GridLength(0);
            ResetColumn.MinWidth = 0;
            Place(LabelText, 0, 0, 1);
            Place(ValueHost, 0, 2, 1);
            Place(MeterHost, 1, 0, 4);
            Place(ResetHost, 2, 0, 4);
        }
        else
        {
            LabelColumn.Width = detail ? new GridLength(150) : GridLength.Auto;
            LabelColumn.MinWidth = detail ? 120 : 90;
            LabelColumn.MaxWidth = detail ? 170 : 130;
            MeterColumn.Width = new GridLength(1, GridUnitType.Star);
            MeterColumn.MinWidth = detail ? 100 : 80;
            ValueColumn.Width = detail ? new GridLength(90) : GridLength.Auto;
            // Star-sized so a long reset line can never push the row past the page gutter; the text trims instead.
            ResetColumn.Width = new GridLength(detail ? 1.4 : 1.6, GridUnitType.Star);
            ResetColumn.MinWidth = detail ? 150 : 140;
            ResetColumn.MaxWidth = detail ? 320 : 260;
            Place(LabelText, 0, 0, 1);
            Place(MeterHost, 0, 1, 1);
            Place(ValueHost, 0, 2, 1);
            Place(ResetHost, 0, 3, 1);
        }
    }

    private static void Place(FrameworkElement element, int row, int column, int span)
    {
        Grid.SetRow(element, row);
        Grid.SetColumn(element, column);
        Grid.SetColumnSpan(element, span);
    }

    private sealed partial class LinePeer(WindowLineView owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;
        protected override string GetClassNameCore() => nameof(WindowLineView);
        protected override bool IsControlElementCore() => true;
        protected override IList<AutomationPeer> GetChildrenCore() => [];
    }
}
