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
    private const string UpgradePreferences = "{\"Version\":1,\"Theme\":2,\"AlwaysOnTop\":true,\"Labels\":{\"opaque/provider\":\"Synthetic checkpoint\"},\"future\":{\"raw\":[null,7]}}";

    [Fact(Explicit = true)]
    public void UpgradeRecovery()
    {
        var exe = Environment.GetEnvironmentVariable("AIU_SMOKE_EXE");
        var aumid = Environment.GetEnvironmentVariable("AIU_SMOKE_AUMID");
        var packaged = string.IsNullOrEmpty(exe);
        var evidence = Environment.GetEnvironmentVariable("AIU_SMOKE_EVIDENCE_DIRECTORY")!;
        Assert.False(string.IsNullOrWhiteSpace(evidence));
        Directory.CreateDirectory(evidence);
        var priorDirectory = Environment.GetEnvironmentVariable("AIU_DEVELOPMENT_STATE_DIRECTORY");
        string root;
        if (packaged)
        {
            Assert.Equal("WDAGUtilityAccount", Environment.UserName);
            Assert.False(string.IsNullOrWhiteSpace(aumid));
            root = Environment.GetEnvironmentVariable("AIU_RECOVERY_GUEST_ROOT")!;
            Assert.StartsWith(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "AiUsage.Dev_"), root);
            Assert.Equal("LocalState", Path.GetFileName(root));
        }
        else
        {
            Assert.False(string.IsNullOrWhiteSpace(priorDirectory));
            root = Path.Combine(priorDirectory!, "recovery-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            Environment.SetEnvironmentVariable("AIU_DEVELOPMENT_STATE_DIRECTORY", root);
            File.WriteAllText(Path.Combine(root, "appearance.v1.json"), UpgradePreferences);
        }

        using var automation = new UIA3Automation();
        var phase = Environment.GetEnvironmentVariable("AIU_UPGRADE_PHASE");
        var legacy = Path.Combine(root, "appearance.v1.json");
        var target = Path.Combine(root, "preferences", "appearance.v1.json");
        var original = File.ReadAllBytes(legacy);
        var passed = false;
        try
        {
            if (phase == "old")
            {
                Session("old-package", window =>
                {
                    Assert.True(WaitUntil(() => SettingsEntry(window)?.IsEnabled == true, TimeSpan.FromSeconds(15)));
                    SettingsEntry(window)!.Click();
                    Assert.True(WaitUntil(() => AlwaysOnTopLoaded(window), TimeSpan.FromSeconds(10)));
                    Capture(window, evidence, "old-preferences");
                });
                Assert.Equal(original, File.ReadAllBytes(legacy));
                passed = true;
                return;
            }

            // A real filesystem sharing violation interrupts the installed product after checkpoint and target publication.
            // No product-only test switch or substitute migration implementation is used.
            using (var barrier = new FileStream(Path.Combine(root, "layout.v1.json.new"), FileMode.CreateNew, FileAccess.Write, FileShare.None))
                Session("interrupted", window =>
                {
                    WaitForRecovery(window, "Local data needs recovery");
                    Assert.True(File.Exists(Path.Combine(root, "maintenance", "journal.v1.json")));
                    Assert.Equal(original, File.ReadAllBytes(legacy));
                    Capture(window, evidence, "interrupted-before-restart");
                }, terminate: true);
            var checkpoint = File.ReadAllBytes(Path.Combine(root, "maintenance", "checkpoint.v1.bin"));
            Session("retry", window =>
            {
                WaitForRecovery(window, "Local data needs recovery");
                Required(window, "Retry recovery").AsButton().Invoke();
                WaitForDashboard(window);
                Assert.Equal(original, File.ReadAllBytes(target));
                Capture(window, evidence, "retry-dashboard");
            });
            Assert.Equal(checkpoint, File.ReadAllBytes(Path.Combine(root, "maintenance", "checkpoint.v1.bin")));
            File.WriteAllText(target, "deliberately-corrupted-synthetic-preferences");
            Session("restore", window =>
            {
                WaitForRecovery(window, "Local data needs recovery");
                Required(window, "Preview diagnostics").AsButton().Invoke();
                Capture(window, evidence, "restore-diagnostics");
                Required(window, "Restore legacy preferences").AsButton().Invoke();
                WaitForDashboard(window);
                Assert.Equal(original, File.ReadAllBytes(target));
                Capture(window, evidence, "restored-dashboard");
            });
            Assert.Equal(checkpoint, File.ReadAllBytes(Path.Combine(root, "maintenance", "checkpoint.v1.bin")));
            File.WriteAllText(Path.Combine(root, "layout.v1.json"), "{\"Version\":1,\"Layout\":99}");
            Session("newer-schema", window =>
            {
                WaitForRecovery(window, "These local data were written by a newer app");
                Assert.False(Required(window, "Retry recovery").IsEnabled);
                Assert.False(Required(window, "Restore legacy preferences").IsEnabled);
                Required(window, "Export recovery summary").AsButton().Invoke();
                var diagnostics = Path.Combine(root, "recovery-diagnostics.txt");
                Assert.True(WaitUntil(() => File.Exists(diagnostics), TimeSpan.FromSeconds(5)));
                var diagnosticText = File.ReadAllText(diagnostics);
                Assert.Contains("Condition: NewerSchema", diagnosticText, StringComparison.Ordinal);
                Assert.DoesNotContain("opaque/provider", diagnosticText, StringComparison.Ordinal);
                Assert.DoesNotContain(root, diagnosticText, StringComparison.Ordinal);
                window.Focus();
                Capture(window, evidence, "newer-schema-blocked");
            });
            Assert.Equal(original, File.ReadAllBytes(target));
            // Return the synthetic fixture to its previously committed layout for later guest inspection.
            File.WriteAllText(Path.Combine(root, "layout.v1.json"), "{\"Version\":1,\"Layout\":2}");
            passed = true;
        }
        finally
        {
            Environment.SetEnvironmentVariable("AIU_DEVELOPMENT_STATE_DIRECTORY", priorDirectory);
            File.WriteAllText(Path.Combine(evidence, "upgrade-recovery-result.json"), JsonSerializer.Serialize(new { passed, packaged, phase, utc = DateTime.UtcNow }));
        }

        void Session(string name, Action<Window> check, bool terminate = false)
        {
            using var app = packaged ? Application.LaunchStoreApp(aumid!) : Application.Launch(exe!);
            using var process = Process.GetProcessById(app.ProcessId);
            _ = process.Handle;
            Window? window = null;
            try
            {
                Assert.True(WaitUntil(() => (window = FindRecoveryWindow(automation, process.Id)) is not null, TimeSpan.FromSeconds(30)));
                window!.Patterns.Window.Pattern.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Maximized);
                check(window);
                if (terminate) { process.Kill(); Assert.True(process.WaitForExit(10000)); return; }
                FocusForKeyboard(window, window, evidence, name + "-exit");
                Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_Q);
                if (phase == "old") ConfirmLegacyExit(automation, process.Id);
                Assert.True(process.WaitForExit(10000));
                Assert.Equal(0, process.ExitCode);
            }
            finally
            {
                if (!process.HasExited)
                {
                    if (window is not null) Capture(window, evidence, name + "-failure");
                    process.Kill();
                    process.WaitForExit(10000);
                }
            }
        }
    }

    private static int GetOwnedProcessId(Window window)
    {
        Assert.NotEqual(0u, GetWindowThreadProcessId(window.Properties.NativeWindowHandle.Value, out var pid));
        return checked((int)pid);
    }

    private static Window? FindRecoveryWindow(UIA3Automation automation, int pid) =>
        automation.GetDesktop().FindAllChildren().FirstOrDefault(element =>
        {
            var handle = element.Properties.NativeWindowHandle.ValueOrDefault;
            return handle != IntPtr.Zero && GetWindowThreadProcessId(handle, out var owner) != 0 && owner == (uint)pid &&
                (element.FindFirstDescendant(cf => cf.ByAutomationId("RecoveryTitle")) is not null ||
                 SettingsEntry(element) is not null || element.Properties.Name.ValueOrDefault == "AI Usage");
        })?.AsWindow();

    private static void WaitForRecovery(Window window, string title)
    {
        Assert.True(WaitUntil(() => window.FindAllDescendants().Any(e => (e.Properties.Name.ValueOrDefault ?? "").StartsWith(title, StringComparison.Ordinal)), TimeSpan.FromSeconds(15)));
        SettingsEntry(window)!.AsButton().Invoke();
        Assert.True(WaitUntil(() => window.FindFirstDescendant(cf => cf.ByName("Retry recovery")) is not null, TimeSpan.FromSeconds(5)));
    }
    private static void WaitForDashboard(Window window) => Assert.True(WaitUntil(() =>
        (window.FindFirstDescendant(cf => cf.ByName("Sign in to Codex")) is not null ||
         window.FindFirstDescendant(cf => cf.ByAutomationId("CardScroller")) is not null) &&
        !window.FindAllDescendants().Any(e => (e.Properties.Name.ValueOrDefault ?? "").StartsWith("Local data needs recovery", StringComparison.Ordinal)), TimeSpan.FromSeconds(15)));
}
