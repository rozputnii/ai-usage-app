using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace AiUsage.Features.Settings.Monitoring;

/// <summary>Threshold rule row: values with inherited/override badge, Edit/Override, inline validation, Save/Cancel, Reset to inherited.</summary>
internal sealed partial class ThresholdRuleRow : UserControl
{
    private ThresholdRuleViewModel? rule;

    public ThresholdRuleRow() => InitializeComponent();

    public ThresholdRuleViewModel Rule
    {
        get => rule!;
        set
        {
            if (rule is not null)
                rule.PropertyChanged -= OnRuleChanged;
            rule = value;
            if (rule is not null)
                rule.PropertyChanged += OnRuleChanged;
            Bindings.Update();
        }
    }

    private string EditorId(string key) => "RuleEditor_" + key;
    private string SaveId(string key) => "RuleSave_" + key;
    private string EditId(string key) => "RuleEdit_" + key;
    private string ResetId(string key) => "RuleReset_" + key;

    private void OnRuleChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ThresholdRuleViewModel.IsEditing) && rule?.IsEditing == true)
            DispatcherQueue.TryEnqueue(() => Editor.Focus(FocusState.Programmatic));
    }

    private void OnEditorKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (rule is null)
            return;
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            e.Handled = true;
            _ = rule.SaveCommand.ExecuteAsync(null);
        }
        else if (e.Key == Windows.System.VirtualKey.Escape)
        {
            e.Handled = true;
            rule.CancelEditCommand.Execute(null);
        }
    }
}
