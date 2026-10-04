using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiUsage.Core.Accounts;
using AiUsage.Core.Budget;
using AiUsage.Core.Usage;
using AiUsage.Features.Ledger.Contract;
using DisplayLimit = AiUsage.Features.Ledger.Contract.LimitValue;

namespace AiUsage.Adapters.Live;

// Scope must be established by source evidence. Current Claude wire mappings do not establish it.
internal enum MonetaryScope { Unknown, Account, Shared }
internal sealed record LedgerLimit(LimitFacts Facts, ReadingSeriesKey Series, IReadOnlyList<ReadingRun> Runs)
{
    public MonetaryScope MonetaryScope { get; init; }
    public bool UsesWorkBudget { get; init; }
}

/// <summary>Core facts and calculations projected once into display units. No transport or persistence.</summary>
internal static class LiveLedgerProjection
{
    // Owner display policy: monetary-only accounts use a monthly work budget.
    // This does not modify provider facts or claim independently verified commercial scope.
    public static LedgerLimit[] AccountLimits(IReadOnlyList<LedgerLimit> limits)
    {
        var hasSubscriptionWindows = limits.Any(l => l.Facts.Kind == LimitKind.PercentWindow);
        return limits.Select(l => l with { UsesWorkBudget = !hasSubscriptionWindows &&
            l.Facts.Kind == LimitKind.MonetaryPool && l.MonetaryScope != MonetaryScope.Shared }).ToArray();
    }

    public static string CardId(ReadingSeriesKey series) => series.AccountTarget + ":" + Convert.ToBase64String(Encoding.UTF8.GetBytes(
        JsonSerializer.Serialize(new[] { series.Limit.Provider, series.Limit.Family, series.Limit.NativeDiscriminator }, LedgerProjectionJson.Default.StringArray)));

    public static ProviderKind Provider(string provider) => provider switch
    {
        "claude" => ProviderKind.Claude, "codex" => ProviderKind.Codex, "copilot" => ProviderKind.Copilot,
        "antigravity" => ProviderKind.Antigravity, _ => throw new ArgumentOutOfRangeException(nameof(provider))
    };

