using AiUsage.Features.Ledger.Contract;

namespace AiUsage.Features.Ledger;

internal enum Tone { Ok, Attention, Critical, Neutral }

/// <summary>A fill from the Ledger tokens. With Stripe set, the fill is a 135° hatch of Fill and Stripe, 3 + 3 px.</summary>
internal sealed record Paint(string Fill, string? Stripe = null)
{
    public const string Transparent = "Transparent";
}

internal sealed record StripPart(double Weight, Paint Paint, bool OverEdge);

/// <summary>One today cell: a 5h window, or the whole today strip. Weight is its flex share of the strip width.</summary>
internal sealed record StripCell(double Weight, IReadOnlyList<StripPart> Parts, bool ShowLabel, bool Dashed, IReadOnlyList<string> Tip);

/// <summary>A period bar segment; Left and Width are percent of the bar, already mirrored for Left mode.</summary>
internal sealed record BarSegment(double Left, double Width, Paint Paint);

internal sealed record BarRing(double Left, double Width, Tone Tone);

internal sealed record MarkVisual(MarkKind Kind, string Text, bool Neutral, IReadOnlyList<string> Tip);

internal sealed record NoteLine(string Key, string Value, string Right);

/// <summary>Everything a card draws and says in one value mode (spec 6.2, 6.3); built from the contract only.</summary>
internal sealed record CardVisual(
    Tone Tone,
    string? Pill,
    IReadOnlyList<string> PillTip,
    IReadOnlyList<MarkVisual> Marks,
    bool HasStrip,
    IReadOnlyList<StripCell> Cells,
    string? TodayNote,
    string? OverLabel,
    bool HasPeriodBar,
    string PeriodLabel,
    IReadOnlyList<BarSegment> Segments,
    BarRing? Ring,
    double? CapTick,
    double? OverTick,
    IReadOnlyList<double> Dividers,
    IReadOnlyList<string> BarTip,
    double Opacity,
    string Footer,
    IReadOnlyList<string> FooterTip,
    bool FooterIsEstimate,
    string ResetText,
    IReadOnlyList<string> ResetTip,
    IReadOnlyList<NoteLine> NoteLines,
    string? ActionText,
    string AccessibleName)
{
    // x:Bind skips function calls with null arguments; bind visibility to non-null values.
    public bool HasPill => !string.IsNullOrEmpty(Pill);
    public bool HasAction => !string.IsNullOrEmpty(ActionText);
    public bool HasOverLabel => !string.IsNullOrEmpty(OverLabel);
    public bool HasTodayNote => !string.IsNullOrEmpty(TodayNote);
}

/// <summary>
/// Ports the accepted reference's drawing rules (Provider States and Surfaces handoff pages, D-186, D-187) to plain numbers.
/// Inputs are contract values; outputs are geometry, token keys and words. No budget figure is derived here.
/// </summary>
internal static class CardVisuals
{
    private enum Part { Used, Allow, Gray, UsedCrit, Over, CritRest, Track }

    private const double Epsilon = 0.01;

