using System.Text.Json.Nodes;
using FlaUI.Core.Definitions;
using FlaUI.Core.WindowsAPI;
using Xunit;

namespace AiUsage.Windows.Tests;

public sealed partial class AuditWindows
{
    private static readonly (string Provider, string Name)[] Providers =
        [("claude", "Claude"), ("codex", "Codex"), ("copilot", "GitHub Copilot"), ("antigravity", "Antigravity")];

    [Fact]
    public void AllFirstRunButtonsAndDuplicateProviderMenuEntriesReceiveMouseClicks()
    {
        DesktopTestEnvironment.RequireUnlockedDesktop();
        foreach (var (provider, name) in Providers)
        {
            var fixture = OverviewFixture();
            fixture["NextAccounts"] = fixture["Accounts"]!.DeepClone();
            fixture["Accounts"] = new JsonArray(); fixture["Observations"] = new JsonArray();
            fixture["ExpectedStates"] = new JsonObject(); fixture["Configuration"]!["Caps"] = new JsonArray();
            AddLoginLabels(fixture);
            using var session = Session.FromFixture(fixture, "first-run-" + provider);
            session.Click("Settings");
            session.Capture("first-run-" + provider + "-settings");
            session.Click("Sign in to " + name);
            Assert.True(Session.Wait(() => session.Receipts.Contains("Connect:" + provider + ":", StringComparison.Ordinal)));
            Assert.True(Session.Wait(() => session.Receipts.Contains("BrowserRequested:synthetic", StringComparison.Ordinal)));
            Assert.True(Session.Wait(() => session.Window.FindAllDescendants().Any(e =>
                (e.Properties.AutomationId.ValueOrDefault ?? "").StartsWith(LoginId(1) + ":", StringComparison.Ordinal))));
            Assert.Equal(1, session.Receipts.Split('\n').Count(l => l.StartsWith("Connect:", StringComparison.Ordinal)));
            Assert.DoesNotContain(session.Window.FindAllDescendants(), e => e.Properties.Name.ValueOrDefault == "Add an account");
            session.Click("Close settings");
            session.Capture("first-run-" + provider + "-succeeded");
            session.Exit();
        }
        var addedFixture = OverviewFixture();
        AddLoginLabels(addedFixture);
        using var added = Session.FromFixture(addedFixture, "duplicate-providers");
        var originalIds = addedFixture["Accounts"]!.AsArray().Select(a => a!["AccountId"]!.GetValue<string>().Replace("-", "", StringComparison.Ordinal)).ToArray();
        for (var index = 0; index < Providers.Length; index++)
        {
            var (provider, name) = Providers[index];
            added.Click("Add account");
            added.Capture("add-menu-" + provider);
            added.Click("Sign in to " + name);
            var id = LoginId(index + 1);
            Assert.True(Session.Wait(() => added.Window.FindAllDescendants().Any(e =>
                (e.Properties.AutomationId.ValueOrDefault ?? "").StartsWith(id + ":", StringComparison.Ordinal))));
            Assert.Equal(index + 1, added.Receipts.Split('\n').Count(l => l.StartsWith("Connect:", StringComparison.Ordinal)));
            Assert.Contains("Connect:" + provider + ":", added.Receipts, StringComparison.Ordinal);
            foreach (var original in originalIds)
                Assert.Contains(added.Window.FindAllDescendants(), e => (e.Properties.AutomationId.ValueOrDefault ?? "").StartsWith(original + ":", StringComparison.Ordinal));
            var newCard = added.Find(e => (e.Properties.AutomationId.ValueOrDefault ?? "").StartsWith(id + ":", StringComparison.Ordinal));
            added.Show(newCard);
            added.Capture("duplicate-" + provider + "-succeeded");
        }
        added.Exit();
    }

    private static JsonObject OverviewFixture() => JsonNode.Parse(File.ReadAllText(
        Path.Combine(Required("AIU_AUDIT_PAGE_DIRECTORY"), "overview.json")))!.AsObject();

    private static string LoginId(int number) => new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, (byte)(number + 100), 1).ToString("N");

    private static void AddLoginLabels(JsonObject fixture)
    {
        for (var number = 1; number <= 8; number++) fixture["Labels"]![LoginId(number)] = "SYNTHETIC added " + number;
    }
}