    public static AccountModel Account(AccountSnapshot account, string name, IReadOnlyList<LedgerLimit> limits,
        BudgetConfiguration configuration, DateTimeOffset now, TimeZoneInfo zone, DateOnly? workToday)
    {
        var id = account.AccountId.ToString("N");
        var session = account.Session;
        var readingAt = session.Quota?.FetchedAt;
        var stale = readingAt is null || now - readingAt > TimeSpan.FromMinutes(15);
        var health = !account.Connected ? AccountHealth.SignedOut : session.Status == ProviderSessionStatus.ReauthenticationRequired
            ? AccountHealth.SignInExpired : session.Status == ProviderSessionStatus.RecoveryRequired ? AccountHealth.ProviderError
            : session.Failure is not null ? stale ? AccountHealth.SyncFailedStale : AccountHealth.SyncFailedFresh
            : readingAt is null ? AccountHealth.ProviderError : AccountHealth.Ok;
        var normalized = AccountLimits(limits).Select(l => l with { Series = new(id, l.Facts.Key) }).ToArray();
        var hasSubscriptionWindows = normalized.Any(l => l.Facts.Kind == LimitKind.PercentWindow);
        var cards = new List<LimitCardModel>();
        foreach (var data in normalized)
        {
            var facts = data.Facts;
            if (hasSubscriptionWindows && facts.Key is { Provider: "codex", Family: "CX-B" }) continue;
            // Only a known shared pool may consume its short window into the period card.
            if (facts.Duration == TimeSpan.FromHours(5) && normalized.Any(w => IsPair(facts, w.Facts, session.Quota))) continue;
            var cap = configuration.Caps.FirstOrDefault(c => c.Series == data.Series)?.Cap;
            var card = Card(data, cap, now, zone, configuration.WorkDays.ToHashSet(), workToday, stale) with
            { Freshness = new(stale, readingAt), ScopeLabel = Label(facts, session.Quota) };
            var shortData = normalized.FirstOrDefault(s => IsPair(s.Facts, facts, session.Quota));
            if (shortData is not null && shortData.Facts.UsedPercent is { } shortUsed)
            {
                var estimate = SessionEstimator.Estimate(data.Runs.Concat(shortData.Runs),
                    new(shortData.Series, data.Series, "shared", "shared", TimeSpan.FromHours(5), TimeSpan.FromDays(7)), now);
                var figures = facts.UsedPercent is { } weeklyUsed
                    ? SessionEstimator.Figures(estimate, weeklyUsed, card.Figures.TodayEnd - card.Figures.DayStart) : new SessionFigures(null, null);
                card = card with
                {
                    Layout = card.Layout == CardLayout.Period ? CardLayout.FiveHourAndPeriod : card.Layout,
                    FiveHour = new(estimate.Cost is { } cost ? BudgetDisplay.Down(cost, .1m) : null, shortUsed, shortUsed > 0,
                        shortData.Facts.Reset?.At, figures.Weekly is { WholeSessions: <= int.MaxValue } count ? (int)count.WholeSessions : null,
                        card.State == CardState.Rush && facts.Reset is { } reset ? (int)Math.Max(0, (reset.At - now).TotalHours / 5) : null),
                    State = shortUsed >= 100 && shortData.Facts.Reset?.At > now && card.State is not (CardState.UsedUp or CardState.NotReady or CardState.DayOff or CardState.ValueUnknown)
                        ? CardState.FiveHourFull : card.State
                };
            }
            var marks = card.Marks.ToList();
            if (health is AccountHealth.SyncFailedFresh or AccountHealth.SyncFailedStale) marks.Add(new(MarkKind.SyncFailed, account.LastFailureAt));
            if (health == AccountHealth.SignInExpired) marks.Add(new(MarkKind.SignInExpired));
            var spend = normalized.FirstOrDefault(l => l.Facts.Key.Family == "CL-X");
            if (!stale && spend is { MonetaryScope: MonetaryScope.Account } && spend.Facts.Enabled != false &&
                QuantityMath.TryAlign(spend.Facts.Used, spend.Runs.LastOrDefault(r => r.Series == spend.Series && r.FirstSeen <= now)?.Value,
                    out var reportedSpend, out var observedSpend, out _) && reportedSpend == observedSpend)
            {
                foreach (var window in new[] { data, shortData }.OfType<LedgerLimit>().Where(x => x.Facts.Duration is not null &&
                    x.Facts.UsedPercent == 100 && x.Facts.Reset?.At > now))
                {
                    var evidence = ExtraUsageEvidence.Calculate(window.Runs, window.Series, window.Facts.Duration!.Value, spend.Runs, spend.Series, now);
                    if (evidence.OnExtraUsage != true) continue;
                    marks.Add(new(MarkKind.OnExtraUsage, evidence.FullSince, window.Facts.Reset?.At, Amount: Amount(evidence.SpendSinceFull),
                        Currency: evidence.SpendSinceFull?.Currency, Exponent: evidence.SpendSinceFull?.Exponent));
                    break;
                }
            }
            cards.Add(card with { Marks = marks, Action = health == AccountHealth.SignInExpired ? CardAction.SignIn : card.Action });
        }
        if (!account.Connected || cards.Count == 0)
            cards = [new(id + ":status", null, CardLayout.Note, ScaleModel.Percent, PeriodModel.Unknown,
                !account.Connected ? CardState.SignedOut : CardState.NotReady, new(stale, readingAt), [], LimitFigures.UsedOnly(null),
                null, null, null, null, !account.Connected || health == AccountHealth.SignInExpired ? CardAction.SignIn : CardAction.None, null)];
        if (cards.Any(c => c.Monetary is null))
            cards = cards.Select(c => c.Monetary is not null && c.Action == CardAction.SignIn ? c with { Action = CardAction.None } : c).ToList();
        return new(id, Provider(account.Provider), name, health, readingAt, account.LastFailureAt, null, cards);
    }

