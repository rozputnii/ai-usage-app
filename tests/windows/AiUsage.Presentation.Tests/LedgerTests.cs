using System.Text.RegularExpressions;
using AiUsage.Features.Ledger;
using AiUsage.Features.Ledger.Contract;
using AiUsage.Features.Ledger.Demo;
using Xunit;

namespace AiUsage.Presentation.Tests;

internal sealed class ManualScheduler : ILedgerScheduler
{
    private readonly List<(TimeSpan Delay, Action Action, Handle Handle)> pending = [];

    public IDisposable Schedule(TimeSpan delay, Action action)
    {
        var handle = new Handle();
        pending.Add((delay, action, handle));
        return handle;
    }

    /// <summary>Runs every live action already scheduled and due within the given delay, in scheduling order.</summary>
    public void Run(TimeSpan upTo)
    {
        var count = pending.Count;
        for (var i = 0; i < count; i++)
        {
            var (delay, action, handle) = pending[i];
            if (handle.Disposed || delay > upTo)
                continue;
            handle.Disposed = true;
            action();
        }
    }

    public sealed class Handle : IDisposable
    {
        public bool Disposed { get; set; }
        public void Dispose() => Disposed = true;
    }
}

/// <summary>AIU-038 contract, demo scenarios and the card drawing rules (spec sections 4 to 6, AC-01 to AC-03).</summary>
public sealed class LedgerCardTests
{
    private static readonly LedgerSnapshot BriefScenario = DemoLedgerScenarios.Build(DemoLedgerScenarios.Brief);
    private static readonly LedgerSnapshot StateGallery = DemoLedgerScenarios.Build(DemoLedgerScenarios.States);

    private static (LimitCardModel Card, AccountModel Account) Find(LedgerSnapshot snapshot, string cardId)
    {
        var account = snapshot.Accounts.First(a => a.Cards.Any(c => c.CardId == cardId));
        return (account.Cards.First(c => c.CardId == cardId), account);
    }

    private static CardVisual Visual(LedgerSnapshot snapshot, string cardId, ValueMode mode = ValueMode.Used)
    {
        var (card, account) = Find(snapshot, cardId);
        return CardVisuals.Build(card, account, mode, snapshot.LocalNow);
    }

    private static CardVisual Brief(string id, ValueMode mode = ValueMode.Used) => Visual(BriefScenario, id, mode);
    private static CardVisual Case(string id, ValueMode mode = ValueMode.Used) => Visual(StateGallery, id, mode);

    [Theory]
    [InlineData(false, null, "Grey: month limit cuts today’s share")]
    [InlineData(true, null, "Grey: month limit cuts today’s share")]
    [InlineData(false, false, "Grey: month limit cuts today’s share")]
    [InlineData(true, false, "Grey: month limit cuts today’s share")]
    [InlineData(false, true, "Grey: cap cuts today’s share")]
    [InlineData(true, true, "Grey: cap cuts today’s share")]
    public void TodayTooltipNamesTheBindingLimit(bool money, bool? binding, string expected)
    {
        var card = DemoLedgerScenarios.Money("synthetic", CardState.OnTrack, 120, 125.5m, 130, 240, 300, 15) with
        {
            Scale = money ? ScaleModel.Money("USD", 2) : ScaleModel.Count("requests"),
            Cap = binding is { } binds ? new(240, binds, CapStatus.Applied) : null
        };
        var account = Find(BriefScenario, "claude-week").Account;
        foreach (var mode in Enum.GetValues<ValueMode>())
        {
            var cell = Assert.Single(CardVisuals.Build(card, account, mode, BriefScenario.LocalNow).Cells);
            Assert.Contains(expected, cell.Tip);
            Assert.Equal(5, cell.Parts.Single(p => p.Paint.Fill == "Grey").Weight);
        }
    }

