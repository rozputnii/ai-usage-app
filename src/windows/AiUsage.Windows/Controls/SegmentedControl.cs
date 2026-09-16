using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace AiUsage.Controls;

/// <summary>Segmented radio group bound to a list of labels and a selected index (density, usage display, channel).</summary>
internal sealed partial class SegmentedControl : UserControl
{
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(nameof(Items), typeof(object), typeof(SegmentedControl), new PropertyMetadata(null, (d, _) => ((SegmentedControl)d).Build()));
    public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(SegmentedControl), new PropertyMetadata(-1, (d, _) => ((SegmentedControl)d).Sync()));

    private readonly StackPanel panel = new() { Orientation = Orientation.Horizontal, XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled };
    private readonly string group = "Segment" + Guid.NewGuid().ToString("N");

    public SegmentedControl()
    {
        IsTabStop = false;
        Content = new Border { Style = (Style)Application.Current.Resources["SegmentHost"], Child = panel };
        HorizontalAlignment = HorizontalAlignment.Left;
    }

    public object? Items { get => GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
    public int SelectedIndex { get => (int)GetValue(SelectedIndexProperty); set => SetValue(SelectedIndexProperty, value); }

    private void Build()
    {
        panel.Children.Clear();
        if (Items is not IEnumerable<string> labels)
            return;
        var index = 0;
        foreach (var label in labels)
        {
            var position = index++;
            var radio = new RadioButton
            {
                Content = label,
                GroupName = group,
                Style = (Style)Application.Current.Resources["SegmentRadio"],
                IsChecked = position == SelectedIndex,
            };
            AutomationProperties.SetName(radio, label);
            radio.Checked += (_, _) =>
            {
                if (SelectedIndex != position)
                    SelectedIndex = position;
            };
            panel.Children.Add(radio);
        }
        var name = AutomationProperties.GetName(this);
        if (!string.IsNullOrEmpty(name))
            AutomationProperties.SetName(panel, name);
    }

    private void Sync()
    {
        for (var i = 0; i < panel.Children.Count; i++)
            if (panel.Children[i] is RadioButton radio)
                radio.IsChecked = i == SelectedIndex;
    }
}
