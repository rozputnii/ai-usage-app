using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace AiUsage.Windows.Tests;

/// <summary>Guest package activation and shared migration helpers. Local Ledger checks use LedgerSmoke.</summary>
public sealed partial class ShellSmoke
{
    [Fact(Explicit = true)]
    public void PackagedLedgerLaunchesAndExits()
    {
        Assert.Equal("WDAGUtilityAccount", Environment.UserName);
        var aumid = Environment.GetEnvironmentVariable("AIU_SMOKE_AUMID");
        var evidence = Environment.GetEnvironmentVariable("AIU_SMOKE_EVIDENCE_DIRECTORY");
        Assert.False(string.IsNullOrWhiteSpace(aumid)); Assert.False(string.IsNullOrWhiteSpace(evidence));
        Directory.CreateDirectory(evidence!);
        File.WriteAllText(Path.Combine(evidence!, "package-activation.json"), JsonSerializer.Serialize(new { phase = "activation-attempted" }));
        using var automation = new UIA3Automation();
        using var app = Application.LaunchStoreApp(aumid!);
        using var process = Process.GetProcessById(app.ProcessId);
        _ = process.Handle;
        Window? window = null;
        try
        {
            Assert.True(WaitUntil(() => (window = FindRecoveryWindow(automation, process.Id)) is not null, TimeSpan.FromSeconds(30)));
            WaitForDashboard(window!);
            SettingsEntry(window!)!.AsButton().Invoke();
            Assert.NotNull(window!.FindFirstDescendant(cf => cf.ByName("Delete stored data").And(cf.ByControlType(ControlType.Button))));
            Capture(window, evidence!, "package-settings");
            FocusForKeyboard(window, window, evidence!, "package-exit");
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_Q);
            Assert.True(process.WaitForExit(10000)); Assert.Equal(0, process.ExitCode);
        }
        finally { if (!process.HasExited) { process.Kill(); process.WaitForExit(5000); } }
    }

    // The old-package phase deliberately supports the historical presentation; the new phase uses Ledger.
    private static AutomationElement? SettingsEntry(AutomationElement window) =>
        window.FindFirstDescendant(cf => cf.ByName("Settings").And(cf.ByControlType(ControlType.Button))) ??
        window.FindFirstDescendant(cf => cf.ByAutomationId("SettingsButton"));
    private static bool AlwaysOnTopLoaded(AutomationElement window) =>
        window.FindFirstDescendant(cf => cf.ByName("Always on top, on")) is not null ||
        window.FindFirstDescendant(cf => cf.ByAutomationId("AlwaysOnTopSwitch"))?.Patterns.Toggle.PatternOrDefault?.ToggleState.Value == ToggleState.On;
    private static AutomationElement Required(Window window, string name) =>
        window.FindFirstDescendant(cf => cf.ByName(name).And(cf.ByControlType(ControlType.Button))) ?? throw new InvalidOperationException("Missing UI action: " + name);
    private static void Capture(Window window, string evidence, string name)
    {
        using var screenshot = window.Capture();
        screenshot.Save(Path.Combine(evidence, name + ".png"), System.Drawing.Imaging.ImageFormat.Png);
    }
    private static bool WaitUntil(Func<bool> check, TimeSpan timeout)
    {
        var elapsed = Stopwatch.StartNew();
        while (elapsed.Elapsed < timeout)
        {
            TestContext.Current.CancellationToken.ThrowIfCancellationRequested();
            try { if (check()) return true; }
            catch (COMException) { /* WinUI can replace the queried element while publishing a snapshot. */ }
            Thread.Sleep(100);
        }
        return false;
    }
    private static void FocusForKeyboard(Window window, AutomationElement target, string evidence, string operation)
    {
        var handle = window.Properties.NativeWindowHandle.Value;
        window.SetForeground();
        if (GetForegroundWindow() != handle) window.TitleBar!.Click();
        Assert.True(WaitUntil(() => GetForegroundWindow() == handle, TimeSpan.FromSeconds(5)));
    }
    private static void ConfirmLegacyExit(UIA3Automation automation, int pid)
    {
        AutomationElement? button = null;
        Assert.True(WaitUntil(() =>
        {
            button = automation.GetDesktop().FindAllChildren().Where(w =>
                GetWindowThreadProcessId(w.Properties.NativeWindowHandle.ValueOrDefault, out var owner) != 0 && owner == pid)
                .Select(w => w.FindFirstDescendant(cf => cf.ByAutomationId("ConfirmAccept"))).FirstOrDefault(b => b is not null);
            return button is not null;
        }, TimeSpan.FromSeconds(10)));
        button!.AsButton().Invoke();
    }
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
}