    public static CardVisual Build(LimitCardModel card, AccountModel account, ValueMode mode, DateTimeOffset now)
    {
        var left = mode == ValueMode.Left;
        var tone = ToneOf(card);
        var (pill, pillTip) = PillOf(card, account, now);
        var marks = card.Marks.Select(mark => MarkOf(mark, card, account, now)).ToArray();
        var period = LedgerFormat.PeriodLabel(card.Period);
        var (resetText, resetTip) = ResetOf(card, now);
        var action = card.Action switch { CardAction.SignIn => "Sign in", CardAction.SetCap => "Set cap", _ => null };
        var opacity = card.Freshness.IsStale ? 0.7 : 1;

        if (card.Layout == CardLayout.Note)
        {
            var lines = NoteLinesOf(card, period, resetText);
            var noteName = Name(card, account, StateWords(card, account, pill, marks, now), string.Join(", ", lines.Select(l => l.Value)), (resetTip.Count > 0 ? resetTip[0] : null));
            return new CardVisual(tone, pill, pillTip, marks, false, [], null, null, false, period, [], null, null, null, [], [], opacity,
                string.Empty, [], false, resetText, resetTip, lines, action, noteName);
        }

        // Keep display arithmetic decimal. Only drawing coordinates and weights need doubles.
        var displayUsed = card.Figures.Used ?? 0;
        var displayMax = BarMax(card);
        var used = (double)displayUsed;
        var max = (double)Math.Max(displayMax, displayUsed);
        double Pc(double v) => max <= 0 ? 0 : v / max * 100;
        string Fm(decimal v) => LedgerFormat.Value(card.Scale, v);

        if (card.Layout == CardLayout.UsedOnly)
        {
            var u = Pc(used);
            var neutral = Keys(Tone.Neutral);
            var segs = new List<BarSegment> { Seg(0, u, PaintOf(Part.Used, neutral, left, false), left) };
            if (left)
                segs.Add(Seg(u, 100 - u, PaintOf(Part.Allow, neutral, left, false), left));
            var footer = left ? Fm(displayMax - displayUsed) + " left" : Fm(displayUsed) + " used";
            var footerTip = card.State == CardState.NotReady
                ? new[] { "Old period reading", "Not used for today’s budget" }
                : ["Window length unknown", "Used and reset come from the provider"];
            var note = card.State switch { CardState.PeriodUnknown => "no daily budget", CardState.NotReady => "budget not ready", _ => null };
            var barLabel = card.Period.Kind == PeriodKind.Unknown ? "Window" : period;
            var usedName = Name(card, account, StateWords(card, account, pill, marks, now), period + " " + footer + (note is null ? string.Empty : ", " + note), (resetTip.Count > 0 ? resetTip[0] : null));
            return new CardVisual(tone, pill, pillTip, marks, false, [], note, null, true, period, segs, null, null, null, [],
                [barLabel, footer], opacity, footer, footerTip, false, resetText, resetTip, [], action, usedName);
        }

        var usedUp = card.State == CardState.UsedUp;
        var off = card.State == CardState.DayOff;
        var rush = card.State == CardState.Rush;
        var displayStart = card.Figures.DayStart ?? displayUsed;
        var displayEnd = card.Figures.TodayEnd ?? displayUsed;
        var u0 = (double)displayStart;
        var t = (double)displayEnd;
        var cap = AppliedCap(card);
        var keys = Keys(tone);
        var fiveHour = card.Layout == CardLayout.FiveHourAndPeriod && card.FiveHour?.WindowShare is not null;

        var cells = usedUp ? [] : fiveHour
            ? FiveHourCells(card, keys, left, off, rush, used, u0, t)
            : card is { Layout: CardLayout.FiveHourAndPeriod, FiveHour: { } one }
                ? [OneWindowCell(one, keys, left, off)]
                : [TodayCell(card, keys, left, off, rush, displayUsed, displayStart, displayEnd, period, Fm)];
        if (left)
            cells = [.. Enumerable.Reverse(cells)];

        // Period bar.
        var up = Pc(used);
        var u0p = Pc(u0);
        var tp = Pc(t);
        var hi = Math.Max(up, tp);
        var lo = Math.Min(up, tp);
        var capP = cap is { } c ? Pc((double)c) : 100;
        var bar = new List<BarSegment>();
        if (left)
            bar.Add(Seg(hi, Math.Max(0, capP - hi), new Paint("Prev"), left));
        else
            bar.Add(Seg(0, u0p, new Paint("Prev"), left));
        if (capP < 100)
            bar.Add(Seg(Math.Max(capP, hi), 100 - Math.Max(capP, hi), new Paint("CardTop", "Rail"), left));
        bar.Add(Seg(u0p, lo - u0p, PaintOf(used > t && !off ? Part.UsedCrit : Part.Used, keys, left, off), left));
        if (tp > up)
            bar.Add(Seg(up, tp - up, PaintOf(Part.Allow, keys, left, off), left));
        if (up > tp)
            bar.Add(Seg(tp, up - tp, PaintOf(Part.Over, keys, left, off), left));
        var dividers = new List<double>();
        if (fiveHour)
        {
            var ws = (double)card.FiveHour!.WindowShare!.Value;
            var start = card.FiveHour.CurrentWindowStarted ? used - (double)card.FiveHour.CurrentWindowUsed * ws / 100 : used;
            for (var k = start + ws; k < 99.5 && ws > 0; k += ws)
                if (k > used + 0.5)
                    dividers.Add(left ? 100 - k : k);
        }
        if (usedUp)
        {
            bar = [Seg(0, 100, left ? new Paint("CritP", "CritD") : new Paint("CritM"), left)];
            dividers.Clear();
        }
        var ring = usedUp ? null : Ring(u0p, hi - u0p, tone, left);
        var overTick = used > t && !usedUp ? (double?)(left ? 100 - tp : tp) : null;
        double? capTick = capP < 100 ? (left ? 100 - capP : capP) : null;

        // Over label.
        string? over = null;
        if (displayUsed > displayEnd && !off && !usedUp)
        {
            if (card.Scale.Kind == ScaleKind.Percent)
            {
                var p = (double)LedgerFormat.Round((decimal)((used - u0) / Math.Max(Epsilon, t - u0) * 100));
                over = left ? LedgerFormat.Minus + (p - 100).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " %" : p.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " %";
            }
            else
                over = (left ? LedgerFormat.Minus : "+") + Fm(displayUsed - displayEnd);
        }

        var (footerText, footerTipLines, estimate) = FooterOf(card, left, displayUsed, displayMax, cap, rush, Fm, now);
        var top = cap ?? displayMax;
        var todayLine = displayUsed > displayEnd ? "today " + Fm(displayUsed - displayEnd) + " over" : left ? "today " + Fm(displayEnd - displayUsed) + " left" : "today " + Fm(displayUsed - displayStart) + " of " + Fm(displayEnd - displayStart);
        var barTip = new List<string>
        {
            BarName(card, period),
            (left ? Fm(top - displayUsed) + " of " + Fm(top) + " left" : Fm(displayUsed) + " of " + Fm(top) + " used") + " · " + todayLine,
        };
        if (capP < 100 && cap is { } capAmount)
            barTip.Add(displayUsed > capAmount ? "Past the tick: over custom cap " + Fm(capAmount) : "Hatched: above custom cap " + Fm(capAmount));

        var today = cells.Count == 0 ? string.Empty : TodaySummary(card, cells, left);
        var name = Name(card, account, StateWords(card, account, pill, marks, now), today + " " + LedgerFormat.PeriodWords(card.Period) + " " + footerText + ".", (resetTip.Count > 0 ? resetTip[0] : null));
        return new CardVisual(tone, pill, pillTip, marks, !usedUp, cells, null, over, !rush || usedUp, period, bar, ring, capTick, overTick, dividers,
            barTip, opacity, footerText, footerTipLines, estimate, resetText, resetTip, [], action, name);
    }