    public static LimitCardModel Card(LedgerLimit data, PersonalCap? cap, DateTimeOffset now, TimeZoneInfo zone,
        IReadOnlySet<DayOfWeek> workDays, DateOnly? workToday, bool stale)
    {
        var facts = data.Facts;
        var period = PeriodResolver.Resolve(data.UsesWorkBudget ? facts with { IsMonthly = true, AllowsCalendarFallback = true } : facts, now, zone);
        var used = facts.Kind == LimitKind.PercentWindow ? facts.UsedPercent is { } p ? new CountQuantity(p, "percent") : null : facts.Used;
        var runs = data.Runs;
        TrackingModel? tracking = null;
        // A balance or cumulative spend without a provider period needs locally tracked consumption.
        if (period is not null && (facts.Used is null && facts.Remaining is not null || !data.UsesWorkBudget && facts.Kind != LimitKind.PercentWindow && period.EndOrigin == ValueOrigin.Assumed))
        {
            var tracked = ReadingCalculations.Track(runs, data.Series, period, now, facts.Used is null, 1);
            used = tracked.Used;
            runs = tracked.Runs;
            tracking = new(tracked.TrackedSince is { } since ? WorkCalendar.Date(since, zone) : null, tracked.Incomplete);
        }
        var instance = runs.LastOrDefault(r => r.FirstSeen <= now)?.PeriodInstance;
        var dayStart = instance is null ? null : ReadingCalculations.DayStart(runs, data.Series, instance, now, zone).Value;
        var result = BudgetEngine.Calculate(new(facts, cap, period, used, dayStart, now, zone)
        { WorkDays = workDays, WorkToday = workToday, IsStale = stale });
        var scale = Scale(result.Scale ?? used ?? facts.Limit.Value ?? facts.Remaining, facts);
        var provider = ProviderLimit(facts);
        var off = !workDays.Contains(WorkCalendar.Date(now, zone).DayOfWeek);
        var extraDay = off && workToday == WorkCalendar.Date(now, zone);
        var share = off ? result.DayOffShare : result.TodayShare;
        var left = share - result.UsedToday;
        var state = State(facts, used, result, period, now, off && !extraDay, left);
        var marks = new List<CardMark>();
        if (facts.Reset?.At <= now) marks.Add(new(MarkKind.PastReset, facts.Reset.At));
        if (extraDay && state == CardState.OnTrack) marks.Add(new(MarkKind.ExtraDay));
        var capModel = cap is null ? null : new CapModel(Amount(cap.Amount) ?? 0, result.Binding == LimitBinding.PersonalCap,
            result.CapRejected ? CapStatus.CurrencyMismatch : CapStatus.Applied);
        decimal? Display(decimal? amount) => DisplayAmount(amount, result.Scale ?? used, scale);
        var figures = new LimitFigures(Display(result.Used) ?? Rounded(Amount(used), scale), Display(result.DayStart),
            result.DayStart is { } start && share is { } today ? Display(start + today) : null, Display(result.Baseline), provider,
            Display(result.Limit), Rounded(Amount(facts.Remaining), scale), facts.Used is null ? Rounded(Amount(facts.Remaining), scale) : null, tracking);
        var layout = state is CardState.SignedOut or CardState.ValueUnknown or CardState.NotIncluded or CardState.NoCap or CardState.LimitUnknown
            ? CardLayout.Note : state is CardState.PeriodUnknown or CardState.NotReady || result.Reason == NoBudgetReason.ShortWindow
                ? CardLayout.UsedOnly : facts.Kind == LimitKind.PercentWindow ? CardLayout.Period : CardLayout.Pool;
        var reset = facts.Reset is { } known ? new ResetModel(known.Precision == ResetPrecision.Date ? null : known.At,
            known.Precision == ResetPrecision.Date ? DateOnly.FromDateTime(known.At.Date) : null, ResetProvenance.Provider,
            period?.StartOrigin == ValueOrigin.Assumed ? WorkCalendar.Date(period.Start, zone) : null)
            : period is null ? null : new ResetModel(period.End, null, ResetProvenance.Assumed, WorkCalendar.Date(period.Start, zone));
        var target = facts.Kind != LimitKind.PercentWindow && (scale is { Kind: ScaleKind.Count, UnitName: not (null or "unknown") } ||
            scale is { Kind: ScaleKind.Money, Currency: not null, Exponent: >= 0 and <= 18 })
            ? CardId(data.Series) : null;
        var card = new LimitCardModel(CardId(data.Series), Label(facts, null), layout, scale, Period(facts, period), state, new(stale, null), marks,
            figures, null, reset, capModel, target, state is CardState.NoCap or CardState.LimitUnknown && target is not null ? CardAction.SetCap : CardAction.None, null);
        return facts.Key.Family == "CL-X" ? MonetaryCard(card, data, cap, period) : card;
    }

