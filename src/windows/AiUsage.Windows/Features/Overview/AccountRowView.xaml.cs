using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace AiUsage.Features.Overview;

/// <summary>
/// Compact Overview account row (D-181). Pointer hover and keyboard focus reveal refresh without changing height;
/// Enter opens detail; Alt+↑/↓ and drag both reorder within the provider with the same announced outcome.
/// </summary>
internal sealed partial class AccountRowView : UserControl
{
    private static string? draggingId;
    private static string? draggingProvider;
    private AccountRowViewModel? row;
    private bool pointerOver;

    public AccountRowView()
    {
        InitializeComponent();
        PointerEntered += (_, _) => { pointerOver = true; RefreshHover(); };
        PointerExited += (_, _) => { pointerOver = false; RefreshHover(); };
        GotFocus += (_, _) => RefreshHover();
        LostFocus += (_, _) => DispatcherQueue.TryEnqueue(RefreshHover);
        KeyDown += OnKeyDown;
        DragStarting += OnDragStarting;
        DragOver += OnDragOver;
        Drop += OnDrop;
        DropCompleted += (_, _) => (draggingId, draggingProvider) = (null, null);
    }

    /// <summary>The page registers its view model so drops can reorder; rows are created by item templates.</summary>
    public static OverviewViewModel? Owner { get; set; }

    public AccountRowViewModel Row
    {
        get => row!;
        set
        {
            row = value;
            Bindings.Update();
        }
    }

    private string AutomationIdFor(string id) => "Row_" + id;
    private double RevealOpacity(bool hovered, bool refreshing) => hovered || refreshing ? 1 : 0;
    private double StaleOpacity(bool stale) => stale ? 0.45 : 1;
    private Thickness CompactPadding(bool compactDensity) => compactDensity ? new Thickness(0, 5, 0, 5) : new Thickness(0, 9, 0, 9);

    /// <summary>Segoe Fluent glyphs: warning triangle, clock for a stale reading, error circle for a failed refresh.</summary>
    private string StatusGlyph(RowStatus status) => status switch
    {
        RowStatus.Attention => "",
        RowStatus.Stale => "",
        RowStatus.Failure => "",
        _ => string.Empty,
    };

    /// <summary>The hover refresh button cancels while a refresh runs.</summary>
    private System.Windows.Input.ICommand? RefreshAction(bool refreshing) => row is null ? null : refreshing ? row.CancelRefreshCommand : row.RefreshCommand;

    protected override AutomationPeer OnCreateAutomationPeer() => new RowPeer(this);

    private void SetHover(bool hovered)
    {
        if (row is null)
            return;
        row.IsHovered = hovered;
        RowBorder.Background = hovered ? Controls.Bind.Token(this, "Card2Brush") : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
    }

    private void RefreshHover()
    {
        if (XamlRoot is null)
            return;
        var focused = FocusManager.GetFocusedElement(XamlRoot) as DependencyObject;
        var inside = false;
        for (var node = focused; node is not null; node = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(node))
            if (node == this)
            {
                inside = true;
                break;
            }
        SetHover(inside || pointerOver);
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (row is null)
            return;
        var alt = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        if (e.Key == VirtualKey.Enter && ReferenceEquals(e.OriginalSource, this))
        {
            row.OpenCommand.Execute(null);
            e.Handled = true;
        }
        else if (alt && e.Key == VirtualKey.Up)
        {
            if (row.MoveUpCommand.CanExecute(null))
                row.MoveUpCommand.Execute(null);
            e.Handled = true;
            RestoreFocusSoon();
        }
        else if (alt && e.Key == VirtualKey.Down)
        {
            if (row.MoveDownCommand.CanExecute(null))
                row.MoveDownCommand.Execute(null);
            e.Handled = true;
            RestoreFocusSoon();
        }
    }

    /// <summary>After a reorder the repeater may re-realize the row; keep keyboard focus on the moved account.</summary>
    private void RestoreFocusSoon()
    {
        var id = row?.Id;
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            if (row?.Id == id && FocusState == FocusState.Unfocused)
                Focus(FocusState.Keyboard);
        });
    }

    private void OnDragStarting(UIElement sender, DragStartingEventArgs args)
    {
        if (row is null)
        {
            args.Cancel = true;
            return;
        }
        draggingId = row.Id;
        draggingProvider = row.ProviderId;
        args.Data.SetText(row.Id);
        args.Data.RequestedOperation = DataPackageOperation.Move;
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (row is not null && draggingId is not null && draggingId != row.Id && draggingProvider == row.ProviderId)
        {
            e.AcceptedOperation = DataPackageOperation.Move;
            e.DragUIOverride.IsGlyphVisible = false;
            e.DragUIOverride.IsCaptionVisible = false;
        }
        else
            e.AcceptedOperation = DataPackageOperation.None;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (row is null || draggingId is null || Owner is null)
            return;
        _ = Owner.DropAsync(draggingId, row.Id);
        draggingId = null;
    }

    private sealed partial class RowPeer(AccountRowView owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ListItem;
        protected override string GetClassNameCore() => nameof(AccountRowView);
        protected override bool IsKeyboardFocusableCore() => true;
    }
}