    public static Tone ToneOf(LimitCardModel card)
    {
        if (card.Freshness.IsStale)
            return Tone.Neutral;
        return card.State switch
        {
            CardState.OnTrack or CardState.Rush => Tone.Ok,
            CardState.TodayLow or CardState.TodayShort or CardState.CapClose or CardState.FiveHourFull => Tone.Attention,
            CardState.TodayUsed or CardState.OverToday or CardState.CapReached or CardState.OverCap or CardState.UsedUp => Tone.Critical,
            _ => Tone.Neutral,
        };
    }

    /// <summary>States whose pill names a reason for having no budget; a stale reading keeps them instead of an age pill.</summary>
    private static bool IsReasonState(CardState state) => state is CardState.NotReady or CardState.PeriodUnknown or CardState.ValueUnknown
        or CardState.NotIncluded or CardState.LimitUnknown or CardState.NoCap or CardState.SignedOut;

    public static string? PillText(CardState state, PeriodModel period) => state switch
    {
        CardState.OnTrack => null,
        CardState.TodayLow => "today low",
        CardState.TodayShort => "today short",
        CardState.CapClose => "cap close",
        CardState.FiveHourFull => "5h full",
        CardState.TodayUsed => "today used",
        CardState.OverToday => "over today",
        CardState.CapReached => "cap reached",
        CardState.OverCap => "over cap",
        CardState.UsedUp => LedgerFormat.PeriodLabel(period) + " used up",
        CardState.DayOff => "day off",
        CardState.Rush => "rush",
        CardState.NotReady => "not ready",
        CardState.ValueUnknown => "unknown",
        CardState.PeriodUnknown => "period unknown",
        CardState.NotIncluded => "not included",
        CardState.LimitUnknown => "limit unknown",
        CardState.NoCap => "no cap",
        CardState.SignedOut => "signed out",
        _ => null,
    };

    private static (string? Pill, IReadOnlyList<string> Tip) PillOf(LimitCardModel card, AccountModel account, DateTimeOffset now)
    {
        var stale = card.Freshness is { IsStale: true, ReadingAt: { } reading };
        if (stale && !IsReasonState(card.State))
        {
            var readingAt = card.Freshness.ReadingAt!.Value;
            if (account.Health == AccountHealth.SyncFailedStale)
            {
                var failed = account.LastSyncFailedAt is { } f ? "Sync failed " + LedgerFormat.Clock(f) + " · showing the reading from " + LedgerFormat.Clock(readingAt) : "Sync failed · showing the reading from " + LedgerFormat.Clock(readingAt);
                var second = "Reading " + LedgerFormat.Age(readingAt, now).Replace(" old", " old", StringComparison.Ordinal) + Retry(account, now);
                return ("sync failed · " + LedgerFormat.Clock(readingAt), [failed, second]);
            }
            if (account.Health == AccountHealth.SignInExpired)
                return (LedgerFormat.Age(readingAt, now), ["Last reading " + LedgerFormat.Clock(readingAt), "Sign in again to refresh"]);
            return (LedgerFormat.Age(readingAt, now), ["Last reading " + LedgerFormat.Clock(readingAt), "Not renewed for over 15 min" + Retry(account, now)]);
        }
        var pill = PillText(card.State, card.Period);
        var tip = PillTip(card, now);
        if (card.Monetary is { } money)
        {
            if (money.Enabled == false) { pill = "disabled"; tip = ["Spending disabled", "Retained readings and caps kept"]; }
            else if (money.BudgetUnavailable is { } reason) { pill = "no budget"; tip = [reason]; }
            else if (card.State == CardState.NotIncluded) { pill = "zero limit"; tip = ["Provider reports a zero spending limit"]; }
            // Scope and period caveats stay on hover instead of as card text.
            tip = [.. tip, money.Qualification];
        }
        if (stale && card.Freshness.ReadingAt is { } at && tip.Count > 0)
            tip = [tip[0] + " · as of " + LedgerFormat.Clock(at), .. tip.Skip(1)];
        return (pill, tip);
    }

    private static string Retry(AccountModel account, DateTimeOffset now) =>
        account.NextRetryAt is { } next && next > now ? " · retry " + LedgerFormat.Relative(next, now) : string.Empty;