    private static LimitCardModel MonetaryCard(LimitCardModel card, LedgerLimit data, PersonalCap? cap, PeriodBounds? period)
    {
        var facts = data.Facts;
        var scope = data.MonetaryScope;
        var qualification = data.UsesWorkBudget ? "Monthly work budget" : scope switch
        {
            MonetaryScope.Account => "Account spending",
            MonetaryScope.Shared => "Shared spending · not a personal allowance",
            _ => "Spending scope unverified · may be shared"
        };
        qualification += facts.Reset is null ? " · provider period unknown" : " · provider period";
        if (period?.EndOrigin == ValueOrigin.Assumed) qualification += " · calendar month assumed";
        var compatible = facts.Used is MoneyQuantity used && Amount(used) is not null &&
            (facts.Limit.Value is null || QuantityMath.TryAlign(used, facts.Limit.Value, out _, out _, out _));
        var unavailable = facts.Enabled == false ? "Spending disabled · retained readings and caps kept"
            : scope != MonetaryScope.Account && !data.UsesWorkBudget ? "Budget and cap editing unavailable until spending scope is established"
            : !compatible ? "Budget unavailable until compatible amounts and currency are known"
            : period is null ? "Budget and cap editing unavailable until the period is known" : null;
        static MonetaryAmount? Native(Quantity? value) => value is MoneyQuantity m ? new(m.MinorUnits, m.Exponent, m.Currency) : null;
        var details = new MonetaryDetails(Native(facts.Used), Native(facts.Limit.Value), card.Figures.ProviderLimit.Kind,
            Native(cap?.Amount), facts.Enabled, qualification, unavailable);
        if (unavailable is null) return card with { Monetary = details };
        return card with
        {
            Monetary = details, Layout = CardLayout.Note,
            State = facts.Enabled == false ? CardState.NotIncluded : CardState.PeriodUnknown,
            Figures = LimitFigures.UsedOnly(Amount(facts.Used)) with { ProviderLimit = ProviderLimit(facts) },
            Cap = card.Cap is { } retained ? retained with { Binding = false, Status = card.Cap.Status == CapStatus.CurrencyMismatch ? CapStatus.CurrencyMismatch : CapStatus.Inactive } : null,
            CapTargetId = null, Action = CardAction.None
        };
    }

    private static CardState State(LimitFacts facts, Quantity? used, BudgetResult result, PeriodBounds? period,
        DateTimeOffset now, bool off, decimal? left)
    {
        if (facts.Allowed == false || facts.Enabled == false || facts.Limit.Value is CountQuantity { Value: 0 } || facts.Limit.Value is MoneyQuantity { MinorUnits: 0 }) return CardState.NotIncluded;
        if (result.Reason == NoBudgetReason.Unlimited || facts.Key.Family == "CX-B" && result.Binding != LimitBinding.PersonalCap) return CardState.NoCap;
        if (used is null) return CardState.ValueUnknown;
        if (period?.End <= now || period?.Start > now) return CardState.NotReady;
        if (result.ProviderUsedUp) return CardState.UsedUp;
        if (result.Binding == LimitBinding.PersonalCap && result.Remaining <= 0) return result.Remaining < 0 ? CardState.OverCap : CardState.CapReached;
        if (result.Reason == NoBudgetReason.LimitUnknown) return CardState.LimitUnknown;
        if (period is null) return CardState.PeriodUnknown;
        if (result.Reason == NoBudgetReason.NotReady) return CardState.NotReady;
        if (off) return CardState.DayOff;
        if (result.Rush) return CardState.Rush;
        if (left < 0) return CardState.OverToday;
        if (left == 0) return CardState.TodayUsed;
        if (result.BudgetState == BudgetState.Attention) return result.Binding == LimitBinding.PersonalCap ? CardState.CapClose
            : facts.Kind == LimitKind.PercentWindow ? CardState.TodayLow : CardState.TodayShort;
        return CardState.OnTrack;
    }

    public static DisplayLimit ProviderLimit(LimitFacts facts)
    {
        if (facts.Kind == LimitKind.PercentWindow) return DisplayLimit.Known(100);
        if (facts.Limit.State == LimitValueState.Unlimited) return DisplayLimit.Unlimited;
        return Amount(facts.Limit.Value) is { } amount ? amount == 0 ? DisplayLimit.Zero : DisplayLimit.Known(amount) : DisplayLimit.Unknown;
    }

    public static decimal? Amount(Quantity? quantity) => quantity switch
    {
        CountQuantity count => count.Value,
        MoneyQuantity { Exponent: >= 0 and <= 18, Currency: not null } money => money.MinorUnits / Power(money.Exponent.Value),
        _ => null
    };
    public static ScaleModel Scale(Quantity? quantity, LimitFacts facts) => quantity is MoneyQuantity money
        ? new(ScaleKind.Money, null, money.Currency, money.Exponent) : facts.Kind == LimitKind.PercentWindow ? ScaleModel.Percent
        : facts.Kind == LimitKind.MonetaryPool ? new(ScaleKind.Money, null, null, null) : ScaleModel.Count(facts.Unit);
    private static decimal Power(int exponent) { decimal scale = 1; for (int i = 0; i < exponent; i++) scale *= 10; return scale; }
    private static decimal? Rounded(decimal? amount, ScaleModel scale) => amount is { } value ? BudgetDisplay.Down(value,
        scale.Kind == ScaleKind.Percent ? .1m : scale.Kind == ScaleKind.Money && scale.Exponent is >= 0 and <= 18 ? 1 / Power(scale.Exponent.Value) : 1) : null;
    private static decimal? DisplayAmount(decimal? amount, Quantity? quantity, ScaleModel scale) => Rounded(quantity is MoneyQuantity { Exponent: >= 0 and <= 18 } money
        ? amount / Power(money.Exponent.Value) : amount, scale);
    private static PeriodModel Period(LimitFacts facts, PeriodBounds? period) => period is null ? PeriodModel.Unknown
        : facts.IsMonthly || period.EndOrigin == ValueOrigin.Assumed ? PeriodModel.Month(period.StartOrigin == ValueOrigin.Assumed)
        : new(PeriodKind.Days, period.End - period.Start, period.StartOrigin == ValueOrigin.Assumed);

