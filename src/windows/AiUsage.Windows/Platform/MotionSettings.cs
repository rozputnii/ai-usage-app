using System.ComponentModel;
using AiUsage.Features.Presentation;
using Microsoft.UI.Dispatching;
using Windows.UI.ViewManagement;

namespace AiUsage.Platform;

/// <summary>
/// Reduced motion follows the Windows animation setting unless the demo overrides it. Decorative animation also stops
/// while the window is hidden to the tray. Controls read <see cref="Current"/> because they are created by XAML.
/// </summary>
internal sealed class MotionSettings : IMotionSettings
{
    private readonly UISettings settings = new();
    private readonly DispatcherQueue queue;
    private bool systemAnimations;

    public MotionSettings(DispatcherQueue queue)
    {
        this.queue = queue;
        systemAnimations = settings.AnimationsEnabled;
        settings.AnimationsEnabledChanged += (_, _) => queue.TryEnqueue(() =>
        {
            systemAnimations = settings.AnimationsEnabled;
            Raise();
        });
        Current = this;
    }

    public static MotionSettings? Current { get; private set; }

    public static bool Allowed => Current?.AnimationsAllowed ?? true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool? Override
    {
        get;
        set
        {
            field = value;
            Raise();
        }
    }

    public bool ReducedMotion => Override ?? !systemAnimations;

    public bool WindowVisible
    {
        get;
        set
        {
            if (field == value)
                return;
            field = value;
            Raise();
        }
    } = true;

    public bool AnimationsAllowed => !ReducedMotion && WindowVisible;

    private void Raise()
    {
        if (!queue.HasThreadAccess)
        {
            queue.TryEnqueue(Raise);
            return;
        }
        PropertyChanged?.Invoke(this, new(nameof(ReducedMotion)));
        PropertyChanged?.Invoke(this, new(nameof(AnimationsAllowed)));
    }
}
