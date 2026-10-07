using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AiUsage.Controls.Ledger;

// WinUI shows the arrow over buttons; every clickable Ledger control shows the hand instead.
// ProtectedCursor is only settable from a subclass, so each clickable kind gets one.

/// <summary>A Ledger button: hand cursor while enabled, and the wash brushes its template lays over the surface on hover and press.</summary>
internal sealed partial class LedgerButton : Button
{
    public static readonly DependencyProperty HoverBrushProperty =
        DependencyProperty.Register(nameof(HoverBrush), typeof(Brush), typeof(LedgerButton), new PropertyMetadata(null));
    public static readonly DependencyProperty PressedBrushProperty =
        DependencyProperty.Register(nameof(PressedBrush), typeof(Brush), typeof(LedgerButton), new PropertyMetadata(null));

    public LedgerButton()
    {
        ProtectedCursor = LedgerCursor.Hand;
        IsEnabledChanged += (_, _) => ProtectedCursor = IsEnabled ? LedgerCursor.Hand : LedgerCursor.Arrow;
    }

    public Brush HoverBrush { get => (Brush)GetValue(HoverBrushProperty); set => SetValue(HoverBrushProperty, value); }
    public Brush PressedBrush { get => (Brush)GetValue(PressedBrushProperty); set => SetValue(PressedBrushProperty, value); }
}

internal sealed partial class LedgerCheckBox : CheckBox
{
    public LedgerCheckBox() => ProtectedCursor = LedgerCursor.Hand;
}

/// <summary>A tappable row, such as an account row in the tray flyout.</summary>
internal sealed partial class LedgerClickRow : ContentControl
{
    public LedgerClickRow() => ProtectedCursor = LedgerCursor.Hand;
}

// A cursor is closable, so each element gets its own instance rather than sharing one.
internal static class LedgerCursor
{
    public static InputSystemCursor Hand => InputSystemCursor.Create(InputSystemCursorShape.Hand);
    public static InputSystemCursor Arrow => InputSystemCursor.Create(InputSystemCursorShape.Arrow);
}
