using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.WindowsAPI;
using Xunit;

namespace AiUsage.Windows.Tests;

public sealed partial class AuditWindows
{
    [Fact(Explicit = true)]
    public void SettingsFormsUndoAndPreferencesSurviveAnIsolatedRestart()
    {
        DesktopTestEnvironment.RequireUnlockedDesktop();
        var input = Path.Combine(Required("AIU_AUDIT_PAGE_DIRECTORY"), "overview.json");
        var fixture = OverviewFixture();
        var moneyId = fixture["ExpectedStates"]!.AsObject().Single(p => p.Key.StartsWith(
            fixture["Accounts"]![0]!["AccountId"]!.GetValue<string>().Replace("-", "", StringComparison.Ordinal), StringComparison.Ordinal)).Key;
        string root;
        using (var session = new Session(input))
        {
            root = session.Root;
            var original = session.ById(moneyId).Properties.Name.Value;
            session.Click("Add account");
            Assert.NotNull(session.Find(e => e.Properties.Name.ValueOrDefault == "Sign in to Claude"));
            session.Key(VirtualKeyShort.ESCAPE);
            Assert.True(Session.Wait(() => !Visible(session, "Sign in to Claude")));
            Assert.Equal(original, session.ById(moneyId).Properties.Name.Value);
            Assert.DoesNotContain("Connect:", session.Receipts, StringComparison.Ordinal);
            session.Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_N);
            Assert.True(Session.Wait(() => Visible(session, "Sign in to Codex")));
            session.Key(VirtualKeyShort.ESCAPE);
            session.Chord(VirtualKeyShort.CONTROL, (VirtualKeyShort)188);
            Assert.True(Session.Wait(() => Visible(session, "Close settings")));
            // Used/Left lives only in the title bar (D-193); the settings sheet stays open beside it.
            var modes = session.Window.FindAllDescendants().Where(e => e.Properties.Name.ValueOrDefault == "Show values: left" &&
                e.Properties.ControlType.ValueOrDefault == ControlType.Button).ToArray();
            Assert.Single(modes);
            session.Menu("Preview diagnostics");
            session.Click(modes[0]);
            RequirePreference(session, p => p["Preferences"]!["Mode"]!.GetValue<int>() == 1);
            Assert.Contains("left", session.ById(moneyId).Properties.Name.Value, StringComparison.Ordinal);
            session.Click("Show values: used");
            RequirePreference(session, p => p["Preferences"]!["Mode"]!.GetValue<int>() == 0);
            session.Click(modes[0]);
            RequirePreference(session, p => p["Preferences"]!["Mode"]!.GetValue<int>() == 1);
            session.Click("Density: comfortable");
            RequirePreference(session, p => p["Preferences"]!["Density"]!.GetValue<int>() == 1);
            session.Click("Always on top, off");
            RequirePreference(session, p => p["Preferences"]!["AlwaysOnTop"]!.GetValue<bool>());
            Assert.True(Session.Wait(() => (GetWindowLongPtr(session.Window.Properties.NativeWindowHandle.Value, -20).ToInt64() & 8) != 0));
            session.Click("Add account"); session.Click("Show signed-out accounts"); session.Key(VirtualKeyShort.ESCAPE);
            RequirePreference(session, p => p["Preferences"]!["ShowSignedOut"]!.GetValue<bool>());
            foreach (var day in new[] { "Tuesday", "Wednesday", "Thursday", "Friday" })
            {
                session.Click(day + ", work day");
                Assert.True(Session.Wait(() => Visible(session, day + ", day off")));
            }
            var monday = session.Find(e => e.Properties.Name.ValueOrDefault == "Monday, work day");
            session.Show(monday); Assert.False(monday.IsEnabled);
            session.Click("Friday, day off");
            Assert.True(Session.Wait(() => monday.IsEnabled));
            session.Click(session.Find(e => (e.Properties.Name.ValueOrDefault ?? "").StartsWith("Undo:", StringComparison.Ordinal)));
            Assert.True(Session.Wait(() => !monday.IsEnabled));
            session.Click("Friday, day off");
            session.Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_Z);
            Assert.True(Session.Wait(() => !monday.IsEnabled));
            session.Capture("preferences-one-workday");
            session.Click("Close settings");
            var beforeRename = session.ById(moneyId).Properties.Name.Value;
            var card = session.ById(moneyId); session.FocusCard(card); session.Key(VirtualKeyShort.F2);
            session.Type(session.Find(e => e.Properties.Name.ValueOrDefault == "Account name"), "SYNTHETIC cancelled rename");
            session.Key(VirtualKeyShort.ESCAPE);
            Assert.Equal(beforeRename, session.ById(moneyId).Properties.Name.Value);
            session.FocusCard(session.ById(moneyId)); session.Key(VirtualKeyShort.F2);
            session.Type(session.Find(e => e.Properties.Name.ValueOrDefault == "Account name"), ""); session.Key(VirtualKeyShort.RETURN);
            Assert.Contains("Claude · SYNTHETIC", session.ById(moneyId).Properties.Name.Value, StringComparison.Ordinal);
            session.FocusCard(session.ById(moneyId)); session.Key(VirtualKeyShort.F2);
            session.Type(session.Find(e => e.Properties.Name.ValueOrDefault == "Account name"), "  SYNTHETIC persistent  "); session.Key(VirtualKeyShort.RETURN);
            Assert.True(Session.Wait(() => session.ById(moneyId).Properties.Name.Value.StartsWith("SYNTHETIC persistent", StringComparison.Ordinal)));
            card = session.ById(moneyId);
            session.Click(card.FindAllDescendants().Single(e => e.Properties.ControlType.ValueOrDefault == ControlType.Button &&
                (e.Properties.Name.ValueOrDefault ?? "").StartsWith("Limit settings,", StringComparison.Ordinal)));
            session.Type(CapInput(session), "240.00"); session.Key(VirtualKeyShort.ESCAPE);
            Assert.Empty(JsonNode.Parse(File.ReadAllText(Path.Combine(root, "budget", "configuration.v1.json")))!["Caps"]!.AsArray());
            session.FocusCard(session.ById(moneyId)); session.Key(VirtualKeyShort.KEY_C);
            var amount = CapInput(session); session.Type(amount, "240.00");
            session.Key(VirtualKeyShort.TAB);
            Assert.True(Session.Wait(() => session.Window.FindAllDescendants().Any(e => e.Properties.Name.ValueOrDefault == "Save" && e.Properties.HasKeyboardFocus.ValueOrDefault)));
            session.Chord(VirtualKeyShort.SHIFT, VirtualKeyShort.TAB);
            Assert.True(Session.Wait(() => amount.Properties.HasKeyboardFocus.ValueOrDefault));
            session.Key(VirtualKeyShort.RETURN);
            Assert.True(Session.Wait(() => session.ById(moneyId).Properties.Name.Value.Contains("cap", StringComparison.OrdinalIgnoreCase)));
            session.FocusCard(session.ById(moneyId)); session.Key(VirtualKeyShort.RETURN);
            Assert.NotNull(session.Find(e => (e.Properties.Name.ValueOrDefault ?? "").Contains(" history,", StringComparison.Ordinal)));
            card = session.ById(moneyId); session.Show(card);
            session.Click(card.FindAllDescendants().Single(e => e.Properties.ControlType.ValueOrDefault == ControlType.Button &&
                (e.Properties.Name.ValueOrDefault ?? "").StartsWith("History,", StringComparison.Ordinal)));
            Assert.True(Session.Wait(() => !session.Window.FindAllDescendants().Any(e => (e.Properties.Name.ValueOrDefault ?? "").Contains(" history,", StringComparison.Ordinal))));
            session.FocusCard(card); session.Key(VirtualKeyShort.RIGHT);
            var detail = session.Window.FindAllDescendants().Single(e => e.Properties.IsKeyboardFocusable.ValueOrDefault &&
                e.Properties.HasKeyboardFocus.ValueOrDefault);
            session.Key(VirtualKeyShort.RIGHT);
            Assert.True(Session.Wait(() => !detail.Properties.HasKeyboardFocus.ValueOrDefault));
            session.Key(VirtualKeyShort.LEFT);
            Assert.True(Session.Wait(() => detail.Properties.HasKeyboardFocus.ValueOrDefault));
            session.Capture("preferences-before-restart"); session.Exit();
        }
        using var restarted = new Session(input, stateDirectory: root);
        Assert.Contains("SYNTHETIC persistent", restarted.ById(moneyId).Properties.Name.Value, StringComparison.Ordinal);
        Assert.Contains("left", restarted.ById(moneyId).Properties.Name.Value, StringComparison.Ordinal);
        Assert.Contains("cap", restarted.ById(moneyId).Properties.Name.Value, StringComparison.OrdinalIgnoreCase);
        restarted.Click("Settings");
        Assert.True(Session.Wait(() => Visible(restarted, "Close settings")));
        Assert.NotNull(restarted.Find(e => e.Properties.Name.ValueOrDefault == "Always on top, on"));
        Assert.False(restarted.Find(e => e.Properties.Name.ValueOrDefault == "Monday, work day").IsEnabled);
        Assert.True(Session.Wait(() => (GetWindowLongPtr(restarted.Window.Properties.NativeWindowHandle.Value, -20).ToInt64() & 8) != 0));
        RequirePreference(restarted, p => p["Preferences"]!["Mode"]!.GetValue<int>() == 1 && p["Preferences"]!["Density"]!.GetValue<int>() == 1 &&
            p["Preferences"]!["ShowSignedOut"]!.GetValue<bool>());
        restarted.Capture("preferences-after-restart"); restarted.Exit();
    }

    private static AutomationElement CapInput(Session session) => session.Find(e =>
        (e.Properties.Name.ValueOrDefault ?? "").StartsWith("Cap amount in", StringComparison.Ordinal));
    private static bool Visible(Session session, string name) => session.Window.FindAllDescendants().Any(e =>
        e.Properties.Name.ValueOrDefault == name && !e.Properties.IsOffscreen.ValueOrDefault &&
        e.BoundingRectangle.Width > 0 && e.BoundingRectangle.Height > 0);
    private static void RequirePreference(Session session, Func<JsonNode, bool> check) => Assert.True(Session.Wait(() =>
    {
        var file = Path.Combine(session.Root, "preferences", "ledger", "appearance.v1.json");
        try { return File.Exists(file) && check(JsonNode.Parse(File.ReadAllText(file))!); }
        catch (IOException) { return false; }
    }));
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr handle, int index);
}
