using AiUsage.Features.Accounts;
using AiUsage.Features.Presentation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AiUsage.Controls;

/// <summary>Presentation gating complements adapter-side rejection. Demo controls retain their existing behavior.</summary>
public static class CapabilityGate
{
    internal static IUsageSource? Source { get; set; }
    public static readonly DependencyProperty KeyProperty = DependencyProperty.RegisterAttached(
        "Key", typeof(string), typeof(CapabilityGate), new PropertyMetadata(null, Changed));
    public static string GetKey(DependencyObject target) => (string)target.GetValue(KeyProperty);
    public static void SetKey(DependencyObject target, string value) => target.SetValue(KeyProperty, value);
    private static void Changed(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        if (target is not Control control) return;
        void Apply()
        {
            if (Source?.Current is not { Mode: UiMode.Live } snapshot) return;
            control.IsEnabled = QuotaRules.IsAvailable(snapshot, (string)args.NewValue);
            if (!control.IsEnabled)
                ToolTipService.SetToolTip(control, new Platform.ResourceText().Get("Capability_Unavailable"));
        }
        control.Loaded += (_, _) => Apply();
        Apply();
    }
}
