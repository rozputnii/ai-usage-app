using AiUsage.Controls.Ledger;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace AiUsage.Features.Ledger.Views;

/// <summary>One limit card (spec 6.2). The card is a keyboard stop with its full accessible name; Enter opens history,
/// F2 renames, C edits the cap, Alt+Up/Down reorders. History and Sign out show on hover or focus.</summary>
internal sealed partial class LedgerCardView : UserControl
{
    private bool pointerOver;

    public LedgerCardView() => InitializeComponent();

    public LimitCardViewModel ViewModel
    {
        get;
        set
        {
            if (ReferenceEquals(field, value))
                return;
            if (field is not null)
                field.PropertyChanged -= OnViewModelChanged;
            field = value;
            field.PropertyChanged += OnViewModelChanged;
            Bindings.Update();
        }
    } = null!;

    private void OnViewModelChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LimitCardViewModel.IsRenaming) && ViewModel.IsRenaming)
            DispatcherQueue.TryEnqueue(() =>
            {
                RenameBox.Focus(FocusState.Programmatic);
                RenameBox.SelectAll();
            });
        else if (e.PropertyName == nameof(LimitCardViewModel.IsRenaming) && !ViewModel.IsRenaming && RenameBox.FocusState != FocusState.Unfocused)
            Focus(FocusState.Programmatic);
        else if (e.PropertyName == nameof(LimitCardViewModel.CapEditor))
        {
            if (ViewModel.CapEditor is not null)
                DispatcherQueue.TryEnqueue(() => (ViewModel.Visual.NoteLines.Count > 0 ? NoteCapEditor : BarCapEditor).FocusInput());
            else
                Focus(FocusState.Programmatic);
        }
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.OriginalSource is TextBox || ViewModel is null)
            return;
        var alt = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        switch (e.Key)
        {
            case VirtualKey.Enter when ReferenceEquals(e.OriginalSource, this) && ViewModel.CanOpenHistory:
                e.Handled = true;
                _ = ViewModel.ToggleHistoryAsync();
                break;
            case VirtualKey.F2:
                e.Handled = true;
                ViewModel.BeginRename();
                break;
            case VirtualKey.C when ViewModel.CanEditCap && !alt:
                e.Handled = true;
                ViewModel.BeginCapEdit();
                break;
            case VirtualKey.Up when alt:
                e.Handled = true;
                _ = ViewModel.MoveUpAsync();
                break;
            case VirtualKey.Down when alt:
                e.Handled = true;
                _ = ViewModel.MoveDownAsync();
                break;
        }
    }

    private void OnRenameKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            e.Handled = true;
            _ = ViewModel.CommitRenameAsync();
        }
        else if (e.Key == VirtualKey.Escape)
        {
            e.Handled = true;
            ViewModel.CancelRename();
        }
    }

    private void OnFooterClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.CanEditCap)
            ViewModel.BeginCapEdit();
    }

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        pointerOver = true;
        UpdateIcons();
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        pointerOver = false;
        UpdateIcons();
    }

    private void OnGettingFocus(UIElement sender, GettingFocusEventArgs args) => DispatcherQueue.TryEnqueue(UpdateIcons);

    private void OnLosingFocus(UIElement sender, LosingFocusEventArgs args) => DispatcherQueue.TryEnqueue(UpdateIcons);

    private void UpdateIcons()
    {
        var focusedInside = XamlRoot is not null && FocusManager.GetFocusedElement(XamlRoot) is DependencyObject focused && IsInside(focused);
        Icons.Visibility = pointerOver || focusedInside || ViewModel?.IsHistoryOpen == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private bool IsInside(DependencyObject element)
    {
        for (var current = element; current is not null; current = VisualTreeHelper.GetParent(current))
            if (ReferenceEquals(current, this))
                return true;
        return false;
    }

    // ---- x:Bind helpers ----

    private Visibility Show(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
    private Visibility Hide(bool value) => value ? Visibility.Collapsed : Visibility.Visible;
    private Visibility ShowText(string? value) => Show(!string.IsNullOrEmpty(value));
    private Visibility BarsVisibility(int noteLines) => Show(noteLines == 0);
    private Visibility NoteVisibility(int noteLines) => Show(noteLines > 0);
    private Visibility TodayLabelVisibility(bool hasStrip, string? note) => Show(hasStrip || !string.IsNullOrEmpty(note));
    private Visibility FooterVisibility(CapEditorViewModel? editor) => Show(editor is null);
    private Visibility EditorVisibility(CapEditorViewModel? editor) => Show(editor is not null);
    private Visibility ShowNoteAction(string? action, CapEditorViewModel? editor) => Show(!string.IsNullOrEmpty(action) && editor is null);
    private Visibility ShowCompactOver(string? over, bool compact) => Show(compact && !string.IsNullOrEmpty(over));
    private Visibility ShowComfortableOver(string? over, bool compact) => Show(!compact && !string.IsNullOrEmpty(over));

    private Brush SurfaceFill(bool fresh) => (Brush)LedgerTheme.Find(fresh ? "LedgerCardFreshSurfaceBrush" : "LedgerCardSurfaceBrush")!;
    private Brush SurfaceStroke(bool fresh) => LedgerTheme.Solid(fresh ? "OkP" : "LineCard");
    private Brush NameBrush(bool stale) => LedgerTheme.Solid(stale ? "Ink2" : "Ink");
    private Brush HistoryBackground(bool open) => LedgerTheme.Solid(open ? "ControlOn" : "Transparent");
    private Brush PillBackground(Tone tone) => LedgerTheme.TonePill(tone);
    private Brush PillDot(Tone tone) => LedgerTheme.ToneMark(tone);
    private Brush PillText(Tone tone) => LedgerTheme.ToneText(tone);

    private Thickness CardPadding(bool compact) => compact ? new Thickness(12, 8, 12, 8) : new Thickness(15, 13, 15, 12);
    private double InnerGap(bool compact) => compact ? 6 : 10;
    private double StripGap(bool compact, bool hasStrip) => hasStrip ? (compact ? 3 : 8) : 0;
    private Thickness BarMargin(bool compact) => compact ? new Thickness(0, -2, 0, -2) : new Thickness(0);

    private string FooterName(string footer, IReadOnlyList<string> tip, bool canEditCap) =>
        LedgerViews.Spoken([footer, .. tip]) + (canEditCap ? ". Press to edit the cap" : string.Empty);
}
