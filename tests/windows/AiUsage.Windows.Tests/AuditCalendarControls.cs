using System.Globalization;
using System.Text.Json.Nodes;
using FlaUI.Core.Definitions;
using FlaUI.Core.WindowsAPI;
using Xunit;

namespace AiUsage.Windows.Tests;

public sealed partial class AuditWindows
{
    [Theory(Explicit = true)]
    [InlineData("CAL-01")]
    [InlineData("CAL-02")]
    [InlineData("CAL-04")]
    public void CalendarRolloverKeepsFirstDayHistoryAndExpiresWorkToday(string scenario)
    {
        DesktopTestEnvironment.RequireUnlockedDesktop();
        var path = Path.Combine(Required("AIU_AUDIT_PAGE_DIRECTORY"), "transitions", scenario + ".json");
        var expected = JsonNode.Parse(File.ReadAllText(Path.ChangeExtension(path, "next.expectations.json")))!;
        var id = expected["CardId"]!.GetValue<string>();
        using var session = new Session(path);
        Assert.Contains("day off", session.ById(id).Properties.Name.Value, StringComparison.OrdinalIgnoreCase);
        var configuration = Path.Combine(session.Root, "budget", "configuration.v1.json");
        var workdays = JsonNode.Parse(File.ReadAllText(configuration))!["WorkDays"]!.ToJsonString();
        if (scenario == "CAL-04")
        {
            session.Click("Work today, off");
            Assert.True(Session.Wait(() => Visible(session, "Work today, on until midnight")));
            Assert.Contains("extra day", session.ById(id).Properties.Name.Value, StringComparison.OrdinalIgnoreCase);
            session.Capture(scenario + "-work-today-on");
            session.Click("Work today, on until midnight");
            Assert.True(Session.Wait(() => Visible(session, "Work today, off")));
            Assert.Contains("day off", session.ById(id).Properties.Name.Value, StringComparison.OrdinalIgnoreCase);
            session.Click("Work today, off");
            Assert.True(Session.Wait(() => Visible(session, "Work today, on until midnight")));
        }
        session.FocusCard(session.ById(id));
        session.Click(session.ById(id).FindAllDescendants().Single(e => e.Properties.ControlType.ValueOrDefault == ControlType.Button &&
            (e.Properties.Name.ValueOrDefault ?? "").StartsWith("History,", StringComparison.Ordinal)));
        session.Find(e => e.Properties.IsKeyboardFocusable.ValueOrDefault &&
            (e.Properties.Name.ValueOrDefault ?? "").Contains(" history,", StringComparison.Ordinal));
        session.Capture(scenario + "-before-midnight");
        session.Key(VirtualKeyShort.F5);
        Assert.True(Session.Wait(() => session.Receipts.Split('\n').Count(line => line.Trim() == "Refresh:" + id.Split(':')[0]) == 1));
        var day = scenario == "CAL-04" ? "Sun 11 Oct, 0 dollars" : "Sun 1 Nov, 10 dollars";
        Assert.True(Session.Wait(() => scenario == "CAL-04" ? Visible(session, "Work today, off") :
            session.ById(id).FindAllDescendants().Any(e => (e.Properties.Name.ValueOrDefault ?? "").Contains("$10.00 of $2,000.00 used", StringComparison.Ordinal))));
        var history = session.Find(e => e.Properties.IsKeyboardFocusable.ValueOrDefault &&
            (e.Properties.Name.ValueOrDefault ?? "").Contains(" history,", StringComparison.Ordinal));
        session.Click(history); // Physical hit testing of the refreshed history surface.
        session.Key(VirtualKeyShort.RIGHT); // The previous focused calendar day is preserved across midnight.
        Assert.True(Session.Wait(() => (history.Properties.Name.ValueOrDefault ?? "").Contains(day, StringComparison.Ordinal)));
        var card = session.ById(id);
        Assert.Contains("day off", card.Properties.Name.Value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("extra day", card.Properties.Name.Value, StringComparison.OrdinalIgnoreCase);
        Assert.True(Visible(session, "Work today, off"));
        Assert.Equal(workdays, JsonNode.Parse(File.ReadAllText(configuration))!["WorkDays"]!.ToJsonString());
        var today = scenario == "CAL-04" ? "$0.00 of $117.15 used" : "$10.00 of $90.90 used";
        Assert.Contains(card.FindAllDescendants(), e => (e.Properties.Name.ValueOrDefault ?? "").Contains(today, StringComparison.Ordinal));
        if (scenario != "CAL-04")
            Assert.Contains(card.FindAllDescendants(), e => (e.Properties.Name.ValueOrDefault ?? "").Contains("1 Dec (assumed)", StringComparison.Ordinal));
        session.Show(history); session.Capture(scenario + "-after-midnight-history");
        session.Click("Show values: left");
        session.Show(session.ById(id)); session.Capture(scenario + "-after-midnight-left");
        var now = expected["Now"]!.GetValue<string>();
        Assert.Equal(scenario == "CAL-01" ? "2026-11-01T00:05:00+00:00" : scenario == "CAL-02" ? "2026-11-01T00:00:00+00:00" :
            "2026-10-11T00:00:00+00:00", DateTimeOffset.Parse(now, CultureInfo.InvariantCulture).ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture));
        session.RecordTree(scenario + "-after-midnight"); session.Exit();
    }
}
