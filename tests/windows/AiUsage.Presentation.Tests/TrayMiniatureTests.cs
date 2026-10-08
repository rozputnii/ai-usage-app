using AiUsage.Features.Ledger;
using AiUsage.Features.Ledger.Contract;
using AiUsage.Features.Ledger.Demo;
using Xunit;

namespace AiUsage.Presentation.Tests;

/// <summary>AIU-055 R-02 to R-06: the tray flyout as a miniature of the window, one row per account with its main limit's
/// today bar and a five-hour ring.</summary>
public sealed class TrayMiniatureTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static IReadOnlyList<TrayRow> Rows(LedgerSnapshot snapshot, ValueMode mode = ValueMode.Used) =>
        LedgerTrayViewModel.Project(snapshot, LedgerPreferences.Default with { Mode = mode });

    private static IReadOnlyList<TrayRow> Brief(ValueMode mode = ValueMode.Used) => Rows(DemoLedgerScenarios.Build(DemoLedgerScenarios.Brief), mode);

    /// <summary>The ring of the Brief Claude Pro row after changing its main limit.</summary>
    private static TrayRing? ClaudeRing(Func<LimitCardModel, LimitCardModel> change, string scenario = DemoLedgerScenarios.Brief, ValueMode mode = ValueMode.Used)
    {
        var snapshot = DemoLedgerScenarios.Build(scenario);
        var claude = snapshot.Accounts[0];
        return Rows(snapshot with { Accounts = [claude with { Cards = [change(claude.Cards[0])] }] }, mode).Single().Ring;
    }

    private static LimitCardModel Window(LimitCardModel card, Func<FiveHourModel, FiveHourModel> change) => card with { FiveHour = change(card.FiveHour!) };

    [Fact]
    public void EachRowShowsTheMainLimitOnly()
    {
        var rows = Brief();
        Assert.Equal(["Claude Pro", "Codex Pro", "Copilot Free", "Antigravity AI Plus"], rows.Select(r => r.Name));
        Assert.Equal(["claude-week", "codex-week", "copilot-completions", "antigravity-g1"], rows.Select(r => r.Strip!.CardId));
        Assert.Equal(TrayStripKind.SolidCritical, rows[1].Strip!.Kind);
        Assert.Empty(rows[1].Strip!.Cells);
        Assert.True(rows[3].IsError);
        Assert.False(rows[0].IsError);
        Assert.Equal(0.7, rows[3].Strip!.Opacity);
        Assert.Equal("Claude Pro · 5h + 7d", rows[0].Strip!.Tip[0]);
        Assert.StartsWith("Antigravity AI Plus, sync failed", rows[3].AccessibleName, StringComparison.Ordinal);
        // Spending, model, other pool and note limits stay out of the tray.
        Assert.DoesNotContain("Extra usage", rows[0].AccessibleName, StringComparison.Ordinal);
    }

    [Fact]
    public void TodayIsOneCellWithoutFiveHourSplit()
    {
        var claude = Assert.Single(Brief()[0].Strip!.Cells);
        Assert.False(claude.ShowLabel);
        Assert.Equal([9, 9.4], claude.Parts.Select(p => p.Weight), (a, b) => Math.Abs(a - b) < 0.001);
        Assert.Equal("Today", claude.Tip[0]);

        // The cell is the today cell the window draws for the same limit laid out as a period limit.
        var snapshot = DemoLedgerScenarios.Build(DemoLedgerScenarios.Brief);
        foreach (var mode in new[] { ValueMode.Used, ValueMode.Left })
        {
            var account = snapshot.Accounts[0];
            var period = account.Cards[0] with { Layout = CardLayout.Period, FiveHour = null };
            var window = CardVisuals.Build(period, account, mode, snapshot.LocalNow).Cells.Single();
            var tray = Assert.Single(Rows(snapshot, mode)[0].Strip!.Cells);
            Assert.Equal(window.Parts, tray.Parts);
            Assert.Equal(window.Tip, tray.Tip);

            var copilot = snapshot.Accounts[2];
            var completions = CardVisuals.Build(copilot.Cards[0], copilot, mode, snapshot.LocalNow).Cells.Single();
            var today = CardVisuals.TodayOnlyCell(copilot.Cards[0], copilot, mode, snapshot.LocalNow);
            Assert.Equal(completions.Parts, today.Parts);
            Assert.Equal(completions.Tip, today.Tip);
        }
    }

    [Fact]
    public void RingShowsTheCurrentFiveHourWindow()
    {
        var rows = Brief();
        var claude = rows[0].Ring!;
        Assert.Equal(0.72, claude.Fraction, 6);
        Assert.Equal(Tone.Ok, claude.Tone);
        Assert.Equal(["Current 5h window · 72 % used · until 16:05"], claude.Tip);
        var left = Brief(ValueMode.Left)[0].Ring!;
        Assert.Equal(0.28, left.Fraction, 6);
        Assert.Equal(["Current 5h window · 28 % left · until 16:05"], left.Tip);
        var codex = rows[1].Ring!;
        Assert.Equal(0.91, codex.Fraction, 6);
        Assert.Equal(["Current 5h window · 91 % used · until 15:48"], codex.Tip);
        Assert.Null(rows[2].Ring);
        Assert.Null(rows[3].Ring);

        // Before the window starts only the rail shows, in both modes.
        LimitCardModel NotStarted(LimitCardModel c) => Window(c, f => f with { CurrentWindowStarted = false, CurrentWindowUsed = 0, CurrentWindowEndsAt = null });
        foreach (var mode in new[] { ValueMode.Used, ValueMode.Left })
        {
            var next = ClaudeRing(NotStarted, mode: mode)!;
            Assert.Equal(0, next.Fraction);
            Assert.Equal(["Next 5h window · starts on first use"], next.Tip);
        }

        var full = ClaudeRing(c => Window(c, f => f with { CurrentWindowUsed = 100 }))!;
        Assert.Equal(Tone.Critical, full.Tone);
        Assert.Equal(1, full.Fraction);
        var over = ClaudeRing(c => Window(c, f => f with { CurrentWindowUsed = 130 }), mode: ValueMode.Left)!;
        Assert.Equal(0, over.Fraction);
        Assert.Equal(["Current 5h window · 0 % left · until 16:05"], over.Tip);

        Assert.Equal(Tone.Neutral, ClaudeRing(c => c, DemoLedgerScenarios.DayOff)!.Tone);
        // A period limit without a five-hour window has no ring.
        Assert.Null(ClaudeRing(c => c with { Layout = CardLayout.Period, FiveHour = null }));
    }

    [Fact]
    public void RowsCarryTheProviderAndANamedTip()
    {
        var rows = Brief();
        Assert.Equal([ProviderKind.Claude, ProviderKind.Codex, ProviderKind.Copilot, ProviderKind.Antigravity], rows.Select(r => r.Provider));
        // R-07: a healthy row's tooltip is the display name alone; an error row adds its status lines after it.
        Assert.Equal(["Claude Pro"], rows[0].Tip);
        Assert.Equal(["Codex Pro"], rows[1].Tip);
        Assert.Equal("Antigravity AI Plus", rows[3].Tip[0]);
        Assert.Equal(rows[3].NameTip, rows[3].Tip.Skip(1));
        Assert.StartsWith("Sync failed", rows[3].Tip[1], StringComparison.Ordinal);
        Assert.StartsWith("Showing the reading from", rows[3].Tip[2], StringComparison.Ordinal);

        var expired = Rows(DemoLedgerScenarios.Build(DemoLedgerScenarios.LastWorkDay))[3];
        Assert.Equal(["Antigravity AI Plus", "Sign-in expired", "Sign in again in the window to refresh"], expired.Tip);
    }

    [Fact]
    public async Task SameProviderRowsAreToldApartByTheirTooltip()
    {
        var scheduler = new ManualScheduler();
        var source = new DemoLedgerSource(scheduler);
        source.LoadScenario(DemoLedgerScenarios.Brief);
        using var tray = new LedgerTrayViewModel(source);
        await source.SignInAsync(ProviderKind.Copilot, Token);
        scheduler.Run(TimeSpan.FromSeconds(3));

        var copilot = tray.Rows.Where(r => r.Provider == ProviderKind.Copilot).ToArray();
        Assert.Equal(2, copilot.Length);
        Assert.NotEqual(copilot[0].Tip[0], copilot[1].Tip[0]);
        Assert.Equal(["Copilot Free", "Copilot Free 2"], copilot.Select(r => r.Tip[0]));
    }

    [Fact]
    public void OneWindowLimitShowsTodayAndTheWindowAsARing()
    {
        var rows = Rows(DemoLedgerScenarios.Build(DemoLedgerScenarios.States));
        // The tray tip uses its own label for the cell title, so the row is found by card.
        var row = rows.Single(r => r.Strip?.CardId == "h2");
        Assert.Equal(TrayStripKind.Cells, row.Strip!.Kind);
        Assert.Equal("Today", Assert.Single(row.Strip.Cells).Tip[0]);
        Assert.Equal(["H2 · Codex Pro · 5h + 7d", "3 % of 18 % used"], row.Strip.Tip);
        Assert.Equal(0.4, row.Ring!.Fraction, 6);
        Assert.Equal(["Current 5h window · 40 % used · until 17:10"], row.Ring.Tip);
    }

    [Fact]
    public void UsedUpIsOneSolidStripAndUsedOnlyAnEmptyDashedTrack()
    {
        var rows = Rows(DemoLedgerScenarios.Build(DemoLedgerScenarios.States));
        TrayStrip StripOf(string cardId) => rows.Single(r => r.Strip?.CardId == cardId).Strip!;
        // D-187: a period-unknown (h1) or used-only (h5) main limit draws an empty dashed track; a used-up one (o4) a solid red strip.
        foreach (var (cardId, kind) in new[] { ("h1", TrayStripKind.EmptyDashed), ("h5", TrayStripKind.EmptyDashed), ("o4", TrayStripKind.SolidCritical) })
        {
            Assert.Equal(kind, StripOf(cardId).Kind);
            Assert.Empty(StripOf(cardId).Cells);
        }
    }

    [Fact]
    public void EmptyTrayAndNoteOnlyPrimaryRow()
    {
        var source = new DemoLedgerSource(new ManualScheduler());
        source.LoadScenario(DemoLedgerScenarios.FirstRun);
        using var tray = new LedgerTrayViewModel(source);
        Assert.True(tray.IsEmpty);
        Assert.Empty(tray.Rows);
        Assert.Equal("No accounts yet · open the window to sign in", tray.EmptyText);

        var snapshot = DemoLedgerScenarios.Build(DemoLedgerScenarios.Brief);
        var copilot = snapshot.Accounts[2];
        var row = Rows(snapshot with { Accounts = [copilot with { Cards = [copilot.Cards[2]] }] }).Single();
        Assert.Null(row.Strip);
        Assert.Null(row.Ring);
        Assert.Equal("Copilot Free.", row.AccessibleName);
    }

    [Fact]
    public async Task DensityFollowsPreferences()
    {
        var source = new DemoLedgerSource(new ManualScheduler());
        using var tray = new LedgerTrayViewModel(source);
        Assert.True(tray.IsCompact);
        var changed = new List<string?>();
        tray.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        await source.SetPreferencesAsync(source.Preferences with { Density = Density.Comfortable }, Token);
        Assert.False(tray.IsCompact);
        Assert.Contains(nameof(LedgerTrayViewModel.IsCompact), changed);
        await source.SetPreferencesAsync(source.Preferences with { Density = Density.Compact }, Token);
        Assert.True(tray.IsCompact);
    }

    [Fact]
    public async Task TrayFollowsTheWindowStatesAndOpensAnAccount()
    {
        var source = new DemoLedgerSource(new ManualScheduler());
        using var tray = new LedgerTrayViewModel(source);

        source.LoadScenario(DemoLedgerScenarios.LastWorkDay);
        Assert.Equal(TrayMark.ExtraUsage, tray.Rows[0].Strip!.Mark);
        Assert.Equal(TrayMark.Rush, tray.Rows[1].Strip!.Mark);
        Assert.Equal(TrayMark.Rush, tray.Rows[2].Strip!.Mark);
        Assert.Equal(Tone.Critical, tray.Rows[0].Ring!.Tone);
        Assert.True(tray.Rows[3].IsError);
        Assert.Equal(["Sign-in expired", "Sign in again in the window to refresh"], tray.Rows[3].NameTip);

        source.LoadScenario(DemoLedgerScenarios.DayOff);
        var cells = tray.Rows.Select(r => r.Strip!).Where(s => s.Kind == TrayStripKind.Cells).SelectMany(s => s.Cells).ToArray();
        Assert.NotEmpty(cells);
        Assert.All(cells, c => Assert.True(c.Dashed));
        Assert.Equal(TrayStripKind.SolidCritical, tray.Rows[1].Strip!.Kind);
        await source.SetWorkTodayAsync(true, Token);
        Assert.All(tray.Rows.Select(r => r.Strip!).Where(s => s.Kind == TrayStripKind.Cells).SelectMany(s => s.Cells), c => Assert.False(c.Dashed));

        await source.SetPreferencesAsync(source.Preferences with { Mode = ValueMode.Left }, Token);
        Assert.True(tray.IsLeft);
        string? opened = null;
        tray.OpenAccountRequested += (_, id) => opened = id;
        tray.OpenAccount("acct-codex");
        Assert.Equal("acct-codex", opened);
    }
}