    private static IReadOnlyList<string> PillTip(LimitCardModel card, DateTimeOffset now)
    {
        var f = card.Figures;
        string Fm(decimal? v) => v is { } value ? LedgerFormat.Value(card.Scale, value) : "unknown";
        var period = LedgerFormat.PeriodLabel(card.Period);
        var share = f.TodayEnd - f.DayStart;
        switch (card.State)
        {
            case CardState.OnTrack: return ["On track", "Today’s allowance is not used up"];
            case CardState.TodayLow: return ["Today low", "Little left today · not used up yet"];
            case CardState.TodayShort:
                return ["Today short", f.UsualShare is { } usual ? period + " limit leaves " + Fm(share).Replace(" %", string.Empty, StringComparison.Ordinal) + " of usual " + Fm(usual) : period + " limit cuts today’s share"];
            case CardState.CapClose:
                return ["Cap close", f.UsualShare is { } usualCap ? "Cap leaves " + Fm(share) + " of usual " + Fm(usualCap) + " today" : "The cap cuts today’s share"];
            case CardState.FiveHourFull:
                return ["5h window full", card.FiveHour?.CurrentWindowEndsAt is { } end ? "Next window opens " + LedgerFormat.Clock(end) : "Waiting for the next window"];
            case CardState.TodayUsed: return ["Today used", "Today’s allowance is used up · not over"];
            case CardState.OverToday: return ["Over today", "Today’s allowance is exceeded"];
            case CardState.CapReached:
                return ["Custom cap reached", Beyond(card, Fm) + " · raise cap in settings"];
            case CardState.OverCap:
                return ["Over custom cap", Fm(f.Used - card.Cap?.Amount) + " over " + Fm(card.Cap?.Amount) + " · " + Beyond(card, Fm).ToLowerInvariant()];
            case CardState.UsedUp:
                return [period + " limit used up", card.Reset?.At is { } back ? "Back " + LedgerFormat.DayDate(back) + " " + LedgerFormat.Clock(back) + " · " + LedgerFormat.Relative(back, now) : "Nothing left until the reset"];
            case CardState.DayOff:
                var used = f.Used - f.DayStart;
                var first = "Day off · " + Fm(used) + " used today";
                if (card.DayOff is { } preview)
                    return [first, LedgerFormat.WeekdayName(preview.NextWorkDay) + "’s share " + PreviewValue(card.Scale, preview.ShareBefore) + " → " + PreviewValue(card.Scale, preview.ShareAfter)];
                return [first, "No colours on a day off · Work today colours it"];
            case CardState.Rush:
                var leftText = Fm(f.EffectiveLimit - f.Used) + " left";
                var second = card.FiveHour?.FitBeforeReset is { } fit && card.Reset?.At is { } at && Unused(card) is { } unused && unused > 0.5m
                    ? "Only " + fit + " × 5h fit before " + LedgerFormat.Clock(at) + " · ≈ " + LedgerFormat.Value(card.Scale, unused) + " resets unused"
                    : ResetLong(card.Reset) + " · use it today";
                return ["Rush · " + leftText, second];
            case CardState.NotReady: return ["Budget not ready", "Waiting for the first reading after reset"];
            case CardState.ValueUnknown: return ["Value unknown", "Provider did not send used"];
            case CardState.PeriodUnknown: return ["Period unknown", "Window length not sent · no daily budget"];
            case CardState.NotIncluded: return ["Not included", "This plan has no " + (card.ScopeLabel ?? "such") + " " + LedgerFormat.Unit(card.Scale)];
            case CardState.LimitUnknown: return ["Limit unknown", "Provider sends no limit · set a cap for a daily budget"];
            case CardState.NoCap: return ["No cap", "Set a cap for a daily budget"];
            case CardState.SignedOut: return ["Signed out", "History, name and caps kept"];
            default: return [];
        }
    }

    private static string PreviewValue(ScaleModel scale, decimal value) => scale.Kind == ScaleKind.Percent ? LedgerFormat.Percent1(value) : LedgerFormat.Value(scale, value);

    /// <summary>For a rush with a fit limit: what the windows that fit cannot use before the reset.</summary>
    private static decimal? Unused(LimitCardModel card)
    {
        if (card.FiveHour is not { WindowShare: { } ws, FitBeforeReset: { } fit } five || card.Figures.Used is not { } used || card.Figures.EffectiveLimit is not { } limit)
            return null;
        var current = five.CurrentWindowStarted ? (100 - five.CurrentWindowUsed) * ws / 100 : ws;
        var usable = current + (fit - 1) * ws;
        return Math.Max(0, limit - used - usable);
    }

    private static string Beyond(LimitCardModel card, Func<decimal?, string> fm)
    {
        var f = card.Figures;
        if (f.ProviderBalance is { } balance)
            return "Provider balance " + fm(balance);
        if (f.ProviderLimit is { Kind: LimitValueKind.Known, Amount: { } limit } && f.Used is { } used)
            return "Provider limit has " + fm(limit - used) + " more";
        if (f.ProviderLimit.Kind == LimitValueKind.Unlimited)
            return "Provider: unlimited";
        return "Provider limit unknown";
    }

    private static MarkVisual MarkOf(CardMark mark, LimitCardModel card, AccountModel account, DateTimeOffset now) => mark.Kind switch
    {
        MarkKind.OnExtraUsage => new MarkVisual(mark.Kind, "on extra usage", false,
        [
            (mark.Until is { } until ? "Window full until " + LedgerFormat.Clock(until) : "Window full") + " · continuing on extra usage",
            (mark.Amount is { } spend ? "+" + LedgerFormat.Money(mark.Currency, mark.Exponent, spend) : "Spend rose") + (mark.Since is { } since ? " since " + LedgerFormat.Clock(since) : string.Empty),
        ]),
        MarkKind.ExtraDay => new MarkVisual(mark.Kind, "extra day", true, ["Extra work day", "Work today is on until midnight · settings unchanged"]),
        MarkKind.SyncFailed => new MarkVisual(mark.Kind, "sync failed", false,
        [
            account.LastSyncFailedAt is { } failed ? "Last sync failed " + LedgerFormat.Clock(failed) : "Last sync failed",
            (card.Freshness.ReadingAt is { } reading ? "Reading from " + LedgerFormat.Clock(reading) : "Reading still fresh") + Retry(account, now),
        ]),
        MarkKind.SignInExpired => new MarkVisual(mark.Kind, "sign-in expired", false, ["Sign-in expired", "Sign in again to refresh"]),
        MarkKind.PastReset when mark.ScopeLabel is { } scope => new MarkVisual(mark.Kind, scope + " past reset", false,
            [scope + " reading is from before the reset" + (mark.Since is { } reset ? " at " + LedgerFormat.Clock(reset) : string.Empty),
             "Waiting for the first reading of the new " + scope + " window"]),
        _ => new MarkVisual(mark.Kind, "past reset", false, ["Reading is from before the reset", "Waiting for the first reading of the new period"]),
    };

