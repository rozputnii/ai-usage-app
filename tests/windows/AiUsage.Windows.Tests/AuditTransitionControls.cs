using System.Globalization;
using System.Text.Json.Nodes;
using FlaUI.Core.Definitions;
using FlaUI.Core.WindowsAPI;
using Xunit;

namespace AiUsage.Windows.Tests;

public sealed partial class AuditWindows
{
    [Theory]
    [InlineData("TRANS-01")]
    [InlineData("TRANS-02")]
    public void RefreshMovesMoneyBetweenMainAndNestedContentAndUpdatesOpenHistory(string scenario)
    {
        DesktopTestEnvironment.RequireUnlockedDesktop();
        var path = Path.Combine(Required("AIU_AUDIT_PAGE_DIRECTORY"), "transitions", scenario + ".json");
        var next = JsonNode.Parse(File.ReadAllText(Path.ChangeExtension(path, "next.expectations.json")))!.AsArray();
        var fixture = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        var moneyId = next[0]!["MoneyCardId"]!.GetValue<string>();
        using var session = new Session(path);
        var initialIds = fixture["ExpectedStates"]!.AsObject().Select(p => p.Key).ToArray();
        var card = session.ById(moneyId); session.Show(card);
        session.Click(card.FindAllDescendants().Single(e => e.Properties.ControlType.ValueOrDefault == ControlType.Button &&
            (e.Properties.Name.ValueOrDefault ?? "").StartsWith("History,", StringComparison.Ordinal)));
        session.Find(e => e.Properties.IsKeyboardFocusable.ValueOrDefault && (e.Properties.Name.ValueOrDefault ?? "").Contains(" history,", StringComparison.Ordinal));
        session.Click("Settings");
        session.Key(VirtualKeyShort.F5);
        var accountId = moneyId.Split(':')[0];
        Assert.True(Session.Wait(() => session.Receipts.Split('\n').Count(line => line.Trim() == "Refresh:" + accountId) == 1));
        foreach (var item in next)
        {
            var id = item!["CardId"]!.GetValue<string>();
            var state = Enum.Parse<AuditCardState>(item["State"]!.GetValue<string>());
            Assert.True(Session.Wait(() => session.ById(id).FindAllDescendants().Any(e =>
                (e.Properties.Name.ValueOrDefault ?? "").Contains(StateWords((int)state), StringComparison.Ordinal))), id);
            if (item["TodayEnd"] is { } end && item["Layout"]!.GetValue<string>() == "Pool")
            {
                var share = (end.GetValue<decimal>() - 120).ToString("0.00", CultureInfo.InvariantCulture);
                Assert.Contains(session.ById(id).FindAllDescendants(), e =>
                    (e.Properties.Name.ValueOrDefault ?? "").Contains("$10.00 used of $" + share + " allowed", StringComparison.Ordinal));
            }
        }
        foreach (var gone in initialIds.Except(next.Select(item => item!["CardId"]!.GetValue<string>())))
            Assert.DoesNotContain(session.Window.FindAllDescendants(), e => e.Properties.AutomationId.ValueOrDefault == gone);
        Assert.True(Session.Wait(() => session.Window.FindAllDescendants().Any(e =>
            e.Properties.IsKeyboardFocusable.ValueOrDefault && (e.Properties.Name.ValueOrDefault ?? "").Contains("Wed 7 Oct, 10 dollars", StringComparison.Ordinal))),
            "Open history must refresh to the independently expected $10 daily delta");
        Assert.True(Visible(session, "Close settings"));
        Assert.Contains(session.Window.FindAllDescendants(), e => e.Properties.HasKeyboardFocus.ValueOrDefault &&
            !(e.Properties.Name.ValueOrDefault ?? "").Contains(" history,", StringComparison.Ordinal));
        session.Capture(scenario + "-settings-history-after-refresh");
        session.Click("Close settings");
        session.Show(session.ById(moneyId)); session.Capture(scenario + "-money-after-refresh");
        session.RecordTree(scenario + "-after-refresh"); session.Exit();
    }

    // Values match the production contract enum; expectations are exported as names, not recomputed states.
    private enum AuditCardState { OnTrack, TodayLow, TodayShort, CapClose, FiveHourFull, TodayUsed, OverToday,
        CapReached, OverCap, UsedUp, DayOff, Rush, NotReady, ValueUnknown, PeriodUnknown, NotIncluded, LimitUnknown,
        NoCap, SignedOut, NoDisplayedLimits }
}
