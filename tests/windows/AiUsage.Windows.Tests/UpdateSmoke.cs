using System.Diagnostics;
using System.Text.Json;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using Xunit;

namespace AiUsage.Windows.Tests;

public sealed partial class ShellSmoke
{
    [Fact(Explicit = true)]
    public void AppInstallerActivationPreservesPreferences()
    {
        Assert.Equal("WDAGUtilityAccount", Environment.UserName);
        var aumid = Environment.GetEnvironmentVariable("AIU_SMOKE_AUMID");
        var expected = Environment.GetEnvironmentVariable("AIU_UPDATE_EXPECTED_VERSION");
        var evidence = Environment.GetEnvironmentVariable("AIU_SMOKE_EVIDENCE_DIRECTORY");
        Assert.False(string.IsNullOrWhiteSpace(aumid));
        Assert.False(string.IsNullOrWhiteSpace(expected));
        Assert.False(string.IsNullOrWhiteSpace(evidence));
        Directory.CreateDirectory(evidence!);
        using var automation = new UIA3Automation();
        using var app = Application.LaunchStoreApp(aumid!);
        using var process = Process.GetProcessById(app.ProcessId);
        _ = process.Handle;
        Window? window = null;
        try
        {
            Assert.Contains("AiUsage.Dev_" + expected + "_x64__", process.MainModule!.FileName, StringComparison.OrdinalIgnoreCase);
            Assert.True(WaitUntil(() => (window = FindRecoveryWindow(automation, process.Id)) is not null, TimeSpan.FromSeconds(30)));
            window!.Patterns.Window.Pattern.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Maximized);
            WaitForDashboard(window);
            Required(window, "NavSettings").Click();
            Assert.True(WaitUntil(() => window.FindFirstDescendant(cf => cf.ByAutomationId("ThemeNote"))?.Name.Contains("Dark", StringComparison.Ordinal) == true,
                TimeSpan.FromSeconds(10)), "The installed version must load the durable Dark preference.");
            Thread.Sleep(500);
            Capture(window, evidence!, "installed-preferences");
            FocusForKeyboard(window, window, evidence!, "installed-exit");
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_Q);
            ConfirmDialog(automation, process.Id, window, evidence!, "installed-exit").Invoke();
            Assert.True(process.WaitForExit(10000));
            Assert.Equal(0, process.ExitCode);
            File.WriteAllText(Path.Combine(evidence!, "activation.json"),
                JsonSerializer.Serialize(new { passed = true, installedVersion = expected, utc = DateTime.UtcNow }));
        }
        finally
        {
            if (!process.HasExited)
            {
                if (window is not null) Capture(window, evidence!, "activation-failure");
                process.Kill();
                process.WaitForExit(10000);
            }
        }
    }
}