    private static (string Text, IReadOnlyList<string> Tip) ResetOf(LimitCardModel card, DateTimeOffset now)
    {
        if (card.Reset is not { } reset)
            return (string.Empty, []);
        if (reset.Provenance == ResetProvenance.Assumed)
        {
            var end = reset.At is { } a ? LedgerFormat.DayMonth(DateOnly.FromDateTime(a.DateTime)) : reset.Date is { } d ? LedgerFormat.DayMonth(d) : "month end";
            var exact = reset.At is { } at ? "Assumed reset " + LedgerFormat.DayDate(at) + " " + LedgerFormat.Clock(at) : "Assumed reset " + end;
            return ("resets " + end + " (assumed)", [exact, "Calendar month · provider sends no period"]);
        }
        if (reset.At is { } time)
        {
            var past = time <= now;
            var verb = card.State == CardState.UsedUp ? "back " : past ? "reset " : "resets ";
            var text = verb + (past ? LedgerFormat.Clock(time) : LedgerFormat.When(time, now));
            return (text, [(past ? "Reset " : "Resets ") + LedgerFormat.DayDate(time) + " " + LedgerFormat.Clock(time), LedgerFormat.Relative(time, now)]);
        }
        if (reset.Date is { } date)
        {
            var tip = new List<string> { "Resets " + LedgerFormat.DayMonth(date) + " (date from provider)" };
            if (card.Period.StartAssumed && reset.AssumedStart is { } start)
                tip.Add("Start assumed " + LedgerFormat.DayMonth(start));
            return ((card.State == CardState.UsedUp ? "back " : "resets ") + LedgerFormat.DayMonth(date), tip);
        }
        return (string.Empty, []);
    }

    private static string ResetLong(ResetModel? reset) => reset switch
    {
        { At: { } at } => "Resets " + LedgerFormat.DayDate(at) + " " + LedgerFormat.Clock(at),
        { Date: { } date } => "Resets " + LedgerFormat.DayMonth(date),
        _ => "Until the reset",
    };

    private static string ResetPlain(ResetModel? reset) => ResetLong(reset).Replace("Resets ", string.Empty, StringComparison.Ordinal);

    private static IReadOnlyList<NoteLine> NoteLinesOf(LimitCardModel card, string period, string reset)
    {
        if (card.Monetary is { } money)
        {
            var lines = new List<NoteLine>
            {
                new("used", LedgerFormat.NativeMoney(money.Used), string.Empty),
                new("limit", money.LimitKind == LimitValueKind.Unlimited ? "unlimited (provider)" : LedgerFormat.NativeMoney(money.ProviderLimit) + " (provider)", string.Empty)
            };
            if (money.PersonalCap is not null)
                lines.Add(new("cap", LedgerFormat.NativeMoney(money.PersonalCap) + (card.Cap?.Status == CapStatus.Applied ? " (personal)" : " (personal, kept; not applied)"), string.Empty));
            return lines;
        }
        var f = card.Figures;
        var unit = LedgerFormat.Unit(card.Scale);
        return card.State switch
        {
            CardState.NotIncluded => [new NoteLine(period, "not included in plan", reset)],
            CardState.LimitUnknown => [new NoteLine(period, (f.Used is { } u ? LedgerFormat.Value(card.Scale, u) + " " + unit + " used" : "used unknown"), reset)],
            CardState.ValueUnknown => [new NoteLine(period, "used unknown", reset)],
            CardState.SignedOut => [new NoteLine(string.Empty, "no current figures", "history kept")],
            CardState.NoDisplayedLimits => [new NoteLine(string.Empty, "No subscription limits to display", string.Empty)],
            CardState.NoCap => [new NoteLine(string.Empty, f.ProviderBalance is { } b ? "balance " + LedgerFormat.Value(card.Scale, b) + " " + unit + " (provider)" : "no cap set", "no budget")],
            _ => [new NoteLine(period, f.Used is { } used ? LedgerFormat.Value(card.Scale, used) + " used" : "used unknown", reset)],
        };
    }

    private static decimal BarMax(LimitCardModel card)
    {
        var f = card.Figures;
        var used = f.Used ?? 0;
        if (card.Scale.Kind == ScaleKind.Percent)
            return f.EffectiveLimit ?? 100;
        var baseMax = f.ProviderLimit is { Kind: LimitValueKind.Known, Amount: { } limit } ? limit : card.Cap?.Amount ?? f.EffectiveLimit ?? used;
        return baseMax;
    }

    private static decimal? AppliedCap(LimitCardModel card) => card.Cap is { Status: CapStatus.Applied } cap ? cap.Amount : null;

    private static (string M, string P) Keys(Tone tone) => tone switch
    {
        Tone.Ok => ("OkM", "OkP"),
        Tone.Attention => ("AttM", "AttP"),
        Tone.Critical => ("CritM", "CritP"),
        _ => ("NeutralM", "NeutralP"),
    };