    [Fact]
    public void LedgerSourcesStayOffTheBackendAndThePlatform()
    {
        var root = Path.Combine(Repository.Root(), "src/windows/AiUsage.Windows/Features/Ledger");
        var sources = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories).Where(p => !p.EndsWith(".xaml.cs", StringComparison.Ordinal)).ToArray();
        Assert.NotEmpty(sources);
        foreach (var path in sources)
        {
            var text = File.ReadAllText(path);
            Assert.False(Regex.IsMatch(text, @"\bAiUsage\.(Core|Infrastructure)\b"), Path.GetFileName(path));
            Assert.DoesNotContain("Microsoft.UI", text);
        }
    }

    /// <summary>AC-03: design-brief 4.3 positions of U0, U and U0 + T as a share of L, to 0.1 point.</summary>
    [Theory]
    [InlineData("claude-week", 38.0, 47.0, 56.4)]
    [InlineData("claude-extra", 69.7, 72.7, 72.0)]
    [InlineData("codex-week", 96.0, 100.0, 97.7)]
    [InlineData("codex-credits", 35.6, 38.1, 40.5)]
    [InlineData("copilot-completions", 59.5, 60.5, 62.6)]
    [InlineData("copilot-chat", 22.0, 24.0, 28.0)]
    [InlineData("antigravity-g1", 30.0, 36.0, 53.3)]
    public void BriefScenarioReproducesTheBarPositions(string cardId, double dayStart, double used, double todayEnd)
    {
        var f = Find(BriefScenario, cardId).Card.Figures;
        var limit = f.EffectiveLimit!.Value;
        decimal At(decimal? v) => Math.Round(v!.Value / limit * 100, 1, MidpointRounding.AwayFromZero);
        Assert.Equal(((decimal)dayStart, (decimal)used, (decimal)todayEnd), (At(f.DayStart), At(f.Used), At(f.TodayEnd)));
    }

    [Fact]
    public void BriefScenarioShowsTheReferencePills()
    {
        Assert.Equal(4, BriefScenario.Accounts.Count);
        Assert.Equal(9, BriefScenario.Accounts.Sum(a => a.Cards.Count));
        Assert.Null(Brief("claude-week").Pill);
        Assert.Equal("over today", Brief("claude-extra").Pill);
        Assert.Equal("7d used up", Brief("codex-week").Pill);
        Assert.Null(Brief("codex-credits").Pill);
        Assert.Null(Brief("copilot-completions").Pill);
        Assert.Null(Brief("copilot-chat").Pill);
        Assert.Equal("not included", Brief("copilot-premium").Pill);
        Assert.Equal("sync failed · 13:38", Brief("antigravity-g1").Pill);
        Assert.Equal("period unknown", Brief("antigravity-g2").Pill);
    }

    [Fact]
    public void CapIsTheFullBarAndATickMarksItOnceExceeded()
    {
        var visual = Brief("claude-extra");
        Assert.Null(visual.CapTick);
        Assert.DoesNotContain(visual.Segments, s => s.Paint.Fill == "CardTop");
        Assert.DoesNotContain(visual.BarTip, line => line.StartsWith("Hatched", StringComparison.Ordinal));
        Assert.StartsWith("$218.00 of $300.00 used", visual.BarTip[1], StringComparison.Ordinal);
        var (card, account) = Find(BriefScenario, "claude-extra");
        var over = CardVisuals.Build(card with { State = CardState.OverCap, Figures = card.Figures with { Used = 320 } }, account, ValueMode.Used, BriefScenario.LocalNow);
        Assert.Equal(93.75, over.CapTick!.Value, 3);
        Assert.Contains("Past the tick: over custom cap $300.00", over.BarTip);
        Assert.Equal("+$2.00", visual.OverLabel);
        Assert.Equal(Tone.Critical, visual.Tone);
        Assert.Equal("≈ $218.00 of $300.00 cap", visual.Footer);
        Assert.Equal("resets 1 Nov (assumed)", visual.ResetText);
        Assert.Equal("−$2.00", Brief("claude-extra", ValueMode.Left).OverLabel);
        Assert.Equal("≈ $82.00 left to cap", Brief("claude-extra", ValueMode.Left).Footer);
    }

    [Fact]
    public void FiveHourStripSplitsTodaysAllowanceIntoWindows()
    {
        var visual = Brief("claude-week");
        Assert.Equal(2, visual.Cells.Count);
        var current = visual.Cells[0];
        Assert.True(current.ShowLabel);
        Assert.Equal(new[] { 72.0, 28.0 }, current.Parts.Select(p => Math.Round(p.Weight, 2)));
        Assert.Equal(new Paint("OkM"), current.Parts[0].Paint);
        Assert.Equal(new Paint("OkP", "Rail"), current.Parts[1].Paint);
        var next = visual.Cells[1];
        Assert.Equal(new[] { 50.33, 49.67 }, next.Parts.Select(p => Math.Round(p.Weight, 2)));
        Assert.Equal(new Paint("Grey"), next.Parts[1].Paint);
        Assert.Equal(["Current 5h window · until 16:05", "72 % used · 28 % allowed today"], current.Tip);
        Assert.Equal("47 % used · ≈ 4 × 5h left", visual.Footer);
        Assert.Equal("resets Mon 09:00", visual.ResetText);
        Assert.Null(visual.OverLabel);
    }

    [Fact]
    public void LeftModeMirrorsTheStripAndSwapsSolidAndHatch()
    {
        var visual = Brief("claude-week", ValueMode.Left);
        Assert.False(visual.Cells[0].ShowLabel);
        Assert.True(visual.Cells[1].ShowLabel);
        var current = visual.Cells[1];
        Assert.Equal(new[] { new Paint("OkM"), new Paint("OkP", "Rail") }, current.Parts.Select(p => p.Paint));
        Assert.Equal("53 % left · ≈ 4 × 5h left", visual.Footer);
        Assert.Equal("28 % of window left today", current.Tip[1]);
        Assert.Equal(new Paint("Grey"), visual.Cells[0].Parts[0].Paint);
    }

    [Fact]
    public void PeriodBarRingsTodayAndMarksDividers()
    {
        var visual = Brief("claude-week");
        Assert.Equal(38, visual.Ring!.Left, 3);
        Assert.Equal(56.4 - 38, visual.Ring.Width, 3);
        Assert.Equal(new BarSegment(0, 38, new Paint("Prev")), visual.Segments[0]);
        Assert.NotEmpty(visual.Dividers);
        Assert.All(visual.Dividers, d => Assert.True(d > 47));
        Assert.Null(visual.OverTick);
    }

    [Fact]
    public void UsedUpShowsOnlyTheRedPeriodBarAndWhenItComesBack()
    {
        var visual = Brief("codex-week");
        Assert.False(visual.HasStrip);
        Assert.Empty(visual.Cells);
        Assert.True(visual.HasPeriodBar);
        Assert.Equal([new BarSegment(0, 100, new Paint("CritM"))], visual.Segments);
        Assert.Null(visual.Ring);
        Assert.Null(visual.OverLabel);
        Assert.Equal("back Fri 09:30", visual.ResetText);
        Assert.Equal(["7d limit used up", "Back Fri 16 Oct 09:30 · in 1 d 19 h 10 min"], visual.PillTip);
        Assert.Contains("7 day used up", visual.AccessibleName, StringComparison.Ordinal);
        Assert.Equal([new BarSegment(0, 100, new Paint("CritP", "CritD"))], Brief("codex-week", ValueMode.Left).Segments);
    }

    [Fact]
    public void StaleReadingsAreDimmedWithTheirTime()
    {
        var visual = Brief("antigravity-g1");
        Assert.Equal(0.7, visual.Opacity);
        Assert.Equal(Tone.Neutral, visual.Tone);
        Assert.Equal("Sync failed 14:15 · showing the reading from 13:38", visual.PillTip[0]);
        Assert.Contains("42 min old", visual.PillTip[1], StringComparison.Ordinal);
        Assert.Contains("sync failed, reading from 13:38, 42 minutes old", visual.AccessibleName, StringComparison.Ordinal);
        var unknown = Brief("antigravity-g2");
        Assert.Equal("no daily budget", unknown.TodayNote);
        Assert.Equal("Period unknown · as of 13:38", unknown.PillTip[0]);
        Assert.Equal("19 % used", unknown.Footer);
        Assert.False(unknown.HasStrip);
        Assert.DoesNotContain("7d", unknown.PeriodLabel, StringComparison.Ordinal);
    }

    [Fact]
    public void PoolsShowProviderFactsAndTrackingEstimate()
    {
        var credits = Brief("codex-credits");
        Assert.Equal("≈ 6,480 of 17,000 cap", credits.Footer);
        Assert.True(credits.FooterIsEstimate);
        Assert.Equal(["Custom cap 17,000 · tracked since 3 Oct (estimate)", "Provider balance 10,160 credits"], credits.FooterTip);
        Assert.Equal("≈ 10,520 left to cap", Brief("codex-credits", ValueMode.Left).Footer);
        var completions = Brief("copilot-completions");
        Assert.Equal("1,210 of 2,000 used", completions.Footer);
        Assert.Equal("resets 1 Nov", completions.ResetText);
        Assert.Equal(["Resets 1 Nov (date from provider)", "Start assumed 1 Oct"], completions.ResetTip);
        Assert.Equal(["Provider limit 2,000 requests", "Provider remaining 790"], completions.FooterTip);
        var premium = Brief("copilot-premium");
        Assert.False(premium.HasPeriodBar);
        Assert.Equal([new NoteLine("month", "not included in plan", "resets 1 Nov")], premium.NoteLines);
    }

    [Theory]
    [InlineData("a1", null, "Ok")]
    [InlineData("a2", "today low", "Attention")]
    [InlineData("a3", "over today", "Critical")]
    [InlineData("a4", "5h full", "Critical")]
    [InlineData("a9", "5h low", "Attention")]
    [InlineData("a6", "7d used up", "Critical")]
    [InlineData("b3", "today used", "Attention")]
    [InlineData("b5", "today short", "Attention")]
    [InlineData("c4", "cap close", "Attention")]
    [InlineData("c5", "cap reached", "Attention")]
    [InlineData("c6", "over cap", "Critical")]
    [InlineData("d6", "over cap", "Critical")]
    [InlineData("g4", "not included", "Neutral")]
    [InlineData("g5", "limit unknown", "Neutral")]
    [InlineData("h1", "period unknown", "Neutral")]
    [InlineData("h5", "not ready", "Neutral")]
    [InlineData("h6", "unknown", "Neutral")]
    [InlineData("h8", "1 h old", "Neutral")]
    [InlineData("h9", "signed out", "Neutral")]
    [InlineData("o1", "day off", "Neutral")]
    [InlineData("o4", "7d used up", "Critical")]
    [InlineData("o5", null, "Ok")]
    [InlineData("r1", "rush", "Ok")]
    [InlineData("r4", "7d used up", "Critical")]
    [InlineData("r6", null, "Ok")]
    public void EveryReferenceStateMapsToItsPillAndTone(string cardId, string? pill, string tone)
    {
        var visual = Case(cardId);
        Assert.Equal((pill, tone), (visual.Pill, visual.Tone.ToString()));
        var word = pill is null ? "OK" : pill.EndsWith(" old", StringComparison.Ordinal) ? "reading from" : pill;
        Assert.Contains(word.Replace("5h", "5 hour", StringComparison.Ordinal).Replace("7d", "7 day", StringComparison.Ordinal), visual.AccessibleName, StringComparison.Ordinal);
    }

    [Fact]
    public void GalleryCoversEveryCardState()
    {
        var states = StateGallery.Accounts.SelectMany(a => a.Cards).Select(c => c.State).ToHashSet();
        foreach (var state in Enum.GetValues<CardState>().Where(s => s != CardState.NoCap))
            Assert.Contains(state, states);
        Assert.Equal(55, StateGallery.Accounts.Count);
    }

    [Fact]
    public void OverTodayLabelsShowTheShareOfToday()
    {
        Assert.Equal("110 %", Case("a3").OverLabel);
        Assert.Equal("−10 %", Case("a3", ValueMode.Left).OverLabel);
        Assert.Equal("+240", Case("c3").OverLabel);
        var b4 = Case("b4");
        Assert.Equal("121 %", b4.OverLabel);
        Assert.True(b4.Cells[0].Parts.Single(p => p.OverEdge).Weight > 0);
        Assert.Equal(44, b4.OverTick!.Value, 3);
        Assert.Null(Case("b3").OverLabel);
    }

    [Fact]
    public void DayOffDrawsTheWouldBeShareNeutralAndDashed()
    {
        var o2 = Case("o2");
        Assert.All(o2.Cells, c => Assert.True(c.Dashed));
        Assert.Null(o2.OverLabel);
        Assert.DoesNotContain(o2.Cells.SelectMany(c => c.Parts), p => p.Paint.Fill.StartsWith("Crit", StringComparison.Ordinal) || p.Paint.Fill.StartsWith("Att", StringComparison.Ordinal));
        Assert.Contains(o2.Cells[0].Parts, p => p.Paint == new Paint("NeutralM", "NeutralP"));
        Assert.Equal(["Day off · 18 % used today", "Monday’s share 20.0 % → 14.0 %"], o2.PillTip);
        var o1 = Case("o1");
        Assert.Contains("Day off · no colours · Work today colours it", o1.Cells[0].Tip);
        Assert.Contains(o1.Cells.SelectMany(c => c.Parts), p => p.Paint.Fill == Paint.Transparent);
        Assert.Equal("Day off · 6 % used today", o1.PillTip[0]);
        var o4 = Case("o4");
        Assert.False(o4.HasStrip);
        Assert.Equal("back Mon 09:30", o4.ResetText);
    }

    [Fact]
    public void WorkTodayColoursTheSameStripsAndMarksExtraDay()
    {
        var o5 = Case("o5");
        Assert.All(o5.Cells, c => Assert.False(c.Dashed));
        Assert.Equal("extra day", Assert.Single(o5.Marks).Text);
        Assert.True(o5.Marks[0].Neutral);
        var o6 = Case("o6");
        Assert.Equal("over today", o6.Pill);
        Assert.Equal("120 %", o6.OverLabel);
    }

    [Fact]
    public void RushSpendsTheWholeRemainderWithoutAPeriodBar()
    {
        var r1 = Case("r1");
        Assert.False(r1.HasPeriodBar);
        Assert.DoesNotContain(r1.Cells.SelectMany(c => c.Parts), p => p.Paint.Fill == "Grey");
        Assert.Equal(["Rush · 30 % left", "Resets Thu 15 Oct 09:00 · use it today"], r1.PillTip);
        var r2 = Case("r2");
        Assert.True(r2.Cells[^1].Weight < 1);
        Assert.DoesNotContain(r2.Cells.SelectMany(c => c.Parts), p => p.Paint.Fill == "Grey");
        var r3 = Case("r3");
        Assert.Equal(2, r3.Cells.Count);
        Assert.Contains("Only 2 × 5h fit before the reset · ≈ 36 % resets unused", r3.Cells[1].Tip);
        Assert.Equal("40 % used · 2 × 5h fit before the reset", r3.Footer);
        Assert.Equal("resets 21:00", r3.ResetText);
        Assert.True(Case("r4").HasPeriodBar);
        Assert.True(Case("r6").HasPeriodBar);
        Assert.Contains(Case("r6").Cells[0].Parts, p => p.Paint.Fill == "Grey" || p.Weight > 0);
        Assert.Null(Case("r6").CapTick);
    }

    [Fact]
    public void FiveHourDividersFollowTheCapScale()
    {
        var (card, account) = Find(BriefScenario, "claude-week");
        CardVisual Build(LimitCardModel model, ValueMode mode = ValueMode.Used) => CardVisuals.Build(model, account, mode, BriefScenario.LocalNow);
        // One 12 % window, 72 % used of the current one: windows start at 38.36 % and the next ones every 12 %.
        Assert.Equal(new[] { 50.36, 62.36, 74.36, 86.36, 98.36 }, Build(card).Dividers.Select(d => Math.Round(d, 2)));
        var capped = card with { Cap = new CapModel(90, true, CapStatus.Applied), CapTargetId = card.CardId, Figures = card.Figures with { EffectiveLimit = 90 } };
        var visual = Build(capped);
        Assert.Equal(new[] { 50.36, 62.36, 74.36, 86.36 }.Select(k => Math.Round(k / 90 * 100, 2)), visual.Dividers.Select(d => Math.Round(d, 2)));
        Assert.Null(visual.CapTick);
        Assert.Equal("47 % of 90 % cap · ≈ 4 × 5h left", visual.Footer);
        Assert.Equal(["Custom cap 90 % · binds", "Provider limit 100 % · 53 % left"], visual.FooterTip.Take(2));
        Assert.StartsWith("47 % of 90 % used", visual.BarTip[1], StringComparison.Ordinal);
        Assert.Equal("43 % left to cap · ≈ 4 × 5h left", Build(capped, ValueMode.Left).Footer);
    }

    [Fact]
    public void MarksCarryTheirFacts()
    {
        var a8 = Case("a8");
        var mark = Assert.Single(a8.Marks);
        Assert.Equal("on extra usage", mark.Text);
        Assert.False(mark.Neutral);
        Assert.Equal(["Window full until 16:05 · continuing on extra usage", "+$2.00 since 13:05"], mark.Tip);
        Assert.Equal(["5h window full", "Next window opens 16:05"], a8.PillTip);
        var h7 = Case("h7");
        Assert.Null(h7.Pill);
        Assert.Equal(["Last sync failed 14:18", "Reading from 14:10 · retry in 5 min"], Assert.Single(h7.Marks).Tip);
        Assert.Equal("past reset", Assert.Single(Case("h5").Marks).Text);
        Assert.Equal("reset 14:00", Case("h5").ResetText);
        Assert.Equal("Sign in", Case("h8").ActionText);
        Assert.Null(Case("g5").ActionText);
    }

    [Fact]
    public void UnknownIsNeverZeroOrUnlimited()
    {
        var h6 = Case("h6");
        Assert.Equal("used unknown", Assert.Single(h6.NoteLines).Value);
        Assert.False(h6.HasPeriodBar);
        var g3 = Case("g3");
        Assert.Equal(["Custom cap 300 · binds", "Provider: unlimited"], g3.FooterTip);
        Assert.Equal("120 of 300 used", g3.FooterTip.Count > 0 ? "120 of 300 used" : string.Empty);
        Assert.Equal("126 of 300 cap", g3.Footer);
    }

    [Fact]
    public void FiveHourWithoutAnEstimateIsOneStrip()
    {
        var h2 = Case("h2");
        var cell = Assert.Single(h2.Cells);
        Assert.True(cell.ShowLabel);
        Assert.Equal([40.0, 60.0], cell.Parts.Select(p => p.Weight));
        Assert.Equal([new Paint("OkM"), new Paint(Paint.Transparent)], cell.Parts.Select(p => p.Paint));
        Assert.Equal(["Current 5h window · until 17:10", "40 % used", "Window count: collecting data"], cell.Tip);
        Assert.Equal("33 % used", h2.Footer);
        Assert.Equal(["5h window size not estimated yet", "Current 5h window 40 % used · until 17:10"], h2.FooterTip);
        Assert.Contains("Current five-hour window 40 percent used.", h2.AccessibleName, StringComparison.Ordinal);
    }

    private static CardVisual Variant(string id, Func<LimitCardModel, LimitCardModel> change, ValueMode mode = ValueMode.Used)
    {
        var (card, account) = Find(StateGallery, id);
        return CardVisuals.Build(change(card), account, mode, StateGallery.LocalNow);
    }

    private static LimitCardModel NoEstimate(LimitCardModel card) => card with { FiveHour = card.FiveHour! with { WindowShare = null } };

    [Fact]
    public void OneWindowCellFollowsLeftModeDayOffAndFull()
    {
        var left = Assert.Single(Case("h2", ValueMode.Left).Cells);
        Assert.Equal("60 % left", left.Tip[1]);
        // D-186: the unfilled track sits right of the solid fill in both modes.
        Assert.Equal([60.0, 40.0], left.Parts.Select(p => p.Weight));
        Assert.Equal([new Paint("OkM"), new Paint(Paint.Transparent)], left.Parts.Select(p => p.Paint));

        var next = Assert.Single(Variant("a5", NoEstimate).Cells);
        Assert.Equal(["Next 5h window · starts on first use", "Window count: collecting data"], next.Tip);

        var full = Variant("a4", NoEstimate);
        Assert.Equal("5h full", full.Pill);
        var fullCell = Assert.Single(full.Cells);
        Assert.Equal(100, Assert.Single(fullCell.Parts).Weight);
        Assert.Equal("100 % used", fullCell.Tip[1]);

        var off = Assert.Single(Variant("h2", c => c with { State = CardState.DayOff }).Cells);
        Assert.True(off.Dashed);
        Assert.DoesNotContain(off.Parts, p => p.Paint.Fill.StartsWith("Crit", StringComparison.Ordinal) || p.Paint.Stripe?.StartsWith("Crit", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void RoughEstimateShowsRangeAndSettledShowsOneNumber()
    {
        var h3 = Case("h3");
        Assert.Equal("50 % used · ≈ 3–7 × 5h left", h3.Footer);
        Assert.Equal("One 5h window ≈ 10 % of 7d (7–13 %) · rough · from 3 windows", h3.FooterTip[0]);
        Assert.True(h3.Cells.Count > 1);

        var settled = Variant("h3", c => DemoLedgerScenarios.FiveHour("h3", CardState.OnTrack, 44, 50, 82.4m, 30,
            DemoLedgerScenarios.At(10, 14, 17, 35), DemoLedgerScenarios.At(10, 19, 9, 0), ws: 10, low: 9.5m, high: 10.5m));
        Assert.Equal("50 % used · ≈ 5 × 5h left", settled.Footer);
        Assert.Equal("One 5h window ≈ 10 % of 7d (9–11 %) · from 3 windows", settled.FooterTip[0]);

        var single = Variant("h3", c => c with { FiveHour = c.FiveHour! with { Windows = 1 } });
        Assert.EndsWith("· rough · from 1 window", single.FooterTip[0], StringComparison.Ordinal);
    }

    [Fact]
    public void FormatsFollowTheReference()
    {
        var now = DemoLedgerScenarios.BriefNow;
        Assert.Equal("in 1 h 45 min", LedgerFormat.Relative(DemoLedgerScenarios.At(10, 14, 16, 5), now));
        Assert.Equal("20 min ago", LedgerFormat.Relative(DemoLedgerScenarios.At(10, 14, 14, 0), now));
        Assert.Equal("−$2.00", LedgerFormat.Value(ScaleModel.Money("USD", 2), -2m));
        Assert.Equal("€250.00", LedgerFormat.Value(ScaleModel.Money("EUR", 2), 250m));
        Assert.Equal("1,210", LedgerFormat.Value(ScaleModel.Count("requests"), 1210m));
        Assert.Equal("57 %", LedgerFormat.Value(ScaleModel.Percent, 56.5m));
        Assert.True(LedgerFormat.TryParseAmount("17,000", ScaleModel.Count("credits"), out var credits));
        Assert.Equal(17000m, credits);
        Assert.False(LedgerFormat.TryParseAmount("1.5", ScaleModel.Count("credits"), out _));
        Assert.False(LedgerFormat.TryParseAmount("1.234", ScaleModel.Money("USD", 2), out _));
        Assert.False(LedgerFormat.TryParseAmount("-3", ScaleModel.Money("USD", 2), out _));
        Assert.True(LedgerFormat.TryParseAmount("$300.50", ScaleModel.Money("USD", 2), out var money));
        Assert.Equal(300.50m, money);
    }
}

/// <summary>The title-bar refresh button: red while an account is not refreshed in time; the tip gives reading times without dates.</summary>
public sealed class LedgerRefreshStatusTests
{
    private static readonly DateTimeOffset Now = DemoLedgerScenarios.BriefNow;

    private static AccountModel Account(string name, AccountHealth health, DateTimeOffset? readingAt, DateTimeOffset? failedAt = null) =>
        new(name, ProviderKind.Claude, name, health, readingAt, failedAt, null, []);

    [Fact]
    public void OneLineWhenEveryAccountReadAtTheSameMinute()
    {
        var (failed, tip) = LedgerViewModel.RefreshStatus([
            Account("Claude Pro", AccountHealth.Ok, Now.AddSeconds(-20)),
            Account("Codex Pro", AccountHealth.SyncFailedFresh, Now.AddSeconds(-40), Now),
            Account("Old", AccountHealth.SignedOut, Now.AddDays(-3))]);
        Assert.False(failed);
        Assert.Equal(["Updated 14:19"], tip);
    }

    [Fact]
    public void FailedAccountsTurnRedAndGetTheirOwnLines()
    {
        var (failed, tip) = LedgerViewModel.RefreshStatus([
            Account("Claude Pro", AccountHealth.Ok, Now.AddMinutes(-5)),
            Account("Copilot business", AccountHealth.SyncFailedStale, Now.AddMinutes(-30), Now.AddMinutes(-2)),
            Account("Codex Pro", AccountHealth.SignInExpired, Now.AddHours(-1)),
            Account("Antigravity", AccountHealth.ProviderError, null)]);
        Assert.True(failed);
        Assert.Equal(["Claude Pro · 14:15", "Copilot business · failed 14:18, showing 13:50", "Codex Pro · sign-in expired, showing 13:20",
            "Antigravity · provider error"], tip);
    }

    [Fact]
    public void DifferentTimesListEveryAccountWithoutRed()
    {
        var (failed, tip) = LedgerViewModel.RefreshStatus([Account("Claude Pro", AccountHealth.Ok, Now.AddMinutes(-1)), Account("Codex Pro", AccountHealth.Ok, Now.AddMinutes(-4))]);
        Assert.False(failed);
        Assert.Equal(["Claude Pro · 14:19", "Codex Pro · 14:16"], tip);
    }

    [Fact]
    public void NoAccountsOffersRefreshOnly()
    {
        var (failed, tip) = LedgerViewModel.RefreshStatus([Account("Old", AccountHealth.SignedOut, Now)]);
        Assert.False(failed);
        Assert.Equal(["Refresh (F5)"], tip);
    }
}

/// <summary>Window, settings, history and tray behaviour on demo data (AC-04 to AC-06).</summary>
public sealed class LedgerInteractionTests
{
    private static (LedgerViewModel Window, DemoLedgerSource Source, ManualScheduler Scheduler, List<string> Spoken) Start(string scenario = DemoLedgerScenarios.Brief)
    {
        var scheduler = new ManualScheduler();
        var source = new DemoLedgerSource(scheduler);
        source.LoadScenario(scenario);
        var spoken = new List<string>();
        var demo = new LedgerDemoControls([.. DemoLedgerScenarios.All.Select(s => new DemoScenario(s.Id, s.Title))], source.LoadScenario, source.DismissStrip);
        return (new LedgerViewModel(source, scheduler, spoken.Add, demo), source, scheduler, spoken);
    }

    private static LimitCardViewModel Card(LedgerViewModel window, string id) => window.Cards.Single(c => c.CardId == id);

    [Fact]
    public void BriefWindowListsNineCardsInUserOrder()
    {
        var (window, _, _, _) = Start();
        Assert.Equal(["claude-week", "claude-extra", "codex-week", "codex-credits", "copilot-completions", "copilot-chat", "copilot-premium", "antigravity-g1", "antigravity-g2"],
            window.Cards.Select(c => c.CardId));
        Assert.True(window.RefreshFailed);
        Assert.Equal("Antigravity AI Plus · failed 14:15, showing 13:38", window.RefreshTip[^1]);
        Assert.False(window.IsDayOff);
        Assert.True(window.IsCompact);
        Assert.False(window.IsFirstRun);
    }

    [Fact]
    public async Task ValueModeAndDensitySwitchEveryCard()
    {
        var (window, _, _, _) = Start();
        await window.ToggleValueModeAsync();
        Assert.True(window.IsLeft);
        Assert.Equal("Show values: left", window.ValueModeName);
        Assert.Equal("53 % left · ≈ 4 × 5h left", Card(window, "claude-week").Visual.Footer);
        await window.Settings.SetDensityAsync(Density.Comfortable);
        Assert.False(window.IsCompact);
    }

    [Fact]
    public async Task UpdatesSectionShowsVersionStatusAndModeSelector()
    {
        var (window, _, _, _) = Start();
        Assert.Equal("Version 2026.10.604.0", window.Settings.UpdateVersionText);
        Assert.Matches(@"^Up to date · checked \d\d:\d\d$", window.Settings.UpdatesText);
        Assert.False(window.Settings.IsUpdateNotable); // up to date stays a tooltip (D-193)
        Assert.True(window.Settings.CanCheckUpdates);
        Assert.False(window.Settings.CanInstallUpdate);
        Assert.True(window.Settings.IsUpdateAlways);
        await window.Settings.SetUpdateModeAsync(UpdateMode.OnLaunch);
        Assert.True(window.Settings.IsUpdateOnLaunch);
        Assert.False(window.Settings.IsUpdateAlways);
    }

    [Fact]
    public void InstallButtonStaysVisibleAndBusyWhileInstalling()
    {
        var (window, source, _, _) = Start();
        var installing = source.Current with { Summaries = source.Current.Summaries with { Updates = new(UpdateState.Installing, "2026.10.604.0") } };
        window.Settings.Rebuild(installing, source.Preferences);
        Assert.True(window.Settings.CanInstallUpdate);
        Assert.True(window.Settings.IsInstallingUpdate);
        Assert.Equal("Installing", window.Settings.InstallUpdateText);
        Assert.Equal("Downloading and installing · the app will restart", window.Settings.UpdatesText);
        Assert.True(window.Settings.IsUpdateNotable);
        Assert.False(window.Settings.CanCheckUpdates);

        var ready = installing with { Summaries = installing.Summaries with { Updates = new(UpdateState.Ready, "2026.10.604.0") } };
        window.Settings.Rebuild(ready, source.Preferences);
        Assert.False(window.Settings.IsInstallingUpdate);
        Assert.Equal("Install and restart", window.Settings.InstallUpdateText);
    }

    [Fact]
    public async Task RenameSavesOnEnterAndCancelsOnEscape()
    {
        var (window, source, _, _) = Start();
        var card = Card(window, "claude-week");
        card.BeginRename();
        card.RenameText = "Claude Pro work";
        card.CancelRename();
        Assert.Equal("Claude Pro", source.Current.Accounts[0].DisplayName);
        card.BeginRename();
        card.RenameText = "  Claude Pro work ";
        await card.CommitRenameAsync();
        Assert.False(card.IsRenaming);
        Assert.Equal("Claude Pro work", source.Current.Accounts[0].DisplayName);
        Assert.Equal("Claude Pro work", Card(window, "claude-extra").Name);
    }

    [Fact]
    public async Task CapEditorValidatesSavesAndRemovesWithUndo()
    {
        var (window, source, scheduler, _) = Start();
        var credits = Card(window, "codex-credits");
        credits.OpenSettings();
        var editor = credits.Settings!.Cap!;
        Assert.Equal("17000", editor.Text);
        Assert.Equal("credits", editor.UnitText);
        editor.Text = "12.5";
        await editor.SaveAsync();
        Assert.Equal("Enter a whole number", editor.Error);
        editor.Text = "18000";
        await editor.SaveAsync();
        Assert.Null(editor.Error);
        Assert.Equal(18000m, Card(window, "codex-credits").Model.Cap!.Amount);

        credits.OpenSettings();
        credits.Settings!.Cap!.Text = string.Empty;
        await credits.Settings.Cap.SaveAsync();
        var removed = Card(window, "codex-credits");
        Assert.Equal(CardState.NoCap, removed.Model.State);
        Assert.Equal([new NoteLine(string.Empty, "balance 10,160 credits (provider)", "no budget")], removed.Visual.NoteLines);
        Assert.True(window.HasUndo);
        Assert.Equal("Cap removed · Codex Pro credits", window.UndoText);
        Assert.DoesNotContain(source.Current.Budget.Caps, c => c.CapTargetId == "codex-credits");

        await window.UndoAsync();
        Assert.False(window.HasUndo);
        Assert.Equal(18000m, Card(window, "codex-credits").Model.Cap!.Amount);
        Assert.Equal(CardState.OnTrack, Card(window, "codex-credits").Model.State);
        scheduler.Run(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void CapEditorAcceptsOnlyDigitsWithinTheProviderLimit()
    {
        static CapEditorViewModel Editor(ScaleModel scale, LimitValue limit, decimal? current = null) =>
            new(scale, limit, current, "month", _ => Task.FromResult(true), () => { });
        var requests = Editor(ScaleModel.Count("requests"), LimitValue.Known(17500));
        Assert.True(requests.Accepts(string.Empty));
        Assert.True(requests.Accepts("17500"));
        Assert.False(requests.Accepts("17501"));
        Assert.False(requests.Accepts("-1"));
        Assert.False(requests.Accepts("1a"));
        Assert.False(requests.Accepts("1.5"));
        Assert.False(requests.Accepts("1,000"));
        var money = Editor(ScaleModel.Money("USD", 2), LimitValue.Known(500));
        Assert.True(money.Accepts("12."));
        Assert.True(money.Accepts("500.00"));
        Assert.False(money.Accepts("500.01"));
        Assert.False(money.Accepts("1.234"));
        Assert.False(money.Accepts("$5"));
        Assert.True(Editor(ScaleModel.Count("credits"), LimitValue.Unknown).Accepts("99999999"));
        // A cap kept above the limit opens unchanged and can only be lowered.
        var kept = Editor(ScaleModel.Count("requests"), LimitValue.Known(17500), 170000);
        Assert.Equal("170000", kept.Text);
        Assert.True(kept.Accepts("170000"));
        Assert.True(kept.Accepts("17000"));
        Assert.False(kept.Accepts("1700000"));
    }

    [Fact]
    public async Task CapEditorRejectsAnAmountAboveTheProviderLimit()
    {
        var (window, _, _, _) = Start();
        var extra = Card(window, "claude-extra");
        extra.OpenSettings();
        var editor = extra.Settings!.Cap!;
        editor.Text = "500.01";
        await editor.SaveAsync();
        Assert.Equal("Enter at most $500.00 · the provider limit", editor.Error);
        Assert.Equal(300m, Card(window, "claude-extra").Model.Cap!.Amount);
        editor.Text = "500";
        await editor.SaveAsync();
        Assert.Null(editor.Error);
        Assert.Equal(500m, Card(window, "claude-extra").Model.Cap!.Amount);
    }

    [Fact]
    public async Task CapOnALimitUnknownPoolIsSetInItsSettings()
    {
        var (window, _, _, _) = Start(DemoLedgerScenarios.States);
        var g5 = Card(window, "g5");
        Assert.Null(g5.Visual.ActionText);
        Assert.True(g5.HasSettings);
        g5.OpenSettings();
        var editor = g5.Settings!.Cap!;
        Assert.Equal(string.Empty, editor.Text);
        await editor.SaveAsync();
        Assert.Equal("Enter a cap", editor.Error);
        editor.Text = "500";
        await editor.SaveAsync();
        var capped = Card(window, "g5").Model;
        Assert.Equal(CardLayout.Pool, capped.Layout);
        Assert.Equal(500m, capped.Cap!.Amount);
    }

    [Fact]
    public async Task UndoExpiresAfterTenSecondsUnlessFocused()
    {
        var (window, _, scheduler, _) = Start();
        await window.Settings.WorkDays.Single(d => d.Day == DayOfWeek.Saturday).ToggleAsync();
        Assert.Equal("Saturday added to work days", window.UndoText);
        window.HoldUndo(true);
        scheduler.Run(TimeSpan.FromSeconds(10));
        Assert.True(window.HasUndo);
        window.HoldUndo(false);
        scheduler.Run(TimeSpan.FromSeconds(10));
        Assert.False(window.HasUndo);
    }

    [Fact]
    public async Task WorkDayChangesOfferUndo()
    {
        var (window, source, _, _) = Start();
        Assert.Equal(["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"], window.Settings.WorkDays.Select(d => d.Label));
        await window.Settings.WorkDays.Single(d => d.Day == DayOfWeek.Friday).ToggleAsync();
        Assert.DoesNotContain(DayOfWeek.Friday, source.Current.Budget.WorkDays);
        Assert.False(window.Settings.WorkDays.Single(d => d.Day == DayOfWeek.Friday).IsOn);
        Assert.Equal("Friday removed from work days", window.UndoText);
        await window.UndoAsync();
        Assert.Contains(DayOfWeek.Friday, source.Current.Budget.WorkDays);
    }

    [Fact]
    public async Task WorkDayNameNotifiesBindingsAndTheLastSelectionCannotBeRemoved()
    {
        var (window, _, _, _) = Start();
        var friday = window.Settings.WorkDays.Single(d => d.Day == DayOfWeek.Friday);
        var changed = new List<string?>();
        friday.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        await friday.ToggleAsync();
        Assert.Contains(nameof(WorkDayToggle.AccessibleName), changed);
        Assert.Equal("Friday, day off", friday.AccessibleName);
        foreach (var day in new[] { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday })
            await window.Settings.WorkDays.Single(d => d.Day == day).ToggleCommand.ExecuteAsync(null);
        var monday = window.Settings.WorkDays.Single(d => d.Day == DayOfWeek.Monday);
        Assert.False(monday.ToggleCommand.CanExecute(null));
        await friday.ToggleCommand.ExecuteAsync(null);
        Assert.True(monday.ToggleCommand.CanExecute(null));
    }

    [Fact]
    public async Task WorkTodayColoursTheDayOffUntilUndone()
    {
        var (window, source, _, _) = Start(DemoLedgerScenarios.DayOff);
        Assert.True(window.IsDayOff);
        Assert.Equal("Day off", window.DayText);
        Assert.Equal("day off", Card(window, "claude-week").Visual.Pill);
        Assert.Equal("7d used up", Card(window, "codex-week").Visual.Pill);
        var workDays = source.Current.Budget.WorkDays;
        await window.ToggleWorkTodayAsync();
        Assert.True(window.WorkTodayOn);
        Assert.Equal("Extra work day · until midnight", window.DayText);
        Assert.Null(Card(window, "claude-week").Visual.Pill);
        Assert.Equal("extra day", Card(window, "claude-week").Visual.Marks.Single().Text);
        Assert.Equal("7d used up", Card(window, "codex-week").Visual.Pill);
        Assert.Same(workDays, source.Current.Budget.WorkDays);
        await window.UndoAsync();
        Assert.False(window.WorkTodayOn);
        Assert.Equal("day off", Card(window, "claude-week").Visual.Pill);
    }

    [Fact]
    public async Task DeleteStoredDataIsConfirmedInPlace()
    {
        var (window, _, _, _) = Start();
        await window.Settings.ConfirmDeleteAsync();
        Assert.False(window.IsFirstRun);
        window.Settings.ArmDelete();
        Assert.True(window.Settings.IsDeleteArmed);
        Assert.True(window.Escape());
        Assert.False(window.Settings.IsDeleteArmed);
        window.Settings.ArmDelete();
        await window.Settings.ConfirmDeleteAsync();
        Assert.True(window.IsFirstRun);
        Assert.Empty(window.Cards);
        Assert.All(window.Providers, p => Assert.Equal("Sign in", p.ButtonText));
    }

    [Fact]
    public async Task SignInShowsProgressThenAddsTheAccount()
    {
        var (window, _, scheduler, spoken) = Start(DemoLedgerScenarios.FirstRun);
        Assert.True(window.IsFirstRun);
        Assert.Equal(["Claude", "Codex", "GitHub Copilot", "Antigravity"], window.Providers.Select(p => p.Name));
        await window.SignInAsync(ProviderKind.Codex);
        Assert.True(window.HasStrip);
        Assert.True(window.StripBusy);
        Assert.Equal("Codex", window.StripTitle);
        Assert.Equal("WDJB-MJHT", window.StripCode);
        Assert.Empty(window.StripText);
        Assert.Equal("Cancel", window.StripAction);
        Assert.Equal("Waiting…", window.Providers.Single(p => p.Provider == ProviderKind.Codex).ButtonText);
        scheduler.Run(TimeSpan.FromSeconds(3));
        Assert.False(window.IsFirstRun);
        Assert.Equal("Codex Pro", window.StripTitle);
        Assert.Equal("added · 2 limits", window.StripText);
        Assert.All(window.Cards, c => Assert.True(c.IsNew));
        Assert.Contains("Codex Pro added · 2 limits", spoken);
        scheduler.Run(TimeSpan.FromSeconds(4));
        Assert.All(window.Cards, c => Assert.False(c.IsNew));
        Assert.False(window.HasStrip);
        Assert.Equal("added", window.Providers.Single(p => p.Provider == ProviderKind.Codex).MenuRight);
    }

    [Fact]
    public async Task SignInStripNamesOnlyTheNextStep()
    {
        var (window, _, _, _) = Start(DemoLedgerScenarios.FirstRun);
        await window.SignInAsync(ProviderKind.Antigravity);
        Assert.Equal("finish in the browser", window.StripText);
        Assert.Empty(window.StripCode);
        Assert.False(window.StripAcceptsCode);
        await window.StripActionAsync();

        await window.SignInAsync(ProviderKind.Claude);
        Assert.Equal("Claude", window.StripTitle);
        Assert.Equal("sign in, then paste the code here", window.StripText);
        Assert.Empty(window.StripCode);
        Assert.True(window.StripAcceptsCode);
        window.SubmitSignInCode();
        Assert.Equal("code not accepted", window.CodeError);
        Assert.True(window.StripBusy);
        window.SignInCode = "pasted-code";
        Assert.Empty(window.CodeError);
        window.SubmitSignInCode();
        Assert.Empty(window.CodeError);
        Assert.False(window.StripBusy);
        Assert.StartsWith("added", window.StripText);
        Assert.Empty(window.StripCode);
    }

    [Fact]
    public async Task CancellingSignInHidesTheStripAtOnce()
    {
        var (window, _, scheduler, _) = Start();
        await window.SignInAsync(ProviderKind.Antigravity);
        await window.StripActionAsync();
        Assert.False(window.HasStrip);
        await window.SignInAsync(ProviderKind.Antigravity);
        Assert.True(window.HasStrip);
        Assert.True(window.StripBusy);
        scheduler.Run(TimeSpan.FromSeconds(3));
        Assert.StartsWith("added", window.StripText);
    }

    [Fact]
    public async Task CancelledSignInOffersTryAgain()
    {
        var (window, _, _, _) = Start(DemoLedgerScenarios.SignIn);
        Assert.Equal("Codex", window.StripTitle);
        Assert.Equal("cancelled", window.StripText);
        Assert.Equal("Try again", window.StripAction);
        Assert.True(window.StripActionIsPrimary);
        Assert.False(window.StripBusy);
        await window.StripActionAsync();
        Assert.True(window.StripBusy);
    }

    [Fact]
    public void CancelledSignInHidesItselfAfterAWhile()
    {
        var (window, _, scheduler, _) = Start(DemoLedgerScenarios.SignIn);
        scheduler.Run(TimeSpan.FromSeconds(7));
        Assert.True(window.HasStrip);
        scheduler.Run(TimeSpan.FromSeconds(8));
        Assert.False(window.HasStrip);
    }

    [Fact]
    public async Task FinishedStripCanBeClosedAndANewSignInShowsItAgain()
    {
        var (window, _, _, _) = Start(DemoLedgerScenarios.SignIn);
        window.DismissStrip();
        Assert.False(window.HasStrip);
        await window.SignInAsync(ProviderKind.Antigravity);
        Assert.True(window.HasStrip);
        Assert.True(window.StripBusy);
        window.DismissStrip();
        Assert.True(window.HasStrip);
    }

    [Fact]
    public async Task ExpiredSignInIsRestoredInline()
    {
        var (window, _, scheduler, _) = Start(DemoLedgerScenarios.SignIn);
        Assert.Equal("Codex", window.StripTitle);
        Assert.Equal("cancelled", window.StripText);
        var week = Card(window, "claude-week");
        Assert.Equal("sign-in expired", week.Visual.Marks.Single().Text);
        Assert.Equal("1 h old", week.Visual.Pill);
        await week.ActionAsync();
        scheduler.Run(TimeSpan.FromSeconds(3));
        week = Card(window, "claude-week");
        Assert.Empty(week.Visual.Marks);
        Assert.False(week.IsStale);
        Assert.Null(week.Visual.ActionText);
    }

    [Fact]
    public async Task SignOutIsImmediateAndHidesTheAccountUntilShown()
    {
        var (window, source, _, spoken) = Start();
        await Card(window, "codex-week").SignOutAsync();
        Assert.DoesNotContain(window.Cards, c => c.Account.AccountId == "acct-codex");
        Assert.Contains(spoken, s => s.Contains("history, name and caps kept", StringComparison.Ordinal));
        Assert.Contains(source.Current.Budget.Caps, c => c.CapTargetId == "codex-credits");
        await window.ToggleShowSignedOutAsync();
        var signedOut = window.Cards.Single(c => c.Account.AccountId == "acct-codex");
        Assert.Equal("signed out", signedOut.Visual.Pill);
        Assert.Equal("Sign in", signedOut.Visual.ActionText);
    }

    [Fact]
    public async Task HistoryOpensUnderItsCardWithGapsAndClosesOnEscape()
    {
        var (window, _, _, _) = Start();
        var card = Card(window, "claude-week");
        await card.ToggleHistoryAsync();
        var history = window.History!;
        Assert.True(card.IsHistoryOpen);
        Assert.Equal(36, history.DayCount);
        Assert.Equal(new HistoryGap(16, 4), Assert.Single(history.Gaps));
        Assert.Equal(32, history.Bars.Count);
        Assert.True(history.Bars[^1].IsToday);
        Assert.Equal(9m, history.Bars[^1].Value);
        Assert.Equal("baseline 20 % / work day", history.BaselineText);
        Assert.Equal("Wed 14 Oct · 9 % of 7d", history.FocusText);
        Assert.All(history.ResetTicks, i => Assert.Equal(DayOfWeek.Monday, DateOnly.FromDateTime(new DateTime(2026, 9, 9)).AddDays(i).DayOfWeek));
        history.MoveFocus(-17);
        Assert.Equal("Sun 27 Sep · no readings", history.FocusText);
        Assert.True(window.Escape());
        Assert.Null(window.History);
        Assert.False(card.IsHistoryOpen);
        await Card(window, "copilot-premium").ToggleHistoryAsync();
        Assert.Null(window.History);
    }

    [Fact]
    public async Task EscapeClosesEditorsBeforeHistoryAndPanel()
    {
        var (window, _, _, _) = Start();
        window.ToggleSettings();
        await Card(window, "claude-week").ToggleHistoryAsync();
        Assert.True(window.Escape());
        Assert.Null(window.History);
        Assert.True(window.Escape());
        Assert.False(window.IsSettingsOpen);
        Assert.False(window.Escape());
    }

    [Fact]
    public async Task CardsReorderWithinTheirAccount()
    {
        var (window, _, _, _) = Start();
        await Card(window, "claude-extra").MoveUpAsync();
        Assert.Equal("claude-extra", window.Cards[0].CardId);
        Assert.Equal("claude-week", window.Cards[1].CardId);
        await Card(window, "claude-extra").MoveUpAsync();
        Assert.Equal("claude-extra", window.Cards[0].CardId);
    }

    [Fact]
    public void SettingsListCapsWithTheirStatus()
    {
        var (window, _, _, _) = Start();
        var caps = window.Settings.Caps;
        Assert.Equal(["Claude Pro · extra usage", "Codex Pro · credits", "Copilot Business · premium", "Claude Team · extra usage"], caps.Select(c => c.Label));
        Assert.Equal("provider limit $500.00 · your cap binds", caps[0].Note);
        Assert.Equal("provider sends a balance only · used is tracked since 3 Oct (estimate)", caps[1].Note);
        Assert.Equal("unmatched · this limit is no longer reported · kept, not applied", caps[2].Note);
        Assert.Equal("Remove", caps[2].ActionText);
        Assert.Equal("currency mismatch · provider reports USD · cap kept, not applied", caps[3].Note);
        Assert.Equal("€250.00", caps[3].AmountText);
        Assert.False(caps[3].CanAct);
        Assert.Equal("1 sync failed · Antigravity", window.Settings.SystemStatusText);
        Assert.True(window.Settings.HasSystemStatus);
        Assert.Equal(5, window.Settings.RefreshMinutes);
    }

    [Fact]
    public void SettingsFooterShowsNoStatusWhileAllSynced()
    {
        var (window, source, _, _) = Start();
        var synced = source.Current with { Summaries = source.Current.Summaries with { FailedSyncs = 0, FailedProviders = [] } };
        window.Settings.Rebuild(synced, source.Preferences);
        Assert.Equal(string.Empty, window.Settings.SystemStatusText);
        Assert.False(window.Settings.HasSystemStatus);
    }

    [Fact]
    public async Task UnmatchedCapIsRemovedFromSettings()
    {
        var (window, _, _, _) = Start();
        await window.Settings.Caps.Single(c => c.Model.Status == CapStatus.Unmatched).ActAsync();
        Assert.DoesNotContain(window.Settings.Caps, c => c.Model.Status == CapStatus.Unmatched);
    }

    [Fact]
    public void TrayRowOpensTheWindowAtThatAccount()
    {
        var (window, _, _, _) = Start();
        string? focused = null;
        window.FocusCardRequested += (_, id) => focused = id;
        window.FocusAccount("acct-copilot");
        Assert.Equal("copilot-completions", focused);
    }

    [Fact]
    public void DemoScenariosSwitchAndResetViewState()
    {
        var (window, _, _, _) = Start();
        Assert.Equal(9, window.Demo!.Scenarios.Count);
        window.ToggleSettings();
        window.LoadScenario(DemoLedgerScenarios.States);
        Assert.Equal(54, window.Cards.Count); // H9 is signed out and hidden until Show signed-out accounts is on.
        Assert.All(window.Cards, c => Assert.False(c.IsNew));
        window.LoadScenario(DemoLedgerScenarios.FirstRun);
        Assert.True(window.IsFirstRun);
    }

    [Fact]
    public void AccessibleNamesCarryStateWords()
    {
        var (window, _, _, _) = Start();
        Assert.StartsWith("Claude Pro, 5 hour and 7 day, OK. Today 2 five-hour windows, 72 percent used, 28 percent allowed today in the current window.",
            Card(window, "claude-week").Visual.AccessibleName, StringComparison.Ordinal);
        Assert.Contains("Resets Mon 19 Oct 09:00", Card(window, "claude-week").Visual.AccessibleName, StringComparison.Ordinal);
        Assert.StartsWith("Claude Pro extra usage, over today.", Card(window, "claude-extra").Visual.AccessibleName, StringComparison.Ordinal);
        Assert.Equal("Sign out Claude Pro", Card(window, "claude-week").SignOutName);
        Assert.Equal("History, Claude Pro 7 day", Card(window, "claude-week").HistoryName);
    }
}

/// <summary>D-199: the limit settings popover.</summary>
public sealed class LimitSettingsTests
{
    private static readonly DateTimeOffset TrackedFrom = new(2026, 10, 14, 10, 43, 0, TimeSpan.FromHours(1));
    private static readonly AccountModel Business = new("acct-business", ProviderKind.Copilot, "Copilot Business", AccountHealth.Ok, null, null, null, []);

    private static LimitCardModel Credits(bool manual = false) =>
        DemoLedgerScenarios.Requests("premium", "Premium requests", CardState.OnTrack, 3120, 3240, 3600, 17500, 500, 14260) with
        { Scale = ScaleModel.Count("credits"), Units = new(false, 0.01m), TodayUse = new(120, TrackedFrom, manual) };

    private static LimitSettingsViewModel Settings(LimitCardModel card, Func<decimal?, Task<bool>>? setToday = null,
        Func<bool, decimal, Task<bool>>? setUnits = null, Action? rebuild = null) =>
        new(card, _ => Task.FromResult(true), setToday ?? (_ => Task.FromResult(true)), setUnits ?? ((_, _) => Task.FromResult(true)), rebuild ?? (() => { }), () => { });

    [Fact]
    public void SettingsAppearOnlyForLimitsWithASetting()
    {
        var scheduler = new ManualScheduler();
        var source = new DemoLedgerSource(scheduler);
        var window = new LedgerViewModel(source, scheduler, _ => { }, null);
        Assert.True(window.Cards.Single(c => c.CardId == "claude-week").HasSettings);
        Assert.True(window.Cards.Single(c => c.CardId == "codex-credits").HasSettings);
    }

    [Fact]
    public async Task WeeklyPercentWindowTakesACapInItsPopover()
    {
        var scheduler = new ManualScheduler();
        var source = new DemoLedgerSource(scheduler);
        var window = new LedgerViewModel(source, scheduler, _ => { }, null);
        LimitCardViewModel Week() => window.Cards.Single(c => c.CardId == "claude-week");
        Week().OpenSettings();
        var cap = Week().Settings!.Cap!;
        Assert.Null(Week().Settings!.Today);
        Assert.Equal("%", cap.UnitText);
        Assert.EndsWith(" · at most 100 %", cap.Hint, StringComparison.Ordinal);
        Assert.False(cap.Accepts("101"));
        Assert.False(cap.Accepts("90.5"));
        Assert.True(cap.Accepts("90"));
        cap.Text = "90";
        await cap.SaveAsync();
        Assert.Null(cap.Error);
        Assert.Equal(90, Week().Model.Cap!.Amount);
        Assert.StartsWith("47 % of 90 % cap", Week().Visual.Footer, StringComparison.Ordinal);
        Assert.Null(Week().Visual.CapTick);
    }

    [Fact]
    public async Task TodayEditorUsesThePeriodUseAsItsLimit()
    {
        decimal? saved = null;
        var settings = Settings(Credits(), amount => { saved = amount; return Task.FromResult(true); });
        Assert.Equal("today", settings.Today!.Label);
        Assert.Equal("120", settings.Today.Text);
        settings.Today.Text = "3241";
        await settings.Today.SaveAsync();
        Assert.Equal("Enter at most 3,240 · this period’s use", settings.Today.Error);
        settings.Today.Text = string.Empty;
        await settings.Today.SaveAsync();
        Assert.Equal("Enter today’s use", settings.Today.Error);
        settings.Today.Text = "900";
        await settings.Today.SaveAsync();
        Assert.Equal(900, saved);
    }

    [Fact]
    public void ResetAppearsOnlyForAManualValue()
    {
        var tracked = Settings(Credits()).Today!;
        Assert.False(tracked.HasCap);
        Assert.DoesNotContain("set by you", tracked.Hint);
        Assert.Contains("tracked 120 since 10:43", tracked.Hint);
        var manual = Settings(Credits(manual: true)).Today!;
        Assert.True(manual.HasCap);
        Assert.Equal("Reset", manual.RemoveText);
        Assert.Contains("set by you", manual.Hint);
    }

    [Fact]
    public async Task RateInputAcceptsOnlyValidRates()
    {
        (bool Usd, decimal Rate)? applied = null;
        var rebuilt = 0;
        var settings = Settings(Credits(), setUnits: (usd, rate) => { applied = (usd, rate); return Task.FromResult(true); }, rebuild: () => rebuilt++);
        Assert.True(settings.HasUnits);
        Assert.False(settings.IsUsd);
        Assert.Equal("0.01", settings.RateText);
        Assert.True(settings.AcceptsRate("0.0123"));
        Assert.False(settings.AcceptsRate("1.1234567"));
        Assert.False(settings.AcceptsRate("-1"));
        settings.RateText = "0";
        await settings.SaveRateCommand.ExecuteAsync(null);
        Assert.Equal("Enter a rate above 0, at most 1000, with up to 6 decimals", settings.RateError);
        Assert.Null(applied);
        await settings.ShowUsdCommand.ExecuteAsync(null);
        Assert.Null(applied);
        settings.RateText = "0.01";
        await settings.ShowUsdCommand.ExecuteAsync(null);
        Assert.Equal((true, 0.01m), applied);
        Assert.Equal(1, rebuilt);
        settings.RateText = "0.04";
        await settings.SaveRateCommand.ExecuteAsync(null);
        Assert.Equal((false, 0.04m), applied);
        Assert.Null(settings.RateError);
    }

    [Fact]
    public void CapRowShowsTheLargestCapInTheShownUnit()
    {
        Assert.Contains("at most 17,500", Settings(Credits()).Cap!.Hint);
        var dollars = Settings(CreditDollars.Apply(Credits(), new(true, 0.01m)));
        Assert.Equal(ScaleKind.Money, dollars.Cap!.Scale.Kind);
        Assert.Contains("at most $175.00", dollars.Cap.Hint);
        Assert.Contains("tracked $1.20", dollars.Today!.Hint);
        Assert.False(Settings(Credits() with { Units = null }).HasUnits);
    }

    [Fact]
    public void TodayTipNamesAManualOrLateTrackedDay()
    {
        var now = TrackedFrom.AddHours(2);
        Assert.Contains("Set by you", CardVisuals.Build(Credits(manual: true), Business, ValueMode.Used, now).Cells[0].Tip);
        var tracked = CardVisuals.Build(Credits(), Business, ValueMode.Used, now).Cells[0].Tip;
        Assert.Contains("Tracked since 10:43", tracked);
        Assert.DoesNotContain("Set by you", tracked);
    }

    [Fact]
    public async Task DemoPopoverSwitchesUnitsAndSetsToday()
    {
        var scheduler = new ManualScheduler();
        var source = new DemoLedgerSource(scheduler);
        source.LoadScenario(DemoLedgerScenarios.WorkBudget);
        var window = new LedgerViewModel(source, scheduler, _ => { }, null);
        var card = window.Cards.Single(c => c.CardId == "copilot-business-premium");
        Assert.Equal("Copilot Business", card.Name);
        Assert.Equal(ScaleModel.Count("credits"), card.Model.Scale);
        Assert.Equal((3240m, 17500m), (card.Model.Figures.Used!.Value, card.Model.Figures.ProviderLimit.Amount!.Value));
        Assert.Equal(new TodayUseModel(120, new DateTimeOffset(2026, 10, 14, 10, 43, 0, TimeSpan.FromHours(1)), false), card.Model.TodayUse);

        // SwitchingUnitsRebuildsThePopoverInTheNewUnit
        card.OpenSettings();
        await card.Settings!.ShowUsdCommand.ExecuteAsync(null);
        Assert.Equal(32.40m, card.Model.Figures.Used);
        Assert.True(card.Settings!.IsUsd);
        Assert.Equal(ScaleKind.Money, card.Settings.Cap!.Scale.Kind);
        Assert.Contains("at most $175.00", card.Settings.Cap.Hint);

        card.Settings.Today!.Text = "9.00";
        await card.Settings.Today.SaveAsync();
        Assert.Equal(9.00m, card.Model.Figures.Used - card.Model.Figures.DayStart);
        Assert.True(card.Model.TodayUse!.Manual);
        card.OpenSettings();
        card.Settings!.Cap!.Text = "100.00";
        await card.Settings.Cap.SaveAsync();
        Assert.Equal(100.00m, card.Model.Cap!.Amount);
        card.OpenSettings();
        await card.Settings!.ShowCreditsCommand.ExecuteAsync(null);
        Assert.Equal(10000m, card.Model.Cap!.Amount);
        Assert.Equal(900m, card.Model.Figures.Used - card.Model.Figures.DayStart);
        card.OpenSettings();
        await card.Settings!.Today!.RemoveAsync();
        Assert.Equal(120m, card.Model.Figures.Used - card.Model.Figures.DayStart);
        Assert.False(card.Model.TodayUse!.Manual);

        var spending = window.Cards.Single(c => c.CardId == "work-month");
        spending.OpenSettings();
        Assert.False(spending.Settings!.HasUnits);
        Assert.NotNull(spending.Settings.Today);
        Assert.NotNull(spending.Settings.Cap);
    }

    [Fact]
    public async Task UndoRestoresARemovedCapInTheUnitShownNow()
    {
        var scheduler = new ManualScheduler();
        var source = new DemoLedgerSource(scheduler);
        source.LoadScenario(DemoLedgerScenarios.WorkBudget);
        var window = new LedgerViewModel(source, scheduler, _ => { }, null);
        var card = window.Cards.Single(c => c.CardId == "copilot-business-premium");
        card.OpenSettings();
        await card.Settings!.ShowUsdCommand.ExecuteAsync(null);
        card.Settings!.Cap!.Text = "100.00";
        await card.Settings.Cap.SaveAsync();
        card.OpenSettings();
        await card.Settings!.Cap!.RemoveAsync();
        Assert.Null(card.Model.Cap);
        Assert.True(window.HasUndo);
        card.OpenSettings();
        await card.Settings!.ShowCreditsCommand.ExecuteAsync(null);
        await window.UndoAsync();
        Assert.Equal(10000m, card.Model.Cap!.Amount);
        scheduler.Run(TimeSpan.FromSeconds(10));
    }
}
