using AiUsage.Features.Ledger;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace AiUsage.Controls.Ledger;

/// <summary>Static helpers for x:Bind in Ledger views.</summary>
internal static class LedgerViews
{
    public static ToolTip? Tip(IReadOnlyList<string>? lines) => lines is null ? null : LedgerTheme.Tip(lines);

    public static ToolTip? TextTip(string text) => LedgerTheme.Tip([text]);

    public static string Spoken(IReadOnlyList<string>? lines) => lines is null ? string.Empty : LedgerFormat.Spoken(string.Join(". ", lines));

    public static Brush MarkBrush(bool neutral) => LedgerTheme.Solid(neutral ? "Ink2" : "AttText");
}

/// <summary>The inline cap editor row (S3): "cap [amount] unit · Save · Remove", its hint and any error.</summary>
internal sealed partial class CapEditorView : StackPanel
{
    private readonly TextBox input;
    private readonly TextBlock unit;
    private readonly TextBlock hint;
    private readonly TextBlock error;
    private readonly LedgerButton remove;

    public static readonly DependencyProperty EditorProperty = DependencyProperty.Register(nameof(Editor), typeof(CapEditorViewModel), typeof(CapEditorView), new PropertyMetadata(null, (d, _) => ((CapEditorView)d).Attach()));

    public CapEditorView()
    {
        Spacing = 5;
        var row = new Grid { ColumnSpacing = 8 };
        foreach (var width in new[] { GridLength.Auto, new GridLength(1000, GridUnitType.Star), GridLength.Auto, new GridLength(1, GridUnitType.Star), GridLength.Auto, GridLength.Auto })
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = width });
        // The amount box takes the free width up to 140 px, in a card and in the narrower settings sheet,
        // before the gap that right-aligns Save and Remove.
        row.ColumnDefinitions[1].MinWidth = 64;
        row.ColumnDefinitions[1].MaxWidth = 140;
        var label = Text("cap", "Ink3");
        label.VerticalAlignment = VerticalAlignment.Center;
        row.Children.Add(label);
        input = new TextBox { Style = (Style)LedgerTheme.Find("LedgerAmountBox")! };
        // The template's border keeps a 32 px theme minimum that the 24 px box would clip at the bottom.
        input.Resources["TextControlThemeMinHeight"] = 24d;
        input.BeforeTextChanging += (_, e) => e.Cancel = Editor is { } editor && !editor.Accepts(e.NewText);
        input.TextChanged += (_, _) =>
        {
            if (Editor is { } editor && editor.Text != input.Text)
                editor.Text = input.Text;
        };
        input.KeyDown += OnInputKeyDown;
        Grid.SetColumn(input, 1);
        row.Children.Add(input);
        unit = Text(string.Empty, "Ink3");
        unit.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(unit, 2);
        row.Children.Add(unit);
        var save = new LedgerButton { Content = "Save", Style = (Style)LedgerTheme.Find("LedgerPrimaryButton")! };
        save.Click += (_, _) => _ = Editor?.SaveAsync();
        Grid.SetColumn(save, 4);
        row.Children.Add(save);
        remove = new LedgerButton { Content = "Remove", Style = (Style)LedgerTheme.Find("LedgerLinkButton")! };
        remove.Click += (_, _) => _ = Editor?.RemoveAsync();
        Grid.SetColumn(remove, 5);
        row.Children.Add(remove);
        Children.Add(row);
        error = Text(string.Empty, "CritText");
        error.TextWrapping = TextWrapping.Wrap;
        error.Visibility = Visibility.Collapsed;
        Children.Add(error);
        hint = Text(string.Empty, "Ink3");
        hint.TextWrapping = TextWrapping.Wrap;
        Children.Add(hint);
    }

    public CapEditorViewModel? Editor { get => (CapEditorViewModel?)GetValue(EditorProperty); set => SetValue(EditorProperty, value); }

    public void FocusInput()
    {
        input.Focus(FocusState.Programmatic);
        input.SelectAll();
    }

    private CapEditorViewModel? attached;

    private void Attach()
    {
        if (attached is not null)
            attached.PropertyChanged -= OnEditorChanged;
        attached = Editor;
        if (attached is null)
            return;
        attached.PropertyChanged += OnEditorChanged;
        input.Text = attached.Text;
        unit.Text = attached.UnitText;
        hint.Text = attached.Hint;
        remove.Visibility = attached.HasCap ? Visibility.Visible : Visibility.Collapsed;
        AutomationProperties.SetName(input, "Cap amount in " + attached.UnitText);
        AutomationProperties.SetHelpText(input, attached.Hint);
        ShowError();
        var target = attached;
        DispatcherQueue.TryEnqueue(() =>
        {
            if (ReferenceEquals(Editor, target) && Visibility == Visibility.Visible)
                FocusInput();
        });
    }

    private void OnEditorChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (attached is null)
            return;
        if (e.PropertyName == nameof(CapEditorViewModel.Text) && input.Text != attached.Text)
            input.Text = attached.Text;
        ShowError();
    }

    private void ShowError()
    {
        error.Text = attached?.Error ?? string.Empty;
        error.Visibility = string.IsNullOrEmpty(error.Text) ? Visibility.Collapsed : Visibility.Visible;
        AutomationProperties.SetName(error, error.Text);
    }

    private void OnInputKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            e.Handled = true;
            _ = Editor?.SaveAsync();
        }
        else if (e.Key == VirtualKey.Escape)
        {
            e.Handled = true;
            Editor?.Cancel();
        }
    }

    private static TextBlock Text(string text, string key) => new()
    {
        Text = text,
        FontSize = 12,
        LineHeight = 16,
        Foreground = LedgerTheme.Solid(key),
        FontFamily = (FontFamily)LedgerTheme.Find("LedgerSansFont")!,
    };
}