    /// <summary>The reference's fill table for both value modes, with the day-off substitutions (no red, no allowance fill).</summary>
    private static Paint PaintOf(Part part, (string M, string P) tone, bool left, bool off)
    {
        if (off)
        {
            switch (part)
            {
                case Part.UsedCrit: part = Part.Used; break;
                case Part.CritRest: part = Part.Gray; break;
                case Part.Over: return new Paint("NeutralM", "NeutralP");
                case Part.Allow: return left ? new Paint(tone.M) : new Paint(Paint.Transparent);
            }
        }
        return (part, left) switch
        {
            (Part.Used, true) => new Paint(tone.P, "Rail"),
            (Part.Used, false) => new Paint(tone.M),
            (Part.Allow, true) => new Paint(tone.M),
            (Part.Allow, false) => new Paint(tone.P, "Rail"),
            (Part.Gray, _) => new Paint("Grey"),
            (Part.Track, _) => new Paint(Paint.Transparent),
            (Part.UsedCrit, true) => new Paint("CritP", "CritD"),
            (Part.UsedCrit, false) => new Paint("CritP"),
            (Part.Over, true) => new Paint("CritM", "CritP"),
            (Part.Over, false) => new Paint("CritM"),
            _ => new Paint("CritD"),
        };
    }

    private static BarSegment Seg(double left, double width, Paint paint, bool mirror) =>
        mirror ? new BarSegment(100 - left - width, Math.Max(0, width), paint) : new BarSegment(left, Math.Max(0, width), paint);

    private static BarRing Ring(double left, double width, Tone tone, bool mirror) =>
        mirror ? new BarRing(100 - left - width, width, tone) : new BarRing(left, width, tone);

    // The one-window track stays right of the solid fill in both modes (D-186).
    private static int Rank(Part part, bool left) => part == Part.Track ? 4 : part is Part.Gray or Part.CritRest ? (left ? 0 : 4) : (left ? part == Part.Allow : part != Part.Allow) ? 1 : 2;

    private static StripCell Cell(double weight, IEnumerable<(Part Part, double Weight)> parts, (string M, string P) tone, bool left, bool off, bool label, IReadOnlyList<string> tip)
    {
        var ordered = parts.Where(p => p.Weight > Epsilon).OrderBy(p => Rank(p.Part, left))
            .Select(p => new StripPart(p.Weight, PaintOf(p.Part, tone, left, off), p.Part == Part.Over && !off)).ToArray();
        return new StripCell(weight, ordered, label, off, tip);
    }

    private static StripCell TodayCell(LimitCardModel card, (string M, string P) tone, bool left, bool off, bool rush, decimal used, decimal u0, decimal t, string period, Func<decimal, string> fm)
    {
        var share = t - u0;
        var today = used - u0;
        var gray = rush ? 0 : Math.Max(0, (card.Figures.UsualShare ?? 0) - share);
        var lines = new List<string> { off ? "Today · day off" : rush ? "Today · rush" : "Today" };
        (Part, double)[] parts;
        if (used > t)
        {
            parts = [(Part.UsedCrit, (double)share), (Part.Over, (double)(used - t)), (Part.Gray, (double)gray)];
            if (off)
                lines.Add(fm(today) + " used · a work day would allow " + fm(share));
            else
            {
                lines.Add(left ? "Nothing left today" : fm(today) + " used of " + fm(share) + " allowed");
                lines.Add(fm(used - t) + " over today’s allowance");
            }
        }
        else
        {
            parts = [(Part.Used, (double)today), (Part.Allow, (double)(t - used)), (Part.Gray, (double)gray)];
            lines.Add(left ? fm(t - used) + " of " + fm(share) + " left" : fm(today) + " of " + fm(share) + " used");
        }
        if (gray > 0)
            lines.Add("Grey: " + (card.Cap is { Binding: true, Status: CapStatus.Applied } ? "cap cuts today’s share" : period + " limit cuts today’s share"));
        if (off)
            lines.Add(used > t ? "More than a work day’s share · the next work days shrink evenly" : "No colours on a day off · Work today colours it");
        if (rush)
            lines.Add("Whole remainder · nothing carries past " + ResetPlain(card.Reset));
        return Cell(1, parts, tone, left, off, false, lines);
    }

    /// <summary>AIU-046 R-01: before a window count exists, today is the current 5h window over a neutral track.</summary>
    private static StripCell OneWindowCell(FiveHourModel five, (string M, string P) tone, bool left, bool off)
    {
        var cwU = (double)five.CurrentWindowUsed;
        var lines = new List<string>();
        if (five.CurrentWindowStarted)
        {
            lines.Add("Current 5h window" + (five.CurrentWindowEndsAt is { } end ? " · until " + LedgerFormat.Clock(end) : string.Empty));
            lines.Add(left ? LedgerFormat.Round(100 - five.CurrentWindowUsed) + " % left" : LedgerFormat.Round(five.CurrentWindowUsed) + " % used");
        }
        else
            lines.Add("Next 5h window · starts on first use");
        lines.Add("Window count: collecting data");
        (Part, double)[] parts = left ? [(Part.Allow, 100 - cwU), (Part.Track, cwU)] : [(Part.Used, cwU), (Part.Track, 100 - cwU)];
        return Cell(1, parts, tone, left, off, true, lines);
    }

