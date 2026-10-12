using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace AiUsage.Controls.Ledger;

/// <summary>
/// The app's motion (owner picks 2026-10-11): 150 ms for small elements, 250 ms for panels and 200 ms for each step of a
/// drag (lift, gap, settle), all on one curve, a quadratic ease-out. A usage bar's fill follows <see cref="Features.Ledger.BarSweep"/>,
/// which eases its last move the same way. With system animations off nothing moves. The sign-in wave and the install dots
/// are progress loops, not transitions, and keep their own timing.
/// </summary>
internal static class LedgerMotion
{
    public const int Small = 150;
    public const int Panel = 250;
    public const int Drag = 200;

    /// <summary>The one easing curve.</summary>
    public static double Ease(double progress) => 1 - (1 - progress) * (1 - progress);

    public static void FadeIn(FrameworkElement element, int milliseconds)
    {
        if (LedgerTheme.AnimationsEnabled)
            Animate(element, "Opacity", 0, element.Opacity, milliseconds);
    }

    /// <summary>
    /// Grows or shrinks a sheet's height; the end height is set first, so an interrupted slide lands on it. Done runs
    /// when the slide ends (at once without animations); the returned storyboard lets a new slide stop this one.
    /// </summary>
    public static Storyboard? SlideHeight(FrameworkElement element, double from, double to, Action done)
    {
        element.Height = to;
        if (!LedgerTheme.AnimationsEnabled || Math.Abs(to - from) < .1)
        {
            done();
            return null;
        }
        var storyboard = Animate(element, "Height", from, to, Panel);
        storyboard.Completed += (_, _) => done();
        return storyboard;
    }

    /// <summary>
    /// Calls frame with the elapsed milliseconds on every rendered frame until the duration has passed, ending on the
    /// duration itself. The returned action stops it early.
    /// </summary>
    public static Action Frames(double milliseconds, Action<double> frame)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        void Tick(object? sender, object e)
        {
            var elapsed = Math.Min(clock.Elapsed.TotalMilliseconds, milliseconds);
            if (elapsed >= milliseconds)
                CompositionTarget.Rendering -= Tick;
            Composition.ApplicationDiagnostics.RunAnimation(() => frame(elapsed));
        }
        CompositionTarget.Rendering += Tick;
        return () => CompositionTarget.Rendering -= Tick;
    }

    private static Storyboard Animate(FrameworkElement target, string property, double from, double to, int milliseconds)
    {
        var animation = new DoubleAnimation
        {
            From = from, To = to, Duration = TimeSpan.FromMilliseconds(milliseconds),
            EnableDependentAnimation = property is "Width" or "Height",
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

/// <summary>
/// A value that moves to a new target on <see cref="LedgerMotion"/>'s curve, frame by frame; a new target starts from
/// wherever the value is, so a slide that changes direction never jumps. Without system animations it jumps at once.
/// </summary>
internal sealed class Tween(Action<double> apply, double value)
{
    private Action? stop;

    public double Value { get; private set; } = value;

    public void To(double target, int milliseconds, Action? done = null)
    {
        stop?.Invoke();
        stop = null;
        var from = Value;
        if (!LedgerTheme.AnimationsEnabled || Math.Abs(target - from) < .01)
        {
            Set(target);
            done?.Invoke();
            return;
        }
        stop = LedgerMotion.Frames(milliseconds, elapsed =>
        {
            Value = from + (target - from) * LedgerMotion.Ease(elapsed / milliseconds);
            apply(Value);
            if (elapsed < milliseconds)
                return;
            stop = null;
            done?.Invoke();
        });
    }

    /// <summary>Stops a running move and shows this value.</summary>
    public void Set(double target)
    {
        stop?.Invoke();
        stop = null;
        Value = target;
        apply(target);
    }
}
