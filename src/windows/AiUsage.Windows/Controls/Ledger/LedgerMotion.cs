using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;

namespace AiUsage.Controls.Ledger;

internal static class LedgerMotion
{
    public static void FadeIn(FrameworkElement element, int milliseconds)
    {
        if (LedgerTheme.AnimationsEnabled)
            Animate(element, "Opacity", 0, element.Opacity, milliseconds);
    }

    public static void WidthOnLoad(FrameworkElement element, double? previousWidth)
    {
        if (previousWidth is not { } from || !LedgerTheme.AnimationsEnabled) return;
        void Loaded(object sender, RoutedEventArgs args)
        {
            element.Loaded -= Loaded;
            if (LedgerTheme.AnimationsEnabled && Math.Abs(element.ActualWidth - from) > .1)
                Animate(element, "Width", from, element.ActualWidth, 200);
        }
        element.Loaded += Loaded;
    }

    /// <summary>
    /// Grows or shrinks a side panel's width; the end width is set first, so an interrupted slide lands on it. Done runs
    /// when the slide ends (at once without animations); the returned storyboard lets a new slide stop this one.
    /// </summary>
    public static Storyboard? SlideWidth(FrameworkElement element, double from, double to, Action done)
    {
        element.Width = to;
        if (!LedgerTheme.AnimationsEnabled || Math.Abs(to - from) < .1)
        {
            done();
            return null;
        }
        var storyboard = Animate(element, "Width", from, to, 200);
        storyboard.Completed += (_, _) => done();
        return storyboard;
    }

    private static Storyboard Animate(FrameworkElement target, string property, double from, double to, int milliseconds)
    {
        var animation = new DoubleAnimation
        {
            From = from, To = to, Duration = TimeSpan.FromMilliseconds(milliseconds),
            EnableDependentAnimation = property == "Width",
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
        };
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, property);
        var storyboard = new Storyboard();
        storyboard.Children.Add(animation);
        storyboard.Completed += (_, _) => storyboard.Stop();
        Composition.ApplicationDiagnostics.RunAnimation(storyboard.Begin);
        return storyboard;
    }
}