    private static List<StripCell> FiveHourCells(LimitCardModel card, (string M, string P) tone, bool left, bool off, bool rush, double used, double u0, double t)
    {
        var five = card.FiveHour!;
        var ws = (double)five.WindowShare!.Value;
        var cwU = (double)five.CurrentWindowUsed;
        var winEnd = five.CurrentWindowEndsAt is { } end ? LedgerFormat.Clock(end) : "the window end";
        static string R(double v) => LedgerFormat.Round((decimal)v).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var over = used - t;
        if (over > 0 || t - used <= 0.001)
        {
            var oW = Math.Max(0, over) / ws * 100;
            var pct = (double)LedgerFormat.Round((decimal)((used - u0) / Math.Max(Epsilon, t - u0) * 100));
            var second = left
                ? (over > 0 ? "Nothing left today · " + R(pct - 100) + " % over" : "Nothing left today")
                : R(cwU) + " % used · " + R(pct) + " % of today’s allowance";
            (Part, double)[] parts = over > 0
                ? [(Part.UsedCrit, cwU - oW), (Part.Over, oW), (Part.CritRest, 100 - cwU)]
                : [(Part.Used, cwU), (Part.Gray, 100 - cwU)];
            return [Cell(1, parts, tone, left, off, true, ["Current 5h window · until " + winEnd, second])];
        }

        var raw = new List<(double Weight, (Part, double)[] Parts)>();
        var remaining = (t - used) / ws * 100;
        var rest = 100 - cwU;
        var a0 = Math.Min(rest, remaining);
        remaining -= a0;
        var fit = rush ? five.FitBeforeReset ?? 99 : 99;
        if (remaining > Epsilon)
            raw.Add((1, [(Part.Used, cwU), (Part.Allow, a0), (Part.Gray, 0)]));
        else if (rush)
            raw.Add(((cwU + a0) / 100, [(Part.Used, cwU), (Part.Allow, a0), (Part.Gray, 0)]));
        else
            raw.Add((1, [(Part.Used, cwU), (Part.Allow, a0), (Part.Gray, rest - a0)]));
        while (remaining > Epsilon && raw.Count < fit)
        {
            var a = Math.Min(100, remaining);
            remaining -= a;
            if (remaining > Epsilon)
                raw.Add((1, [(Part.Allow, a), (Part.Gray, 0)]));
            else if (rush)
                raw.Add((a / 100, [(Part.Allow, a), (Part.Gray, 0)]));
            else
                raw.Add((1, [(Part.Allow, a), (Part.Gray, 100 - a)]));
        }

        var cells = new List<StripCell>(raw.Count);
        for (var i = 0; i < raw.Count; i++)
        {
            var parts = raw[i].Parts;
            var usedPart = parts.FirstOrDefault(p => p.Item1 == Part.Used).Item2;
            var allow = parts.First(p => p.Item1 == Part.Allow).Item2;
            var gray = parts.FirstOrDefault(p => p.Item1 == Part.Gray).Item2;
            var last = i == raw.Count - 1;
            var title = i == 0
                ? (five.CurrentWindowStarted ? "Current 5h window · until " + winEnd : "Next 5h window · starts on first use")
                : rush && last ? "Last 5h window before the reset" : gray > 0 ? "Last 5h window today" : "Next 5h window";
            var lines = new List<string> { title };
            if (left)
                lines.Add(allow > 0 ? R(allow) + " % of window left today" : "Window full · opens again " + winEnd);
            else
                lines.Add((usedPart > 0 ? R(usedPart) + " % used · " : string.Empty) + (allow > 0 ? R(allow) + " % allowed today" : "window full"));
            if (gray > 0)
                lines.Add(off ? "Grey: beyond today’s would-be share" : "Grey: counts against tomorrow");
            if (rush && last)
                lines.Add(remaining > Epsilon
                    ? "Only " + raw.Count + " × 5h fit before the reset · ≈ " + R(remaining * ws / 100) + " % resets unused"
                    : "Rush · nothing carries past " + ResetPlain(card.Reset));
            if (off && i == 0)
                lines.Add("Day off · no colours · Work today colours it");
            cells.Add(Cell(raw[i].Weight, parts, tone, left, off, i == 0, lines));
        }
        return cells;
    }

    private static (string Text, IReadOnlyList<string> Tip, bool Estimate) FooterOf(LimitCardModel card, bool left, decimal used, decimal max, decimal? cap, bool rush, Func<decimal, string> fm, DateTimeOffset now)
    {
        var f = card.Figures;
        var estimate = f.Tracking is not null;
        if (cap is { } c)
        {
            var capValue = c;
            var text = (estimate ? "≈ " : string.Empty) + (left
                ? used <= capValue ? fm(capValue - used) + " left to cap" : fm(used - capValue) + " over cap"
                : fm(used) + " of " + fm(capValue) + " cap");
            var tip = new List<string>();
            if (estimate)
                tip.Add("Custom cap " + fm(capValue) + (f.Tracking!.TrackedSince is { } since ? " · tracked since " + LedgerFormat.DayMonth(since) : string.Empty) + " (estimate)" + (f.Tracking.Incomplete ? " · incomplete" : string.Empty));
            else
                tip.Add("Custom cap " + fm(capValue) + (card.Cap!.Binding ? " · binds" : string.Empty));
            if (f.ProviderBalance is { } balance)
                tip.Add("Provider balance " + LedgerFormat.Value(card.Scale, balance) + " " + LedgerFormat.Unit(card.Scale));
            else if (f.ProviderLimit is { Kind: LimitValueKind.Known, Amount: { } limit })
                tip.Add("Provider limit " + fm(limit) + " · " + fm(limit - used) + " left");
            else if (f.ProviderLimit.Kind == LimitValueKind.Unlimited)
                tip.Add("Provider: unlimited");
            else
                tip.Add("Provider limit unknown");
            return (text, tip, estimate);
        }
        if (card.Scale.Kind != ScaleKind.Percent)
        {
            var text = left ? fm(max - used) + " of " + fm(max) + " left" : fm(used) + " of " + fm(max) + " used";
            var tip = new List<string> { "Provider limit " + fm(max) + " " + LedgerFormat.Unit(card.Scale) };
            if (f.ProviderRemaining is { } remaining)
                tip.Add("Provider remaining " + LedgerFormat.Value(card.Scale, remaining));
            return (text, tip, estimate);
        }
        var windows = string.Empty;
        IReadOnlyList<string> footerTip;
        if (card.Layout == CardLayout.FiveHourAndPeriod && card.FiveHour is { } five)
        {
            if (five.WindowShare is { } ws)
            {
                if (rush && five.FitBeforeReset is { } fit)
                    windows = " · " + fit + " × 5h fit before the reset";
                else if (five.WindowsLeftInPeriod is { } n && card.State != CardState.UsedUp)
                    windows = " · ≈ " + n + (five.WindowsLeftMax > n ? "–" + five.WindowsLeftMax : string.Empty) + " × 5h left";
                var share = "One 5h window ≈ " + LedgerFormat.Round(ws) + " % of " + LedgerFormat.PeriodLabel(card.Period);
                if (five is { WindowShareLow: { } low, WindowShareHigh: { } high })
                {
                    // Whole numbers rounded outward keep the shown bounds guaranteed.
                    share += " (" + Math.Floor(low) + "–" + Math.Ceiling(high) + " %)" + (five.Rough ? " · rough" : string.Empty);
                    share += " · from " + five.Windows + (five.Windows == 1 ? " window" : " windows");
                }
                else
                    share += " (estimate)";
                footerTip = [share, "≈ " + Math.Floor(100 / ws) + " windows per " + (card.Period.Kind == PeriodKind.CalendarMonth ? "month" : "week")];
            }
            else
                footerTip = ["5h window size not estimated yet", "Current 5h window " + LedgerFormat.Round(five.CurrentWindowUsed) + " % used" + (five.CurrentWindowEndsAt is { } end ? " · until " + LedgerFormat.Clock(end) : string.Empty)];
        }
        else
            footerTip = f.UsualShare is { } usual ? ["Usual daily share ≈ " + LedgerFormat.Value(card.Scale, usual) + " of " + LedgerFormat.PeriodLabel(card.Period)] : ["Daily share from the work days"];
        return ((left ? fm(max - used) + " left" : fm(used) + " used") + windows, footerTip, estimate || windows.Contains('≈', StringComparison.Ordinal));
    }

