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
public sealed partial class AuditWindows
{
    [Fact]
    public void ReplayPagesRenderUsedAndLeft()
    {
        DesktopTestEnvironment.RequireUnlockedDesktop();
        var pages = Required("AIU_AUDIT_PAGE_DIRECTORY");
        var firstPage = Environment.GetEnvironmentVariable("AIU_AUDIT_FIRST_PAGE");
        var files = Directory.EnumerateFiles(pages, "*.json").Where(p => !p.EndsWith(".expectations.json", StringComparison.Ordinal))
            .Where(p => string.IsNullOrEmpty(firstPage) || string.CompareOrdinal(Path.GetFileNameWithoutExtension(p), firstPage) >= 0).Order().ToArray();
        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            var pageId = Path.GetFileNameWithoutExtension(file);
            var input = JsonNode.Parse(File.ReadAllText(file))!.AsObject();
            var expectations = JsonNode.Parse(File.ReadAllText(Path.Combine(pages, pageId + ".expectations.json")))!.AsArray();
            using var session = new Session(file);
            var expected = input["ExpectedStates"]!.AsObject();
            if (expected.Any(p => p.Value!.GetValue<int>() == 18))
            {
                session.Click("Add account");
                session.Click("Show signed-out accounts");
                var signedOutId = expected.First(p => p.Value!.GetValue<int>() == 18).Key;
                Assert.True(Session.Wait(() => session.Window.FindAllDescendants().Any(e => e.Properties.AutomationId.ValueOrDefault == signedOutId)),
                    "Signed-out preference must visibly finish before dismissing the flyout");
                session.Capture(pageId + "-signed-out-toggle");
                session.RecordTree(pageId + "-signed-out-toggle");
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
                    var accountIndex = input["Accounts"]!.AsArray().ToList().FindIndex(a =>
                        pair.Key.StartsWith(a!["AccountId"]!.GetValue<string>().Replace("-", string.Empty, StringComparison.Ordinal) + ":", StringComparison.Ordinal));
                    Assert.True(accountIndex >= 0);
                    var cardExpected = expectations.FirstOrDefault(e => e?["CardId"]?.GetValue<string>() == pair.Key)
                        ?? expectations[accountIndex];
                    AssertDisplayedValue(session, pair.Key, cardExpected!.AsObject(), input["Accounts"]![accountIndex]!, mode);
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
        var moneyAccount = input["Accounts"]!.AsArray().Single(a => a!["Provider"]!.GetValue<string>() == "claude")!["AccountId"]!.GetValue<string>().Replace("-", string.Empty, StringComparison.Ordinal);
        var moneyId = input["ExpectedStates"]!.AsObject().Single(p => p.Key.StartsWith(moneyAccount + ":", StringComparison.Ordinal)).Key;
        var card = session.ById(moneyId);
        session.Focus(card);
        session.Click(card.FindAllDescendants().Single(e => (e.Properties.Name.ValueOrDefault ?? "").StartsWith("History,", StringComparison.Ordinal)));
        var history = session.Find(e => (e.Properties.Name.ValueOrDefault ?? "").Contains(" history,", StringComparison.Ordinal));
        history.Focus();
        var previous = history.Properties.Name.Value;
        session.Key(VirtualKeyShort.LEFT);
        Assert.True(Session.Wait(() => history.Properties.Name.Value != previous));
        session.Capture("controls-money-history");
        session.Key(VirtualKeyShort.ESCAPE);
        card = session.ById(moneyId); session.Focus(card);
        session.Key(VirtualKeyShort.F2);
        session.Type(session.Find(e => e.Properties.Name.ValueOrDefault == "Account name" && e.Properties.ControlType.ValueOrDefault == ControlType.Edit), "SYNTHETIC renamed account");
        session.Key(VirtualKeyShort.RETURN);
        Assert.True(Session.Wait(() => session.ById(moneyId).Properties.Name.Value.Contains("SYNTHETIC renamed account", StringComparison.Ordinal)));
        card = session.ById(moneyId); session.Focus(card); session.Key(VirtualKeyShort.KEY_C);
        var amount = session.Find(e => (e.Properties.Name.ValueOrDefault ?? "").StartsWith("Cap amount in", StringComparison.Ordinal));
        session.Type(amount, "€250.00"); session.Click("Save");
        Assert.NotNull(session.Find(e => (e.Properties.Name.ValueOrDefault ?? "").Contains("Enter a", StringComparison.Ordinal)));
        session.Capture("controls-invalid-cap");
        session.Type(amount, "250.00"); session.Click("Save");
        Assert.True(Session.Wait(() => session.ById(moneyId).Properties.Name.Value.Contains("cap", StringComparison.OrdinalIgnoreCase)));
        card = session.ById(moneyId); session.Focus(card); session.Key(VirtualKeyShort.KEY_C);
        session.Capture("controls-cap-editor"); session.Click("Remove");
        session.Key(VirtualKeyShort.F5);
        var accountId = moneyId.Split(':')[0];
        Assert.True(Session.Wait(() => session.Receipts.Contains("Refresh:" + accountId, StringComparison.Ordinal)));
        card = session.ById(moneyId); session.Focus(card);
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

    [Fact]
    public void MockAuthenticationSuccess() => MockAuthenticationCodeCancellationRetryAndOutcomes(-1);

    [Theory]
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
        fixture["NextAccounts"] = fixture["Accounts"]!.DeepClone();
        fixture["Accounts"] = new JsonArray(); fixture["Observations"] = new JsonArray(); fixture["ExpectedStates"] = new JsonObject();
        fixture["Configuration"]!["Caps"] = new JsonArray(); fixture["ManualCode"] = true;
        AddLoginLabels(fixture);
        fixture["SignInFailure"] = failure < 0 ? null : JsonValue.Create(failure);
        using var session = Session.FromFixture(fixture, "auth-" + failure.ToString(CultureInfo.InvariantCulture));
        session.Click("Sign in to Claude");
        Assert.True(Session.Wait(() => session.Receipts.Contains("BrowserRequested:synthetic", StringComparison.Ordinal)));
        session.Capture("auth-" + failure.ToString(CultureInfo.InvariantCulture) + "-waiting");
        session.Click("Cancel");
        Assert.True(Session.Wait(() => session.Receipts.Contains("CancelConnect", StringComparison.Ordinal)));
        session.Capture("auth-" + failure.ToString(CultureInfo.InvariantCulture) + "-cancelled");
        session.Click("Try again");
        session.Click("Cancel");
        Assert.True(Session.Wait(() => session.Receipts.Split('\n').Count(line => line.StartsWith("CancelConnect", StringComparison.Ordinal)) == 2));
        session.Click("Try again");
        session.Type(session.Find(e => e.Properties.Name.ValueOrDefault == "Sign-in code"), "invalid-synthetic-code");
        session.Click("Submit code");
        Assert.True(Session.Wait(() => session.Receipts.Contains("SubmitCode", StringComparison.Ordinal)));
        Assert.NotNull(session.Find(e => e.Properties.Name.ValueOrDefault == "Code was not accepted; check the current sign-in attempt"));
        session.Type(session.Find(e => e.Properties.Name.ValueOrDefault == "Sign-in code"), "synthetic-code");
        session.Click("Submit code");
        Assert.True(Session.Wait(() => session.Receipts.Contains("SubmitCode:Accepted", StringComparison.Ordinal)), "The fake provider must accept the exact current synthetic code");
        Assert.True(Session.Wait(() => !session.Window.FindAllDescendants().Any(e => e.Properties.Name.ValueOrDefault == "Sign-in code" && !e.IsOffscreen)));
        session.Capture("auth-" + failure.ToString(CultureInfo.InvariantCulture) + "-result");
        if (failure >= 0)
        {
            Assert.NotNull(session.Find(e => e.Properties.Name.ValueOrDefault == "Try again"));
            var reason = failure switch
            {
                0 => "This account is already connected", 1 => "Choose the account you are reconnecting",
                2 => "Local storage needs recovery before sign-in can continue", 3 => "Authorization was denied",
                4 => "The sign-in attempt expired", 5 => "The browser sign-in could not start",
                6 => "Provider registration is not configured on this PC", _ => "The provider could not complete sign-in"
            };
            Assert.NotNull(session.Find(e => e.Properties.Name.ValueOrDefault == reason));
            Assert.DoesNotContain(session.Window.FindAllDescendants(), e => (e.Properties.AutomationId.ValueOrDefault ?? "").StartsWith("00000000000000000000000000006501:", StringComparison.Ordinal));
        }
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

    private static void AssertDisplayedValue(Session session, string cardId, JsonObject expected, JsonNode account, string mode)
    {
        if (expected["Used"] is null || expected["State"]!.GetValue<string>() == "SignedOut") return;
        var used = expected["Used"]!.GetValue<decimal>();
        var layout = expected["Layout"]!.GetValue<string>();
        var suffix = cardId[(cardId.IndexOf(':') + 1)..];
        if (suffix == "status") return;
        var key = JsonNode.Parse(Convert.FromBase64String(suffix))!.AsArray();
        var fact = account["Session"]!["Quota"]!["Limits"]!["Limits"]!.AsArray().Single(f =>
            f!["Key"]!["Family"]!.GetValue<string>() == key[1]!.GetValue<string>() &&
            f["Key"]!["NativeDiscriminator"]!.GetValue<string>() == key[2]!.GetValue<string>())!;
        var money = fact["Used"]?["kind"]?.GetValue<string>() == "money";
        var percent = fact["UsedPercent"] is not null;
        var exponent = money ? fact["Used"]!["exponent"]!.GetValue<int>() : 0;
        var currency = money ? fact["Used"]!["currency"]!.GetValue<string>() : null;
        string Format(decimal amount)
        {
            var sign = amount < 0 ? "−" : string.Empty;
            var magnitude = Math.Abs(amount);
            if (percent) return sign + decimal.Round(magnitude, 0, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture) + " %";
            var number = magnitude.ToString("N" + exponent.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
            return sign + (currency switch { "USD" => "$" + number, "EUR" => "€" + number, "GBP" => "£" + number, null => number, _ => currency + " " + number });
        }
        string? footer = null;
        if (layout is "Period" or "FiveHourAndPeriod" or "UsedOnly")
            footer = Format(mode == "left" ? 100 - used : used) + (mode == "left" ? " left" : " used");
        else if (layout == "Pool")
        {
            if (expected["Cap"] is { } capValue)
            {
                var cap = capValue.GetValue<decimal>();
                footer = mode == "used" ? Format(used) + " of " + Format(cap) + " cap"
                    : used <= cap ? Format(cap - used) + " left to cap" : Format(used - cap) + " over cap";
            }
            else
            {
                var nativeLimit = fact["Limit"]!["value"]!;
                var limit = money ? nativeLimit["minor"]!.GetValue<decimal>() / DecimalPower(exponent) : nativeLimit["value"]!.GetValue<decimal>();
                footer = Format(mode == "left" ? limit - used : used) + " of " + Format(limit) + (mode == "left" ? " left" : " used");
            }
        }
        var state = expected["State"]!.GetValue<string>();
        var balance = account["Session"]!["Quota"]?["Credits"]?["Balance"];
        var literal = footer ?? (money ? used.ToString("F" + exponent.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture) + " " + currency
            : state == "NoCap" ? balance is null ? "no cap set" : "balance " + Format(balance.GetValue<decimal>())
            : state == "NotIncluded" ? "not included in plan" : Format(used));
        var visible = Session.Wait(() => session.ById(cardId).FindAllDescendants().Any(e =>
            e.Properties.ControlType.ValueOrDefault == ControlType.Text && !e.IsOffscreen &&
            (e.Properties.Name.ValueOrDefault ?? "").Contains(literal, StringComparison.Ordinal)));
        if (!visible)
        {
            session.Capture("failure-value-" + expected["Id"]!.GetValue<string>() + "-" + mode);
            session.RecordTree("failure-value-" + expected["Id"]!.GetValue<string>() + "-" + mode);
        }
        Assert.True(visible, "Expected independent visible value: " + literal);
        if (layout is "Period" or "Pool" && expected["TodayEnd"] is { } todayEnd &&
            state is not ("UsedUp" or "NotReady" or "SignedOut"))
        {
            // A first observation today establishes its own baseline; a real zero remains zero.
            var baseline = expected["Baseline"]?.GetValue<decimal>() ?? used;
            var end = todayEnd.GetValue<decimal>();
            var share = end - baseline;
            var today = used - baseline;
            var todayText = used <= end ? Format(mode == "left" ? end - used : today) + " of " + Format(share) + " " + mode
                : state == "DayOff" ? Format(today) + " used · a work day would allow " + Format(share)
                : mode == "left" ? "Nothing left today" : Format(today) + " used of " + Format(share) + " allowed";
            Assert.True(Session.Wait(() => session.ById(cardId).FindAllDescendants().Any(e =>
                e.Properties.ControlType.ValueOrDefault == ControlType.Group &&
                (e.Properties.Name.ValueOrDefault ?? "").Contains(todayText, StringComparison.Ordinal))),
                "Expected independent today tooltip: " + todayText);
        }
    }

    private static decimal DecimalPower(int exponent)
    {
        decimal result = 1;
        for (var i = 0; i < exponent; i++) result *= 10;
        return result;
    }

    private sealed partial class Session : IDisposable
    {
        private readonly Application app;
        private readonly Process process;
        private readonly IntPtr windowHandle;
        private readonly UIA3Automation automation = new();
        public string Input { get; }
        public string Root { get; }
        public string Evidence { get; }
        private Window? initialWindow;
        public Window Window
        {
            get => windowHandle == IntPtr.Zero ? initialWindow! : automation.FromHandle(windowHandle).AsWindow();
            private set => initialWindow = value;
        }
        public string Receipts => File.Exists(Path.Combine(Root, "requests.txt")) ? File.ReadAllText(Path.Combine(Root, "requests.txt")) : string.Empty;

        public int ProcessId => process.Id;
        public Session(string input, string? evidenceDirectory = null, string? stateDirectory = null, int? attachProcessId = null)
        {
            DesktopTestEnvironment.RequireUnlockedDesktop();
            Input = input;
            Evidence = evidenceDirectory ?? Required("AIU_SMOKE_EVIDENCE_DIRECTORY");
            Root = stateDirectory ?? Path.Combine(Path.GetTempPath(), "aiu-ui-audit-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Evidence);
            var start = new ProcessStartInfo(Required("AIU_SMOKE_EXE")) { UseShellExecute = false };
            start.ArgumentList.Add("--demo"); start.ArgumentList.Add("--audit-input=" + input);
            start.Environment["AIU_DEVELOPMENT_STATE_DIRECTORY"] = Root;
            Application? launched = null;
            Process? launchedProcess = null;
            try
            {
                if (attachProcessId is { } existingId)
                {
                    Assert.Equal("WDAGUtilityAccount", Environment.UserName);
                    Assert.Equal("AI Usage synthetic audit v1", File.ReadAllText(Path.Combine(Root, "synthetic-audit.marker")));
                    using var candidate = Process.GetProcessById(existingId);
                    Assert.Equal(Path.GetFullPath(Required("AIU_SMOKE_EXE")), candidate.MainModule!.FileName, ignoreCase: true);
                    Assert.True(Wait(() => HasProcessContext(candidate, Root, Input)), "BLOCKED: attachment requires the process-specific synthetic fixture receipt");
                }
                app = launched = attachProcessId is { } id ? Application.Attach(id) : Application.Launch(start);
                process = launchedProcess = Process.GetProcessById(app.ProcessId);
                _ = process.Handle;
                Window? window = null;
                var started = Wait(() => (window = OwnedSurfaces().FirstOrDefault(w =>
                    w.FindFirstDescendant(cf => cf.ByName("Settings").And(cf.ByControlType(ControlType.Button))) is not null)?.AsWindow()) is not null);
                if (!started) RecordStartupFailure();
                Assert.True(started, "The isolated audit window did not start; see startup-failure.json");
                Window = window!;
                windowHandle = GetAncestor(Window.Properties.NativeWindowHandle.Value, 2);
                Window.SetForeground();
                if (!OwnsForeground())
                {
                    // Windows may deny programmatic activation of a later test process.
                    // An initial single caption click is safe only after native hit testing;
                    // every subsequent keyboard/content action requires owned foreground.
                    var bounds = Window.BoundingRectangle;
                    var caption = new System.Drawing.Point(bounds.Left + 50, bounds.Top + 18);
                    DesktopTestEnvironment.RequireOwnedPoint(app.ProcessId, caption);
                    Mouse.MoveTo(caption);
                    DesktopTestEnvironment.RequireOwnedPoint(app.ProcessId, caption);
                    Mouse.Click();
                }
                Assert.True(Wait(OwnsForeground), "BLOCKED: test process must own foreground input");
                Assert.True(Wait(() => Window.FindAllDescendants().Any(e => e.Properties.Name.ValueOrDefault is "Add an account" or "Opening local data…" or "Recovery and diagnostics" || (e.Properties.Name.ValueOrDefault ?? "").Contains("SYNTHETIC", StringComparison.Ordinal))));
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
        public static Session FromFixture(JsonObject fixture, string id, string? stateDirectory = null)
        {
            var path = Path.Combine(Required("AIU_AUDIT_PAGE_DIRECTORY"), id + ".fixture");
            File.WriteAllText(path, fixture.ToJsonString());
            return new(path, stateDirectory: stateDirectory);
        }
        public AutomationElement Find(Func<AutomationElement, bool> predicate)
        {
            AutomationElement? found = null;
            Assert.True(Wait(() => (found = OwnedSurfaces().SelectMany(w => w.FindAllDescendants()).FirstOrDefault(predicate)) is not null), "Required audit control was not found");
            return found!;
        }
        private IEnumerable<AutomationElement> OwnedSurfaces() => automation.GetDesktop().FindAllChildren().Where(w =>
            w.Properties.NativeWindowHandle.ValueOrDefault is var handle && handle != IntPtr.Zero &&
            GetWindowThreadProcessId(handle, out var owner) != 0 && owner == app.ProcessId);
        private void RecordStartupFailure()
        {
            // Preserve only this explicitly selected, marked synthetic root's diagnostics.
            if (File.Exists(Path.Combine(Root, "synthetic-audit.marker")) &&
                File.ReadAllText(Path.Combine(Root, "synthetic-audit.marker")) == "AI Usage synthetic audit v1")
            {
                var logs = Path.Combine(Root, "logs");
                if (Directory.Exists(logs))
                    foreach (var file in Directory.EnumerateFiles(logs, "application-*.jsonl"))
                        File.Copy(file, Path.Combine(Evidence, "startup-" + Path.GetFileName(file)), overwrite: true);
                File.WriteAllText(Path.Combine(Evidence, "startup-requests.txt"), Receipts);
            }
            var candidates = automation.GetDesktop().FindAllChildren().Select(w =>
            {
                var handle = w.Properties.NativeWindowHandle.ValueOrDefault;
                _ = GetWindowThreadProcessId(handle, out var owner);
                return (Window: w, NativeOwner: owner);
            }).Where(w => w.NativeOwner == app.ProcessId || w.Window.Properties.ProcessId.ValueOrDefault == app.ProcessId);
            File.WriteAllText(Path.Combine(Evidence, "startup-failure.json"), JsonSerializer.Serialize(new
            {
                Synthetic = true, ProcessExited = process.HasExited, ExitCode = process.HasExited ? (int?)process.ExitCode : null,
                Windows = candidates.Select(w => new
                {
                    Name = w.Window.Properties.Name.ValueOrDefault, NativeOwner = w.NativeOwner,
                    AutomationOwner = w.Window.Properties.ProcessId.ValueOrDefault,
                    Children = w.Window.FindAllDescendants().Take(80).Select(e => new
                    {
                        Name = e.Properties.Name.ValueOrDefault, Type = e.Properties.ControlType.ValueOrDefault.ToString(),
                        Id = e.Properties.AutomationId.ValueOrDefault
                    }).ToArray()
                }).ToArray()
            }));
        }
        public AutomationElement ById(string id) => Find(e => e.Properties.AutomationId.ValueOrDefault == id);
        public void Click(string name) => Click(Find(e => e.Properties.Name.ValueOrDefault == name &&
            e.Properties.ControlType.ValueOrDefault is ControlType.Button or ControlType.CheckBox));
        public void Click(AutomationElement element)
        {
            DesktopTestEnvironment.RequireOwnedForeground(app.ProcessId);
            Show(element);
            Assert.True(element.IsEnabled, element.Properties.Name.ValueOrDefault);
            Assert.False(element.Properties.IsOffscreen.ValueOrDefault);
            var point = element.GetClickablePoint();
            RequireMouseTarget(point);
            Mouse.MoveTo(point);
            RequireMouseTarget(point);
            Mouse.Click();
            FlaUI.Core.Input.Wait.UntilInputIsProcessed();
        }
        public void Show(AutomationElement element)
        {
            try { ShowCore(element); }
            catch (COMException error) when (error.HResult == unchecked((int)0x80040201))
            {
                // WinUI can replace an ancestor's UIA provider during layout. Retry once;
                // native ownership and point checks still precede all physical input.
                Thread.Sleep(150);
                ShowCore(element);
            }
        }
        private void ShowCore(AutomationElement element)
        {
            DesktopTestEnvironment.RequireUnlockedDesktop();
            RequireOwnedElement(element);
            element.Patterns.ScrollItem.PatternOrDefault?.ScrollIntoView();
            FlaUI.Core.Input.Wait.UntilInputIsProcessed();
            if (element.Properties.IsOffscreen.ValueOrDefault) element.Focus();
            var viewport = Window.BoundingRectangle;
            for (AutomationElement? parent = element; parent is not null; parent = parent.Parent)
            {
                var handle = parent.Properties.NativeWindowHandle.ValueOrDefault;
                if (handle == IntPtr.Zero) continue;
                Assert.True(GetWindowThreadProcessId(handle, out var owner) != 0 && owner == app.ProcessId);
                viewport = parent.BoundingRectangle;
                break;
            }
            for (var parent = element.Parent; parent is not null; parent = parent.Parent)
                if (parent.Patterns.Scroll.PatternOrDefault?.VerticallyScrollable.ValueOrDefault == true)
                { viewport = System.Drawing.Rectangle.Intersect(viewport, parent.BoundingRectangle); break; }
            for (var attempt = 0; attempt < 15; attempt++)
            {
                var target = element.BoundingRectangle;
                if (target.Top >= viewport.Top + 2 && target.Bottom <= viewport.Bottom - 2 && !element.Properties.IsOffscreen.ValueOrDefault) break;
                // Some WinUI buttons expose neither ScrollItem nor a reliable IsOffscreen value.
                // Wheel inside their own column, never at an off-window target point.
                var wheelPoint = new System.Drawing.Point(Math.Clamp(target.Left + target.Width / 2, viewport.Left + 20, viewport.Right - 20),
                    viewport.Top + viewport.Height / 2);
                RequireMouseTarget(wheelPoint); Mouse.MoveTo(wheelPoint); RequireMouseTarget(wheelPoint);
                Mouse.Scroll(target.Top < viewport.Top + 2 ? 2 : -2);
                FlaUI.Core.Input.Wait.UntilInputIsProcessed();
                Thread.Sleep(100);
            }
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
            FlaUI.Core.Input.Wait.UntilInputIsProcessed();
        }
        public void Chord(params VirtualKeyShort[] keys)
        {
            DesktopTestEnvironment.RequireOwnedForeground(app.ProcessId);
            Keyboard.TypeSimultaneously(keys);
            FlaUI.Core.Input.Wait.UntilInputIsProcessed();
        }
        public void Focus(AutomationElement element)
        {
            Show(element); element.Focus();
            Assert.True(Wait(() => element.Properties.HasKeyboardFocus.ValueOrDefault));
        }
        public void Type(AutomationElement element, string value)
        {
            Click(element);
            FlaUI.Core.Input.Wait.UntilInputIsProcessed();
            Assert.True(Wait(() => element.Properties.HasKeyboardFocus.ValueOrDefault), "Mouse click must focus the exact input field");
            DesktopTestEnvironment.RequireOwnedForeground(app.ProcessId);
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
            FlaUI.Core.Input.Wait.UntilInputIsProcessed();
            if (value.Length == 0) Key(VirtualKeyShort.BACK);
            foreach (var character in value)
            {
                DesktopTestEnvironment.RequireOwnedForeground(app.ProcessId);
                Keyboard.Type(character);
            }
            FlaUI.Core.Input.Wait.UntilInputIsProcessed();
            Assert.True(Wait(() => element.AsTextBox().Text == value), "Physical typing must produce the exact synthetic field value");
        }
        public void Capture(string name, bool keepPointer = false)
        {
            RequireUnobscuredWindow();
            if (!keepPointer)
            {
                var bounds = Window.BoundingRectangle;
                var caption = new System.Drawing.Point(bounds.Left + 70, bounds.Top + 18);
                RequireMouseTarget(caption); Mouse.MoveTo(caption); RequireMouseTarget(caption);
            }
            Thread.Sleep(400); // Capture the settled layout, after the ordinary 250 ms motion and tooltip dismissal.
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    Window = automation.FromHandle(windowHandle).AsWindow();
                    RequireUnobscuredWindow();
                    using var capture = Window.Capture();
                    RequireUnobscuredWindow();
                    capture.Save(Path.Combine(Evidence, name + ".png"), System.Drawing.Imaging.ImageFormat.Png);
                    return;
                }
                catch (COMException) when (attempt == 0 && !process.HasExited)
                {
                    // WinUI can replace a UIA provider after layout; retry once with a fresh
                    // binding to the observed native window, without relaxing input guards.
                    Thread.Sleep(150);
                }
            }
        }
        public void RecordTree(string name) => File.WriteAllText(Path.Combine(Evidence, name + ".json"), JsonSerializer.Serialize(
            OwnedSurfaces().SelectMany(w => w.FindAllDescendants()).Select(e => new
            {
                Name = e.Properties.Name.ValueOrDefault, Type = e.Properties.ControlType.ValueOrDefault.ToString(),
                Id = e.Properties.AutomationId.ValueOrDefault, Visible = !e.Properties.IsOffscreen.ValueOrDefault,
                Enabled = e.Properties.IsEnabled.ValueOrDefault,
                Focused = e.Properties.HasKeyboardFocus.ValueOrDefault,
                Focusable = e.Properties.IsKeyboardFocusable.ValueOrDefault
            }).ToArray()));
        public void Exit()
        {
            DesktopTestEnvironment.RequireUnlockedDesktop();
            Window.SetForeground();
            Assert.True(Wait(OwnsForeground), "BLOCKED: test process must own exit input");
            DesktopTestEnvironment.RequireOwnedForeground(app.ProcessId);
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_Q);
            Assert.True(process.WaitForExit(10000), "The audit process must drain and exit");
            Assert.Equal(0, process.ExitCode);
            Assert.Contains("Stopped", Receipts, StringComparison.Ordinal);
        }
        public void Dispose()
        {
            try
            {
                if (!process.HasExited)
                {
                    try
                    {
                        Capture("failure-" + Path.GetFileNameWithoutExtension(Input));
                        RecordTree("failure-" + Path.GetFileNameWithoutExtension(Input));
                        File.WriteAllText(Path.Combine(Evidence, "failure-" + Path.GetFileNameWithoutExtension(Input) + ".requests.txt"), Receipts);
                    }
                    catch (Exception failure)
                    {
                        try { File.WriteAllText(Path.Combine(Evidence, "failure-capture.json"), JsonSerializer.Serialize(new { Type = failure.GetType().Name })); }
                        catch (IOException) { }
                        catch (UnauthorizedAccessException) { }
                    }
                }
            }
            finally
            {
                try { if (!process.HasExited) { process.Kill(); process.WaitForExit(5000); } }
                finally { process.Dispose(); app.Dispose(); automation.Dispose(); }
            }
        }
        private void RequireMouseTarget(System.Drawing.Point point)
        {
            DesktopTestEnvironment.RequireOwnedForeground(app.ProcessId);
            DesktopTestEnvironment.RequireOwnedPoint(app.ProcessId, point);
        }
        private bool OwnsForeground() => GetWindowThreadProcessId(GetForegroundWindow(), out var owner) != 0 && owner == app.ProcessId;
        private void RequireOwnedElement(AutomationElement element)
        {
            // WinUI's guest UIA provider can report ProcessId=0. Establish ownership through
            // the nearest native ancestor before invoking any UIA focus/scroll operation.
            for (AutomationElement? ancestor = element; ancestor is not null; ancestor = ancestor.Parent)
            {
                var handle = ancestor.Properties.NativeWindowHandle.ValueOrDefault;
                if (handle == IntPtr.Zero) continue;
                Assert.True(GetWindowThreadProcessId(handle, out var owner) != 0 && owner == app.ProcessId,
                    "BLOCKED: the isolated app must own the target's native ancestor");
                return;
            }
            Assert.Fail("BLOCKED: target ownership cannot be established");
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
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr handle, out uint owner);
    }
}