    private static string? Group(LimitFacts facts, QuotaSnapshot? quota) => quota?.Groups.FirstOrDefault(g => g.Windows.Any(w =>
        facts.Key.NativeDiscriminator == JsonSerializer.Serialize(new[] { g.Id, w.Id }, LedgerProjectionJson.Default.StringArray)))?.Id;
    private static bool IsPair(LimitFacts shortWindow, LimitFacts weekly, QuotaSnapshot? quota) =>
        shortWindow.Key.Provider == weekly.Key.Provider && shortWindow.Duration == TimeSpan.FromHours(5) && weekly.Duration == TimeSpan.FromDays(7) &&
        (shortWindow.Key.Family == "CL-S" && weekly.Key.Family == "CL-W" || shortWindow.Key.Family == "CX-P" && weekly.Key.Family == "CX-S" ||
         shortWindow.Key.Family is "AG-5" or "CX-A" && weekly.Key.Family is "AG-W" or "CX-A" && Group(shortWindow, quota) is { } group && group == Group(weekly, quota));
    private static string? Label(LimitFacts facts, QuotaSnapshot? quota) => facts.Key.Family switch
    {
        "CL-S" or "CL-W" or "CX-P" or "CX-S" => null, "CL-M" => facts.Key.NativeDiscriminator,
        "CL-X" => "Spending", "CX-B" => "Credits", "CX-I" => "Individual limit", "GH-P" => "Premium requests",
        "GH-C" => "Chat", "GH-I" => "Completions", _ => quota?.Groups.FirstOrDefault(g => g.Id == Group(facts, quota))?.Name ?? facts.Key.NativeDiscriminator
    };

    public static HistoryModel History(LedgerLimit data, DateTimeOffset now, TimeZoneInfo zone)
    {
        var today = WorkCalendar.Date(now, zone);
        var days = new List<HistoryDay>();
        var resets = new HashSet<DateOnly>();
        for (var date = today.AddDays(-34); date <= today; date = date.AddDays(1))
        {
            var start = WorkCalendar.Midnight(date, zone);
            var end = WorkCalendar.Midnight(date.AddDays(1), zone);
            var at = end > now ? now : end.AddTicks(-1);
            var allRuns = data.Runs;
            if (PeriodResolver.Resolve(data.Facts, at, zone) is { } period &&
                (data.Facts.Used is null && data.Facts.Remaining is not null || !data.UsesWorkBudget && data.Facts.Kind != LimitKind.PercentWindow && period.EndOrigin == ValueOrigin.Assumed))
                allRuns = ReadingCalculations.Track(allRuns, data.Series, period, at, data.Facts.Used is null, 1).Runs;
            var runs = allRuns.Where(r => r.FirstSeen <= at && r.LastConfirmed >= start).ToArray();
            decimal? total = null;
            foreach (var group in runs.GroupBy(r => r.PeriodInstance))
            {
                var last = group.MaxBy(r => r.FirstSeen)!;
                var baseline = ReadingCalculations.DayStart(allRuns, data.Series, group.Key, at, zone);
                var difference = QuantityMath.Subtract(last.Value, baseline.Value);
                if (Amount(difference) is { } amount) total = (total ?? 0) + Math.Max(0, amount);
                if (group.Any(r => r.PeriodStartedAt >= start && r.PeriodStartedAt < end)) resets.Add(date);
            }
            days.Add(new(date, total));
        }
        return new(CardId(data.Series), Period(data.Facts, PeriodResolver.Resolve(data.Facts, now, zone)), days, null, resets.Order().ToArray(), today);
    }
}

[JsonSerializable(typeof(string[]))]
internal sealed partial class LedgerProjectionJson : JsonSerializerContext;