    private static string BarName(LimitCardModel card, string period) => card.Scale.Kind switch
    {
        ScaleKind.Percent => period,
        ScaleKind.Money => "Month · " + (card.Scale.Currency ?? "money") + (card.Figures.Tracking is not null ? " (tracked estimate)" : string.Empty),
        _ => "Month · " + LedgerFormat.Unit(card.Scale) + (card.Figures.Tracking is not null ? " (tracked estimate)" : string.Empty),
    };

    private static string StateWords(LimitCardModel card, AccountModel account, string? pill, IReadOnlyList<MarkVisual> marks, DateTimeOffset now)
    {
        var words = new List<string>();
        if (card.Freshness is { IsStale: true, ReadingAt: { } reading })
        {
            if (account.Health == AccountHealth.SyncFailedStale)
                words.Add("sync failed");
            words.Add("reading from " + LedgerFormat.Clock(reading) + ", " + LedgerFormat.AgeWords(reading, now));
            if (!IsReasonState(card.State))
                pill = PillText(card.State, card.Period);
        }
        words.Insert(0, pill is null ? "OK" : LedgerFormat.Spoken(pill.Replace("5h", "5 hour", StringComparison.Ordinal).Replace("7d", "7 day", StringComparison.Ordinal)));
        words.AddRange(marks.Select(m => m.Text));
        return string.Join(", ", words);
    }

    private static string TodaySummary(LimitCardModel card, IReadOnlyList<StripCell> cells, bool left)
    {
        var visual = left ? cells.Reverse().ToArray() : [.. cells];
        if (card is { Layout: CardLayout.FiveHourAndPeriod, FiveHour: { WindowShare: null } one })
            return one.CurrentWindowStarted ? "Current five-hour window " + LedgerFormat.Spoken(visual[0].Tip[1]) + "." : "Next five-hour window starts on first use.";
        if (card.Layout == CardLayout.FiveHourAndPeriod && card.FiveHour?.WindowShare is not null && visual.Length > 0)
        {
            var current = visual[0].Tip.Count > 1 ? visual[0].Tip[1] : string.Empty;
            return "Today " + visual.Length + (visual.Length == 1 ? " five-hour window, " : " five-hour windows, ") + LedgerFormat.Spoken(current.Replace(" · ", ", ", StringComparison.Ordinal)) + " in the current window.";
        }
        var line = visual.Length > 0 && visual[0].Tip.Count > 1 ? visual[0].Tip[1] : string.Empty;
        return "Today " + LedgerFormat.Spoken(line.Replace(" · ", ", ", StringComparison.Ordinal)) + ".";
    }

    private static string Name(LimitCardModel card, AccountModel account, string state, string body, string? reset)
    {
        var kind = card.Layout switch
        {
            CardLayout.FiveHourAndPeriod => "5 hour and " + LedgerFormat.PeriodWords(card.Period),
            CardLayout.Period => LedgerFormat.PeriodWords(card.Period),
            _ => null,
        };
        var head = account.DisplayName + (card.ScopeLabel is { } scope ? " " + scope.Replace("7d", "7 day", StringComparison.Ordinal) : string.Empty) + (kind is null ? string.Empty : ", " + kind);
        var text = head + ", " + state + ". " + LedgerFormat.Spoken(body.Trim());
        if (!string.IsNullOrEmpty(reset))
            text += " " + reset + ".";
        return text.Replace("..", ".", StringComparison.Ordinal);
    }
}
