using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using Xunit;

namespace AiUsage.Windows.Tests;

/// <summary>Physical input on the actual unpackaged build. Fixtures were exported after real parser/budget assertions.</summary>
public sealed class AuditWindows
{
    [Fact]
    public void ReplayPagesRenderUsedAndLeft()
    {
        DesktopTestEnvironment.RequireUnlockedDesktop();
        var pages = Required("AIU_AUDIT_PAGE_DIRECTORY");
        var files = Directory.EnumerateFiles(pages, "*.json").Where(p => !p.EndsWith(".expectations.json", StringComparison.Ordinal)).Order().ToArray();
        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            var pageId = Path.GetFileNameWithoutExtension(file);
            var input = JsonNode.Parse(File.ReadAllText(file))!.AsObject();
            using var session = new Session(file);
            var expected = input["ExpectedStates"]!.AsObject();
            if (expected.Any(p => p.Value!.GetValue<int>() == 18))
            {
                session.Click("Add account");
                session.Click("Show signed-out accounts");
                session.Key(VirtualKeyShort.ESCAPE);
            }
            foreach (var mode in new[] { "used", "left" })
            {
                session.Click("Show values: " + mode);
                foreach (var pair in expected)
                {
                    var card = session.ById(pair.Key);
                    session.Show(card);
                    var words = StateWords(pair.Value!.GetValue<int>());
                    var name = card.Properties.Name.Value;
                    // Monetary unavailable/disabled variants deliberately use their more specific reason.
                    Assert.True(name.Contains(words, StringComparison.OrdinalIgnoreCase) ||
                        (pair.Value.GetValue<int>() == 14 && name.Contains("no budget", StringComparison.OrdinalIgnoreCase)) ||
                        (pair.Value.GetValue<int>() == 15 && name.Contains("disabled", StringComparison.OrdinalIgnoreCase)), name);
                    Assert.Contains("SYNTHETIC", name, StringComparison.Ordinal);
                    Assert.True(card.BoundingRectangle.Width >= 250, "Cards must stay readable at normal window size");
                    session.Capture(pageId + "-" + mode + "-" + expected.ToList().FindIndex(p => p.Key == pair.Key).ToString(CultureInfo.InvariantCulture));
                }
                session.ScrollToTop();
                session.Capture(pageId + "-" + mode);
            }
            session.Exit();
            File.WriteAllText(Path.Combine(session.Evidence, pageId + ".ui-result.json"), JsonSerializer.Serialize(new
            {
                Page = pageId, ScenarioCards = expected.Select(p => p.Key).ToArray(), NativeAssertions = "PASS",
                MouseModes = new[] { "used", "left" }, VisualInspection = "NOT_RUN", ProcessExit = 0
            }));
        }
    }

    [Fact]
    public void SettingsEditorsHistoryAndSupportReceivePhysicalClicks()
    {
        DesktopTestEnvironment.RequireUnlockedDesktop();
        using var session = new Session(Path.Combine(Required("AIU_AUDIT_PAGE_DIRECTORY"), "overview.json"));
        session.Click("Settings");
        session.Capture("controls-settings-top");
        foreach (var name in new[] { "Density: comfortable", "Density: compact", "Always on top, off", "Always on top, on",
                     "Show signed-out accounts, off", "Show signed-out accounts, on" })
            session.Click(name);
        foreach (var day in new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" })
        {
            var button = session.Find(e => e.Properties.ControlType.ValueOrDefault == ControlType.Button &&
                (e.Properties.Name.ValueOrDefault ?? "").StartsWith(day + ",", StringComparison.Ordinal));
            var before = button.Properties.Name.Value;
            session.Show(button); session.Click(button);
            Assert.True(Session.Wait(() => !session.Find(e => (e.Properties.Name.ValueOrDefault ?? "").StartsWith(day + ",", StringComparison.Ordinal)).Properties.Name.Value.Equals(before, StringComparison.Ordinal)));
            session.Click(session.Find(e => (e.Properties.Name.ValueOrDefault ?? "").StartsWith(day + ",", StringComparison.Ordinal)));
        }
        foreach (var (button, receipt) in new[] { ("Preview diagnostics", "PreviewDiagnostics"), ("Open logs", "Support:OpenLogs"),
                     ("Open data folder", "Support:OpenDataFolder"), ("Export recovery summary", "Support:ExportRecovery") })
        {
            session.Click(button);
            Assert.True(Session.Wait(() => session.Receipts.Contains(receipt, StringComparison.Ordinal)), receipt);
        }
        Assert.True(File.Exists(Path.Combine(session.Root, "recovery-diagnostics.txt")));
        session.Capture("controls-settings-diagnostics");
        session.Click("Delete stored data"); session.Capture("controls-delete-confirmation");
        session.Click("Cancel deleting stored data");
        Assert.DoesNotContain("DeleteSyntheticData", session.Receipts, StringComparison.Ordinal);
        session.Click("Close settings");
        session.ScrollToTop();
        var input = JsonNode.Parse(File.ReadAllText(session.Input))!;
        var moneyAccount = input["Labels"]!.AsObject().Single(p => p.Value!.GetValue<string>().EndsWith("M-ordinary", StringComparison.Ordinal)).Key;
        var moneyId = input["ExpectedStates"]!.AsObject().Single(p => p.Key.StartsWith(moneyAccount + ":", StringComparison.Ordinal)).Key;
        var card = session.ById(moneyId);
        session.Show(card);
        card.Focus();
        session.Click(card.FindAllDescendants().Single(e => (e.Properties.Name.ValueOrDefault ?? "").StartsWith("History,", StringComparison.Ordinal)));
        var history = session.Find(e => (e.Properties.Name.ValueOrDefault ?? "").Contains(" history,", StringComparison.Ordinal));
        history.Focus();
        var previous = history.Properties.Name.Value;
        session.Key(VirtualKeyShort.LEFT);
        Assert.True(Session.Wait(() => history.Properties.Name.Value != previous));
        session.Capture("controls-money-history");
        session.Key(VirtualKeyShort.ESCAPE);
        card = session.ById(moneyId); session.Show(card); card.Focus();
        session.Key(VirtualKeyShort.F2);
        session.Type(session.Find(e => e.Properties.Name.ValueOrDefault == "Account name" && e.Properties.ControlType.ValueOrDefault == ControlType.Edit), "SYNTHETIC renamed account");
        session.Key(VirtualKeyShort.RETURN);
        Assert.True(Session.Wait(() => session.ById(moneyId).Properties.Name.Value.Contains("SYNTHETIC renamed account", StringComparison.Ordinal)));
        card = session.ById(moneyId); session.Show(card); card.Focus(); session.Key(VirtualKeyShort.KEY_C);
        var amount = session.Find(e => (e.Properties.Name.ValueOrDefault ?? "").StartsWith("Cap amount in", StringComparison.Ordinal));
        session.Type(amount, "€250.00"); session.Click("Save");
        Assert.NotNull(session.Find(e => (e.Properties.Name.ValueOrDefault ?? "").Contains("Enter a", StringComparison.Ordinal)));
        session.Capture("controls-invalid-cap");
        session.Type(amount, "250.00"); session.Click("Save");
        Assert.True(Session.Wait(() => session.ById(moneyId).Properties.Name.Value.Contains("cap", StringComparison.OrdinalIgnoreCase)));
        card = session.ById(moneyId); session.Show(card); card.Focus(); session.Key(VirtualKeyShort.KEY_C);
        session.Capture("controls-cap-editor"); session.Click("Remove");
        session.Key(VirtualKeyShort.F5);
        var accountId = moneyId.Split(':')[0];
        Assert.True(Session.Wait(() => session.Receipts.Contains("Refresh:" + accountId, StringComparison.Ordinal)));
        card = session.ById(moneyId); session.Show(card); card.Focus();
        session.Click("Sign out SYNTHETIC renamed account");
        Assert.True(Session.Wait(() => session.Receipts.Contains("Disconnect:" + accountId, StringComparison.Ordinal)));
        session.Click("Add account"); session.Click("Show signed-out accounts"); session.Key(VirtualKeyShort.ESCAPE);
        session.Click("Sign in");
        Assert.True(Session.Wait(() => session.Receipts.Contains("Connect:claude:" + accountId, StringComparison.Ordinal)));
        Assert.True(Session.Wait(() => session.ById(moneyId).Properties.Name.Value.Contains("SYNTHETIC renamed account", StringComparison.Ordinal)));
        session.Click("Settings"); session.Click("Delete stored data"); session.Click("Confirm deleting stored data");
        Assert.True(Session.Wait(() => session.Receipts.Contains("DeleteSyntheticData", StringComparison.Ordinal)));
        session.Click("Close settings");
        Assert.NotNull(session.Find(e => e.Properties.Name.ValueOrDefault == "Add an account"));
        session.Capture("controls-deleted-first-run"); session.Exit();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void MockAuthenticationCodeCancellationRetryAndOutcomes(int failure)
    {
        DesktopTestEnvironment.RequireUnlockedDesktop();
        var fixture = JsonNode.Parse(File.ReadAllText(Path.Combine(Required("AIU_AUDIT_PAGE_DIRECTORY"), "overview.json")))!.AsObject();
        fixture["Accounts"] = new JsonArray(); fixture["Observations"] = new JsonArray(); fixture["ExpectedStates"] = new JsonObject();
        fixture["Configuration"]!["Caps"] = new JsonArray(); fixture["ManualCode"] = true;
        fixture["SignInFailure"] = failure < 0 ? null : JsonValue.Create(failure);
        using var session = Session.FromFixture(fixture, "auth-" + failure.ToString(CultureInfo.InvariantCulture));
        session.Click("Sign in to Claude");
        Assert.True(Session.Wait(() => session.Receipts.Contains("BrowserRequested:synthetic", StringComparison.Ordinal)));
        session.Capture("auth-" + failure.ToString(CultureInfo.InvariantCulture) + "-waiting");
        session.Click("Cancel");
        Assert.True(Session.Wait(() => session.Receipts.Contains("CancelConnect", StringComparison.Ordinal)));
        session.Capture("auth-" + failure.ToString(CultureInfo.InvariantCulture) + "-cancelled");
        session.Click("Try again");
        session.Type(session.Find(e => e.Properties.Name.ValueOrDefault == "Sign-in code"), "invalid-synthetic-code");
        session.Click("Submit code");
        Assert.Contains("SubmitCode", session.Receipts, StringComparison.Ordinal);
        session.Type(session.Find(e => e.Properties.Name.ValueOrDefault == "Sign-in code"), "synthetic-code");
        session.Click("Submit code");
        Assert.True(Session.Wait(() => !session.Window.FindAllDescendants().Any(e => e.Properties.Name.ValueOrDefault == "Sign-in code" && !e.IsOffscreen)));
        session.Capture("auth-" + failure.ToString(CultureInfo.InvariantCulture) + "-result");
        if (failure >= 0) Assert.NotNull(session.Find(e => e.Properties.Name.ValueOrDefault == "Try again"));
        else Assert.NotNull(session.Find(e => (e.Properties.Name.ValueOrDefault ?? "").EndsWith(" added", StringComparison.Ordinal)));
        session.Exit();
    }

    private static string StateWords(int state) => state switch
    {
        0 => "OK", 1 => "today low", 2 => "today short", 3 => "cap close", 4 => "5 hour full", 5 => "today used", 6 => "over today",
        7 => "cap reached", 8 => "over cap", 9 => "used up", 10 => "day off", 11 => "rush", 12 => "not ready", 13 => "unknown",
        14 => "period unknown", 15 => "not included", 16 => "limit unknown", 17 => "no cap", 18 => "signed out", 19 => "No subscription limits to display",
        _ => throw new InvalidDataException("Unsupported synthetic state")
    };

    private static string Required(string variable) => Environment.GetEnvironmentVariable(variable) is { Length: > 0 } value
        ? value : throw new InvalidOperationException(variable + " is required");

    private sealed class Session : IDisposable
    {
        private readonly Application app;
        private readonly Process process;
        private readonly UIA3Automation automation = new();
        public string Input { get; }
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "aiu-ui-audit-" + Guid.NewGuid().ToString("N"));
        public string Evidence { get; } = Required("AIU_SMOKE_EVIDENCE_DIRECTORY");
        public Window Window { get; }
        public string Receipts => File.Exists(Path.Combine(Root, "requests.txt")) ? File.ReadAllText(Path.Combine(Root, "requests.txt")) : string.Empty;

        public Session(string input)
        {
            DesktopTestEnvironment.RequireUnlockedDesktop();
            Input = input;
            Directory.CreateDirectory(Evidence);
            var start = new ProcessStartInfo(Required("AIU_SMOKE_EXE")) { UseShellExecute = false };
            start.ArgumentList.Add("--demo"); start.ArgumentList.Add("--audit-input=" + input);
            start.Environment["AIU_DEVELOPMENT_STATE_DIRECTORY"] = Root;
            Application? launched = null;
            Process? launchedProcess = null;
            try
            {
                app = launched = Application.Launch(start);
                process = launchedProcess = Process.GetProcessById(app.ProcessId);
                _ = process.Handle;
                Window? window = null;
                Assert.True(Wait(() => (window = automation.GetDesktop().FindAllChildren().FirstOrDefault(w =>
                    w.Properties.ProcessId.ValueOrDefault == app.ProcessId && w.FindFirstDescendant(cf => cf.ByName("Settings").And(cf.ByControlType(ControlType.Button))) is not null)?.AsWindow()) is not null), "The isolated audit window did not start");
                Window = window!;
                Window.SetForeground();
                Assert.True(Wait(() => GetForegroundWindow() == Window.Properties.NativeWindowHandle.Value), "BLOCKED: test window must own foreground input");
                Assert.True(Wait(() => Window.FindAllDescendants().Any(e => e.Properties.Name.ValueOrDefault == "Add an account" || (e.Properties.Name.ValueOrDefault ?? "").Contains("SYNTHETIC", StringComparison.Ordinal))));
            }
            catch
            {
                // Construction can fail before the caller's using statement owns this session.
                // Only the process launched above is eligible for cleanup.
                try
                {
                    if (launchedProcess is { HasExited: false })
                    {
                        launchedProcess.Kill();
                        launchedProcess.WaitForExit(5000);
                    }
                    else if (launched is { HasExited: false }) launched.Kill();
                }
                finally
                {
                    launchedProcess?.Dispose();
                    launched?.Dispose();
                    automation.Dispose();
                }
                throw;
            }
        }
        public static Session FromFixture(JsonObject fixture, string id)
        {
            var path = Path.Combine(Required("AIU_AUDIT_PAGE_DIRECTORY"), id + ".fixture");
            File.WriteAllText(path, fixture.ToJsonString());
            return new(path);
        }
        public AutomationElement Find(Func<AutomationElement, bool> predicate)
        {
            AutomationElement? found = null;
            Assert.True(Wait(() => (found = OwnedSurfaces().SelectMany(w => w.FindAllDescendants()).FirstOrDefault(predicate)) is not null), "Required audit control was not found");
            return found!;
        }
        private IEnumerable<AutomationElement> OwnedSurfaces() => automation.GetDesktop().FindAllChildren().Where(w => w.Properties.ProcessId.ValueOrDefault == app.ProcessId);
        public AutomationElement ById(string id) => Find(e => e.Properties.AutomationId.ValueOrDefault == id);
        public void Click(string name) => Click(Find(e => e.Properties.Name.ValueOrDefault == name &&
            e.Properties.ControlType.ValueOrDefault is ControlType.Button or ControlType.CheckBox));
        public void Click(AutomationElement element)
        {
            DesktopTestEnvironment.RequireOwnedForeground(app.ProcessId);
            Show(element);
            Assert.True(element.IsEnabled, element.Properties.Name.ValueOrDefault);
            Assert.False(element.IsOffscreen);
            var point = element.GetClickablePoint();
            RequireMouseTarget(point);
            Mouse.MoveTo(point);
            RequireMouseTarget(point);
            Mouse.Click();
        }
        public void Show(AutomationElement element)
        {
            DesktopTestEnvironment.RequireUnlockedDesktop();
            Assert.Equal(app.ProcessId, element.Properties.ProcessId.Value);
            element.Patterns.ScrollItem.PatternOrDefault?.ScrollIntoView();
            if (element.IsOffscreen) element.Focus();
            var bounds = element.BoundingRectangle;
            var point = new System.Drawing.Point(bounds.Left + bounds.Width / 2, bounds.Top + Math.Min(15, bounds.Height / 2));
            RequireMouseTarget(point);
            Mouse.MoveTo(point);
        }
        public void ScrollToTop()
        {
            var bounds = Window.BoundingRectangle;
            var point = new System.Drawing.Point(bounds.Left + bounds.Width / 3, bounds.Top + bounds.Height / 2);
            RequireMouseTarget(point);
            Mouse.MoveTo(point);
            RequireMouseTarget(point);
            Mouse.Scroll(30);
            Thread.Sleep(200);
        }
        public void Key(VirtualKeyShort key)
        {
            DesktopTestEnvironment.RequireOwnedForeground(app.ProcessId);
            Keyboard.Type(key);
        }
        public void Type(AutomationElement element, string value)
        {
            Click(element);
            DesktopTestEnvironment.RequireOwnedForeground(app.ProcessId);
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
            foreach (var character in value)
            {
                DesktopTestEnvironment.RequireOwnedForeground(app.ProcessId);
                Keyboard.Type(character);
            }
        }
        public void Capture(string name)
        {
            RequireUnobscuredWindow();
            Assert.Equal(Window.Properties.NativeWindowHandle.Value, GetForegroundWindow());
            using var capture = Window.Capture();
            RequireUnobscuredWindow();
            capture.Save(Path.Combine(Evidence, name + ".png"), System.Drawing.Imaging.ImageFormat.Png);
        }
        public void Exit()
        {
            DesktopTestEnvironment.RequireUnlockedDesktop();
            Window.SetForeground();
            Assert.True(Wait(() => GetForegroundWindow() == Window.Properties.NativeWindowHandle.Value), "BLOCKED: test window must own exit input");
            DesktopTestEnvironment.RequireOwnedForeground(app.ProcessId);
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_Q);
            Assert.True(process.WaitForExit(10000), "The audit process must drain and exit");
            Assert.Equal(0, process.ExitCode);
            Assert.Contains("Stopped", Receipts, StringComparison.Ordinal);
        }
        public void Dispose()
        {
            if (!process.HasExited) { process.Kill(); process.WaitForExit(5000); }
            process.Dispose(); app.Dispose(); automation.Dispose();
        }
        private void RequireMouseTarget(System.Drawing.Point point)
        {
            DesktopTestEnvironment.RequireOwnedForeground(app.ProcessId);
            DesktopTestEnvironment.RequireOwnedPoint(app.ProcessId, point);
        }
        private void RequireUnobscuredWindow()
        {
            DesktopTestEnvironment.RequireOwnedForeground(app.ProcessId);
            var bounds = Window.BoundingRectangle;
            // A regular grid rejects covered captures; rendered evidence still requires visual inspection.
            for (var y = bounds.Top + 8; y < bounds.Bottom - 8; y += 32)
                for (var x = bounds.Left + 8; x < bounds.Right - 8; x += 32)
                    DesktopTestEnvironment.RequireOwnedPoint(app.ProcessId, new(x, y));
        }
        public static bool Wait(Func<bool> condition)
        {
            var timer = Stopwatch.StartNew();
            while (timer.Elapsed < TimeSpan.FromSeconds(15))
            {
                TestContext.Current.CancellationToken.ThrowIfCancellationRequested();
                try { if (condition()) return true; } catch (COMException) { }
                Thread.Sleep(100);
            }
            return false;
        }
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
    }
}
