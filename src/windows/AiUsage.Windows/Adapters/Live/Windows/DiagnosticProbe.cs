using AiUsage.Core.Diagnostics;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace AiUsage.Composition;

/// <summary>Explicit offline test entry, requiring a disposable temp root; never starts provider services.</summary>
internal static class DiagnosticProbe
{
    internal static bool TryStart(ApplicationDiagnostics diagnostics, Action exit)
    {
        var kind = Environment.GetEnvironmentVariable("AIU_DIAGNOSTIC_PROBE");
        if (kind is not ("ui" or "dispatcher" or "binding" or "converter" or "animation" or "background" or "stall")) return false;
        var root = Environment.GetEnvironmentVariable("AIU_DEVELOPMENT_STATE_DIRECTORY");
        if (root is null || !Path.GetFullPath(root).StartsWith(Path.Combine(Path.GetTempPath(), "aiu-ui-probe-"), StringComparison.OrdinalIgnoreCase) ||
            Directory.Exists(Path.Combine(root, "providers"))) return false;
        var panel = new Grid();
        var window = new Window { Content = panel, Title = "AI Usage diagnostic probe" };
        window.Activate();
        var queue = DispatcherQueue.GetForCurrentThread();
        queue.TryEnqueue(() =>
        {
            switch (kind)
            {
                case "ui": RaiseAsync(); break;
                case "dispatcher": new Platform.UiDispatcher(queue, diagnostics).Post(() => throw new InvalidOperationException("dispatcher-secret-canary")); break;
                case "converter": _ = Controls.Bind.Token(panel, "missing-secret-canary"); break;
                case "animation": ApplicationDiagnostics.RunAnimation(() => throw new InvalidOperationException("animation-secret-canary")); break;
                case "background": _ = ObserveAsync(); break;
                case "stall": Thread.Sleep(TimeSpan.FromSeconds(14)); break;
                case "binding":
                    var text = new TextBlock();
                    text.SetBinding(TextBlock.TextProperty, new Binding { Source = new object(), Path = new PropertyPath("missing-secret-canary") });
                    panel.Children.Add(text);
                    break;
            }
            if (kind is "ui" or "dispatcher") return; // Fatal probes must terminate through the runtime, never our timer.
            var timer = queue.CreateTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.IsRepeating = false;
            timer.Tick += (_, _) => { timer.Stop(); window.Close(); diagnostics.Dispose(); exit(); };
            timer.Start();
        });
        return true;

        static async void RaiseAsync()
        {
            await Task.Yield();
            throw new InvalidOperationException("ui-secret-canary");
        }

        async Task ObserveAsync()
        {
            try { await Task.Yield(); throw new InvalidOperationException("background-secret-canary"); }
            catch (Exception exception) { diagnostics.Failure(DiagnosticEvent.BackgroundFailure, exception); }
        }
    }
}
