using System.ComponentModel;
using AiUsage.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace AiUsage.Features.Accounts;

/// <summary>
/// Quota window line in account detail: label, meter with the native amount under it, value and reset. Below 720 px it
/// stacks: label + value, then full-width meter, then reset.
/// Exposes one UI Automation text element whose name carries value, state word and reset.
/// </summary>
internal sealed partial class WindowLineView : UserControl
{
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
        AbsoluteUnderMeter.Visibility = window.HasAbsolute ? Visibility.Visible : Visibility.Collapsed;

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
            LabelColumn.Width = new GridLength(150);
            LabelColumn.MinWidth = 120;
            LabelColumn.MaxWidth = 170;
            MeterColumn.Width = new GridLength(1, GridUnitType.Star);
            MeterColumn.MinWidth = 100;
            ValueColumn.Width = new GridLength(90);
            // Star-sized so a long reset line can never push the row past the page gutter; the text trims instead.
            ResetColumn.Width = new GridLength(1.4, GridUnitType.Star);
            ResetColumn.MinWidth = 150;
            ResetColumn.MaxWidth = 320;
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
