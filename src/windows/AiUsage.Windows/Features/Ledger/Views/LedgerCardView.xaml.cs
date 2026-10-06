using AiUsage.Controls.Ledger;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace AiUsage.Features.Ledger.Views;

/// <summary>One limit card (spec 6.2). The card is a keyboard stop with its full accessible name; Enter opens history,
/// F2 renames, C edits the cap, Alt+Up/Down reorders. History and Sign out stay visible but quiet until pointed at.</summary>
internal sealed partial class LedgerCardView : UserControl
{
    public LedgerCardView() => InitializeComponent();

    public LedgerCardView? MonetarySection
    {
        set
        {
            MonetaryHost.Content = value;
            MonetaryDivider.Visibility = value is null ? Visibility.Collapsed : Visibility.Visible;
        }
    }

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
        else if (e.PropertyName == nameof(LimitCardViewModel.IsHistoryOpen) && !HistoryButton.IsPointerOver)
            QuietIcon(HistoryButton);
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
            case VirtualKey.Left or VirtualKey.Right when !alt:
                var stops = DetailStops(Bars).ToList();
                var index = stops.FindIndex(stop => ReferenceEquals(stop, FocusManager.GetFocusedElement(XamlRoot)));
                var next = index < 0 ? (e.Key == VirtualKey.Right ? 0 : stops.Count - 1)
                    : Math.Clamp(index + (e.Key == VirtualKey.Right ? 1 : -1), 0, stops.Count - 1);
                if (stops.Count > 0)
                {
                    e.Handled = true;
                    stops[next].Focus(FocusState.Keyboard);
                }
                break;
        }
    }

    private static IEnumerable<Control> DetailStops(DependencyObject root)
    {
        if (root is UIElement { Visibility: Visibility.Collapsed }) yield break;
        if (root is Control { IsTabStop: true, IsEnabled: true } control && ToolTipService.GetToolTip(control) is not null)
            yield return control;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (var child in DetailStops(VisualTreeHelper.GetChild(root, i)))
                yield return child;
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

    private void OnIconPointerEntered(object sender, PointerRoutedEventArgs e) => ((UIElement)sender).Opacity = 1;

    private void OnIconPointerExited(object sender, PointerRoutedEventArgs e) => QuietIcon((UIElement)sender);

    /// <summary>Returns an icon to the style's quiet opacity; the History icon stays opaque while its history is open.</summary>
    private void QuietIcon(UIElement icon)
    {
        if (ReferenceEquals(icon, HistoryButton) && ViewModel?.IsHistoryOpen == true)
            icon.Opacity = 1;
        else
            icon.ClearValue(OpacityProperty);
    }

    // ---- x:Bind helpers ----

    private Visibility Show(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
    private Visibility Hide(bool value) => value ? Visibility.Collapsed : Visibility.Visible;
    private Visibility ShowText(string? value) => Show(!string.IsNullOrEmpty(value));
    private Visibility BarsVisibility(int noteLines) => Show(noteLines == 0);
    private Visibility NoteVisibility(int noteLines) => Show(noteLines > 0);
    private Visibility TodayLabelVisibility(bool hasStrip, bool hasNote) => Show(hasStrip || hasNote);
    private Visibility FooterVisibility(CapEditorViewModel? editor) => Show(editor is null);
    private Visibility EditorVisibility(CapEditorViewModel? editor) => Show(editor is not null);
    private KeyboardNavigationMode BarTabNavigation(bool editing) => editing ? KeyboardNavigationMode.Local : KeyboardNavigationMode.Once;
    private Visibility ShowNoteAction(bool hasAction, bool editing) => Show(hasAction && !editing);
    private Visibility ShowCompactOver(bool hasOver, bool compact) => Show(compact && hasOver);
    private Visibility ShowComfortableOver(bool hasOver, bool compact) => Show(!compact && hasOver);

    private Brush SurfaceFill(bool fresh, bool section) => section ? LedgerTheme.Solid("Transparent") : (Brush)LedgerTheme.Find(fresh ? "LedgerCardFreshSurfaceBrush" : "LedgerCardSurfaceBrush")!;
    private Brush SurfaceStroke(bool fresh, bool section) => LedgerTheme.Solid(section ? "Transparent" : fresh ? "OkP" : "LineCard");
    private Brush NameBrush(bool stale) => LedgerTheme.Solid(stale ? "Ink2" : "Ink");
    private Brush HistoryBackground(bool open) => LedgerTheme.Solid(open ? "ControlOn" : "Transparent");
    private Brush PillBackground(Tone tone) => LedgerTheme.TonePill(tone);
    private Brush PillDot(Tone tone) => LedgerTheme.ToneMark(tone);
    private Brush PillText(Tone tone) => LedgerTheme.ToneText(tone);

    private Thickness CardPadding(bool compact, bool section) => section ? new Thickness(0) : compact ? new Thickness(12, 8, 12, 8) : new Thickness(15, 13, 15, 12);
    private double InnerGap(bool compact) => compact ? 6 : 10;
    private double StripGap(bool compact, bool hasStrip) => hasStrip ? (compact ? 3 : 8) : 0;
    private Thickness BarMargin(bool compact) => compact ? new Thickness(0, -2, 0, -2) : new Thickness(0);

    private string FooterName(string footer, IReadOnlyList<string> tip, bool canEditCap) =>
        LedgerViews.Spoken([footer, .. tip]) + (canEditCap ? ". Press to edit the cap" : string.Empty);
}
