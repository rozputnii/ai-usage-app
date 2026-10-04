using System.Globalization;
using System.Text;
using System.Text.Json;
using AiUsage.Adapters.Live;
using AiUsage.Adapters.Live.Audit;
using AiUsage.Core.Accounts;
using AiUsage.Core.Budget;
using AiUsage.Core.Usage;
using AiUsage.Features.Ledger.Contract;
using AiUsage.Infrastructure.Persistence;
using AiUsage.Infrastructure.Providers.Claude;
using AiUsage.Infrastructure.Providers.Codex;
using AiUsage.Infrastructure.Providers.Copilot;
using AiUsage.Infrastructure.Providers.Antigravity;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

/// <summary>Independent expected states/amounts over real parsers, local recording and budget projection; exports exactly tested inputs.</summary>
public sealed partial class AuditScenarioTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset WeeklyReset = new(2026, 10, 12, 0, 0, 0, TimeSpan.Zero);
    private sealed record Case(string Id, string Provider, string Json, decimal? Baseline, CardState State,
        decimal? Used, decimal? TodayEnd, CardLayout Layout, decimal? Cap = null, DateTimeOffset? Time = null,
        AccountHealth Health = AccountHealth.Ok, bool Estimate = false, string Zone = "UTC", DateTimeOffset? Fetched = null, int? Fit = null);
    private static string N(decimal? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "null";
    private static string Stamp(DateTimeOffset value) => value.ToString("O", CultureInfo.InvariantCulture);
    private static string Week(string provider, decimal? used, decimal? shortUsed = null, bool pair = false, DateTimeOffset? reset = null, bool shortReset = true,
        DateTimeOffset? shortEndOverride = null)
    {
        var shortEnd = shortReset ? Stamp(shortEndOverride ?? Now.AddHours(3)) : null;
        if(provider=="claude")
        {
            var body=new Dictionary<string,object?> { ["seven_day"]=new { utilization=used,resets_at=Stamp(reset??WeeklyReset) } };
            if(pair) body["five_hour"]=new { utilization=shortUsed,resets_at=shortEnd };
            return JsonSerializer.Serialize(body);
        }
        if(provider=="codex")
        {
            var limits=new Dictionary<string,object?> { ["secondary_window"]=new { used_percent=used,limit_window_seconds=604800,reset_at=(reset??WeeklyReset).ToUnixTimeSeconds() } };
            if(pair) limits["primary_window"]=new { used_percent=shortUsed,limit_window_seconds=18000,reset_at=shortReset?(long?)(shortEndOverride ?? Now.AddHours(3)).ToUnixTimeSeconds():null };
            return JsonSerializer.Serialize(new { rate_limit=limits,credits=new { has_credits=true,balance=500 } });
        }
        var buckets=new List<object> { new { bucketId="weekly",window="weekly",remainingFraction=used is null?null:1-used/100,resetTime=Stamp(reset??WeeklyReset) } };
        if(pair) buckets.Add(new { bucketId="short",window="5h",remainingFraction=shortUsed is null?null:1-shortUsed/100,resetTime=shortEnd });
        return JsonSerializer.Serialize(new { groups=new[] { new { displayName="Gemini Models",buckets } } });
    }
    private static string Pool(decimal? used, decimal? limit = 300, bool unlimited = false, string group = "premium_interactions", string reset = "2026-11-01") =>
        JsonSerializer.Serialize(new { quota_reset_date=reset,quota_snapshots=new Dictionary<string,object> { [group]=new { entitlement=limit,remaining=limit-used,unlimited } } });
    private static string Money(decimal? used, decimal? limit = 300, bool? enabled = true, string currency = "USD", int exponent = 2) =>
        JsonSerializer.Serialize(new { spend=new { enabled,used=new { amount_minor=used is { } u?(long?)(u*(decimal)Math.Pow(10,exponent)):null,currency,exponent },
            limit=limit is null?null:new { amount_minor=(long)(limit*(decimal)Math.Pow(10,exponent)),currency,exponent } } });
    public static IEnumerable<object[]> Cases() => AllCases().Select(c => new object[] { c.Id });
    private static IEnumerable<Case> AllCases()
    {
        foreach (var provider in new[] { "claude", "codex", "antigravity" })
        {
            foreach (var (suffix, value, state) in new (string, decimal, CardState)[]
            {
                ("zero", 0, CardState.OnTrack), ("ordinary", 45, CardState.OnTrack),
                ("low-below", 53.9m, CardState.OnTrack), ("low-at", 54, CardState.OnTrack), ("low-above", 54.1m, CardState.TodayLow),
                ("today-below", 59.9m, CardState.TodayLow), ("today-at", 60, CardState.TodayUsed), ("today-above", 60.1m, CardState.OverToday),
                ("provider-below", 99.9m, CardState.OverToday), ("provider-at", 100, CardState.UsedUp)
            }) yield return new("P-" + provider + "-" + suffix, provider, Week(provider, value), value == 0 ? 0 : 40,
                state, value, value == 0 ? 33.3m : 60, CardLayout.Period);
            yield return new("P-" + provider + "-unknown", provider, Week(provider, null), null, CardState.ValueUnknown, null, null, CardLayout.Note);
            foreach (var shortValue in new decimal?[] { 0, 35, 99.9m, 100, null, -0.1m, 100.1m })
                yield return new("W-" + provider + "-" + (shortValue is null ? "null" : N(shortValue).Replace('.', '_')), provider,
                    Week(provider, 45, shortValue, true), 40, shortValue == 100 ? CardState.FiveHourFull : CardState.OnTrack,
                    45, 60, shortValue is < 0 or > 100 or null ? CardLayout.Period : CardLayout.FiveHourAndPeriod, Estimate: true);
            yield return new("W-" + provider + "-unknown-reset", provider, Week(provider, 45, 100, true, shortReset: false), 40,
                CardState.OnTrack, 45, 60, CardLayout.FiveHourAndPeriod);
            yield return new("W-" + provider + "-both-full", provider, Week(provider, 100, 100, true), 40, CardState.UsedUp, 100, 60, CardLayout.FiveHourAndPeriod);
            yield return new("W-" + provider + "-day-full", provider, Week(provider, 60, 35, true), 40, CardState.TodayUsed, 60, 60, CardLayout.FiveHourAndPeriod);
            foreach (var used in new[] { 85m, 90m, 95m }) yield return new("W-" + provider + "-estimate-" + N(used), provider,
                Week(provider, used, 35, true), 40, CardState.OverToday, used, 60, CardLayout.FiveHourAndPeriod, Estimate: true);
        }
        foreach (var (id, used, state) in new (string, decimal, CardState)[]
        { ("zero",0,CardState.OnTrack), ("ordinary",125,CardState.OnTrack), ("low-below",126,CardState.OnTrack),
          ("low-at",127,CardState.OnTrack), ("low-above",128,CardState.TodayShort), ("today-below",129,CardState.TodayShort),
          ("today-at",130,CardState.TodayUsed), ("today-above",131,CardState.OverToday), ("provider-below",299,CardState.OverToday), ("provider-at",300,CardState.UsedUp) })
            yield return new("C-" + id, "copilot", Pool(used), used == 0 ? 0 : 120, state, used, used == 0 ? 16 : 130, CardLayout.Pool);
        foreach (var (id, used, state) in new (string, decimal, CardState)[]
        { ("zero",0,CardState.OnTrack), ("ordinary",125.50m,CardState.OnTrack), ("low-below",126.99m,CardState.OnTrack),
          ("low-at",127,CardState.OnTrack), ("low-above",127.01m,CardState.TodayShort), ("today-below",129.99m,CardState.TodayShort),
          ("today-at",130,CardState.TodayUsed), ("today-above",130.01m,CardState.OverToday), ("provider-below",299.99m,CardState.OverToday),
          ("provider-at",300,CardState.UsedUp), ("provider-above",300.01m,CardState.UsedUp) })
            yield return new("M-" + id, "claude", Money(used), used == 0 ? 0 : 120, state, used, used == 0 ? 16.66m : 130, CardLayout.Pool);
        foreach (var money in new[] { false, true })
        foreach (var cap in new[] { 180m, 300m, 500m })
        foreach (var used in new[] { cap - (money ? .01m : 1), cap, cap + (money ? .01m : 1) }.Where(u => money || u <= 300))
        {
            var state = used >= 300 ? CardState.UsedUp : used >= cap ? used == cap ? CardState.CapReached : CardState.OverCap : CardState.OverToday;
            var baseline = 120m;
            var end = cap < 300 ? 123.33m : 130m;
            if (!money) end = decimal.Floor(end);
            yield return new((money ? "MC-" : "CC-") + N(cap) + "-" + N(used), money ? "claude" : "copilot",
                money ? Money(used) : Pool(used), baseline, state, used, end, CardLayout.Pool, cap);
        }
        yield return new("C-cap-close", "copilot", Pool(125), 120, CardState.CapClose,125,126,CardLayout.Pool,240);
        yield return new("M-cap-close", "claude", Money(125), 120, CardState.CapClose,125,126.66m,CardLayout.Pool,240);
        yield return new("C-zero-limit", "copilot", Pool(0,0), 0, CardState.NotIncluded,0,null,CardLayout.Note);
        yield return new("C-unlimited", "copilot", Pool(125,300,true),120,CardState.NoCap,125,null,CardLayout.Note);
        yield return new("C-unlimited-cap", "copilot", Pool(125,300,true),120,CardState.OnTrack,125,141,CardLayout.Pool,500);
        yield return new("C-missing-limit", "copilot", Pool(null,null),null,CardState.ValueUnknown,null,null,CardLayout.Note);
        yield return new("C-unknown-pool", "copilot", Pool(125,300,false,"future_pool"),null,CardState.LimitUnknown,125,null,CardLayout.Note);
        yield return new("M-no-limit", "claude", Money(125,null),120,CardState.LimitUnknown,125,null,CardLayout.Note);
        yield return new("M-no-limit-cap", "claude", Money(125,null),120,CardState.OnTrack,125,141.11m,CardLayout.Pool,500);
        yield return new("M-disabled", "claude", Money(125,300,false),120,CardState.NotIncluded,125,null,CardLayout.Note);
        yield return new("M-unknown-enabled", "claude", Money(125,300,null),120,CardState.OnTrack,125,130,CardLayout.Pool);
        yield return new("M-unknown-amount", "claude", """{"spend":{"enabled":true,"used":null,"limit":{"amount_minor":30000,"currency":"USD","exponent":2}}}""",null,CardState.PeriodUnknown,null,null,CardLayout.Note);
        yield return new("M-long", "claude", Money(12345678.91m,999999999.99m),12345678.90m,CardState.OnTrack,12345678.91m,67215363.40m,CardLayout.Pool);
        foreach (var currency in new[] { "EUR", "GBP", "JPY", "XTS" }) yield return new("M-currency-" + currency,"claude",Money(125,300,true,currency,currency=="JPY"?0:2),120,CardState.OnTrack,125,130,CardLayout.Pool);
        foreach (var health in Enum.GetValues<AccountHealth>()) yield return new("H-" + health,"claude",Week("claude",45),40,
            health==AccountHealth.SignedOut?CardState.SignedOut:CardState.OnTrack,health==AccountHealth.SignedOut?null:45,health==AccountHealth.SignedOut?null:60,
            health==AccountHealth.SignedOut?CardLayout.Note:CardLayout.Period,Health:health);
        foreach (var time in new[] { WeeklyReset.AddSeconds(-1), WeeklyReset, WeeklyReset.AddSeconds(1) })
            yield return new("R-" + (time < WeeklyReset ? "before" : time == WeeklyReset ? "at" : "after"),"claude",Week("claude",45),40,
                time<WeeklyReset?CardState.DayOff:CardState.NotReady,45,time<WeeklyReset?100:null,time<WeeklyReset?CardLayout.Period:CardLayout.UsedOnly,Time:time);
        yield return new("D-off","claude",Week("claude",45),40,CardState.DayOff,45,100,CardLayout.Period,Time:Now.AddDays(3));
        yield return new("D-rush","claude",Week("claude",45),40,CardState.Rush,45,100,CardLayout.Period,Time:Now.AddDays(2));
        foreach (var (id,date,baseline,end) in new (string,DateTimeOffset,decimal,decimal)[]
        {
            ("first",new(2026,10,1,12,0,0,TimeSpan.Zero),0,13.63m),
            ("middle",new(2026,10,16,12,0,0,TimeSpan.Zero),120,136.36m),
            ("last",new(2026,10,31,23,59,59,TimeSpan.Zero),120,300),
            ("short-first",new(2027,2,1,12,0,0,TimeSpan.Zero),0,15),
            ("short-last",new(2027,2,28,12,0,0,TimeSpan.Zero),120,300),
            ("year-last",new(2026,12,31,23,59,59,TimeSpan.Zero),120,300)
        }) yield return new("CAL-"+id,"claude",Money(baseline),baseline,date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday?CardState.DayOff:CardState.OnTrack,baseline,end,CardLayout.Pool,Time:date);
        yield return new("M-first-observation","claude",Money(125),null,CardState.OnTrack,125,134.72m,CardLayout.Pool);
        yield return new("P-period-unknown","claude","""{"limits":[{"kind":"future_window","percent":25,"resets_at":"2026-10-12T00:00:00Z"}]}""",0,CardState.PeriodUnknown,25,null,CardLayout.UsedOnly);
        yield return new("P-short-only","claude","""{"five_hour":{"utilization":25,"resets_at":"2026-10-07T15:00:00Z"}}""",0,CardState.OnTrack,25,null,CardLayout.UsedOnly);
        yield return new("P-fortnight","codex",Week("codex",45,reset:WeeklyReset.AddDays(7)).Replace("604800","1209600",StringComparison.Ordinal),40,CardState.OnTrack,45,47.5m,CardLayout.Period);
        yield return new("B-no-cap","codex","""{"credits":{"balance":500,"has_credits":true,"unlimited":false}}""",null,CardState.NoCap,0,null,CardLayout.Note);
        yield return new("B-with-cap","codex","""{"credits":{"balance":500,"has_credits":true,"unlimited":false}}""",null,CardState.OnTrack,0,16,CardLayout.Pool,300);
        yield return new("B-unknown","codex","""{"credits":{"balance":null,"has_credits":true}}""",null,CardState.NoCap,null,null,CardLayout.Note);
        yield return new("F-CL-S-modern", "claude", """{"limits":[{"kind":"session","percent":35,"resets_at":"2026-10-07T15:00:00Z"}]}""",
            null, CardState.OnTrack, 35, null, CardLayout.UsedOnly);
        yield return new("F-CL-W-modern", "claude", """{"limits":[{"kind":"weekly_all","percent":45,"resets_at":"2026-10-12T00:00:00Z"}]}""",
            40, CardState.OnTrack, 45, 60, CardLayout.Period);
        foreach (var family in new[] { "opus", "sonnet" })
            yield return new("F-CL-M-" + family, "claude", "{\"seven_day_" + family + "\":{\"utilization\":45,\"resets_at\":\"2026-10-12T00:00:00Z\"}}",
                40, CardState.OnTrack, 45, 60, CardLayout.Period);
        yield return new("F-CL-M-scoped", "claude", """{"limits":[{"kind":"weekly_scoped","percent":45,"is_active":false,"resets_at":"2026-10-12T00:00:00Z","scope":{"model":{"display_name":"SYNTHETIC long model group / Ω with an intentionally lengthy name"}}}]}""",
            40, CardState.OnTrack, 45, 60, CardLayout.Period);
        yield return new("F-CL-X-legacy", "claude", """{"extra_usage":{"is_enabled":true,"used_credits":12550,"monthly_limit":30000,"decimal_places":2,"currency":"USD"}}""",
            120, CardState.OnTrack, 125.5m, 130, CardLayout.Pool);
        yield return new("F-CL-X-currency-mismatch", "claude", """{"spend":{"enabled":true,"used":{"amount_minor":12550,"currency":"USD","exponent":2},"limit":{"amount_minor":30000,"currency":"EUR","exponent":2}}}""",
            120, CardState.PeriodUnknown, 125.5m, null, CardLayout.Note);
        yield return new("F-CL-X-exponent-unknown", "claude", """{"extra_usage":{"is_enabled":true,"used_credits":12550,"monthly_limit":30000,"currency":"USD"}}""",
            null, CardState.PeriodUnknown, null, null, CardLayout.Note);
        foreach (var group in new[] { "chat", "completions" })
            yield return new("F-GH-" + group, "copilot", Pool(125, group: group), 120, CardState.OnTrack, 125, 130, CardLayout.Pool);
        yield return new("F-GH-timestamp", "copilot", Pool(125, reset: "2026-11-01T00:00:00+00:00"), 120, CardState.OnTrack, 125, 130, CardLayout.Pool);
        yield return new("F-CX-I", "codex", """{"spend_control":{"individual_limit":{"used_percent":45,"used":"opaque 45","limit":"opaque 100","reset_at":1793491200}}}""",
            40, CardState.OverToday, 45, 43.3m, CardLayout.Period);
        yield return new("F-CX-I-unknown", "codex", """{"spend_control":{"individual_limit":{"used":"1","limit":"2","remaining":"1"}}}""",
            null, CardState.ValueUnknown, null, null, CardLayout.Note);
        yield return new("F-CX-A", "codex", """{"additional_rate_limits":[{"limit_name":"SYNTHETIC model group / Ω","metered_feature":"synthetic-feature","normal_model_slug":"synthetic-model","rate_limit":{"secondary_window":{"used_percent":45,"limit_window_seconds":604800,"reset_at":1791763200}}}]}""",
            40, CardState.OnTrack, 45, 60, CardLayout.Period);
        yield return new("F-CX-disabled", "codex", Week("codex", 45).Replace("\"secondary_window\"", "\"allowed\":false,\"secondary_window\"", StringComparison.Ordinal),
            40, CardState.NotIncluded, 45, 60, CardLayout.Note);
        yield return new("F-AG-disabled", "antigravity", """{"groups":[{"displayName":"SYNTHETIC disabled group","buckets":[{"bucketId":"disabled","window":"weekly","disabled":true,"remainingFraction":0.8}]}]}""",
            null, CardState.NoDisplayedLimits, null, null, CardLayout.Note);
        yield return new("F-AG-unknown-window", "antigravity", """{"buckets":[{"bucketId":"future","window":"opaque period","remainingFraction":0.75,"remainingAmount":"250","resetTime":"2026-10-12T00:00:00Z"}]}""",
            null, CardState.PeriodUnknown, 25, null, CardLayout.UsedOnly);
        yield return new("F-AG-amount-only", "antigravity", """{"buckets":[{"bucketId":"future","window":"weekly","remainingAmount":"250","resetTime":"2026-10-12T00:00:00Z"}]}""",
            null, CardState.ValueUnknown, null, null, CardLayout.Note);
        foreach(var (provider,json) in new[] { ("claude","{\"seven_day\":null}"),("codex","{\"plan_type\":\"synthetic\"}"),
            ("copilot","{\"quota_snapshots\":{}}"),("antigravity","{\"groups\":[]}") })
            yield return new("E-"+provider,provider,json,null,CardState.NoDisplayedLimits,null,null,CardLayout.Note);
        foreach (var provider in new[] { "claude", "codex", "antigravity" })
        {
            yield return new("PREC-01-" + provider, provider, Week(provider, 60, 100, true), 40, CardState.FiveHourFull, 60, 60, CardLayout.FiveHourAndPeriod);
            foreach (var seconds in new[] { -1, 0, 1 })
                yield return new("WRESET-01-" + provider + "-" + (seconds < 0 ? "before" : seconds == 0 ? "at" : "after"), provider,
                    Week(provider, 45, 100, true), 40, seconds < 0 ? CardState.FiveHourFull : CardState.OnTrack,
                    45, 60, seconds < 0 ? CardLayout.FiveHourAndPeriod : CardLayout.Period,
                    Time: Now.AddHours(3).AddSeconds(seconds), Fetched: Now);
            yield return new("WRESET-02-" + provider, provider, Week(provider, 45, 0, true, shortEndOverride: Now.AddHours(8)),
                40, CardState.OnTrack, 45, 60, CardLayout.FiveHourAndPeriod, Time: Now.AddHours(3).AddSeconds(1));
            foreach (var used in new[] { 69.9m, 70, 70.1m, 89.9m, 90, 90.1m })
            {
                yield return new("PREC-04-paired-" + provider + "-" + N(used), provider, Week(provider, 45, used, true),
                    40, CardState.OnTrack, 45, 60, CardLayout.FiveHourAndPeriod);
                var single = JsonSerializer.SerializeToNode(JsonSerializer.Deserialize<object>(Week(provider, 45, used, true)))!.AsObject();
                if (provider == "claude") single.Remove("seven_day");
                else if (provider == "codex") { single["rate_limit"]!.AsObject().Remove("secondary_window"); single.Remove("credits"); }
                else single["groups"]![0]!["buckets"]!.AsArray().RemoveAt(0);
                yield return new("PREC-04-single-" + provider + "-" + N(used), provider, single.ToJsonString(), null,
                    CardState.OnTrack, used, null, CardLayout.UsedOnly);
            }
        }
        yield return new("PREC-02", "claude", Money(180.01m), 120, CardState.OverCap, 180.01m, 123.75m,
            CardLayout.Pool, 180, Now.AddDays(3));
        foreach (var money in new[] { false, true })
        foreach (var (used, state) in new[] { (money ? 66.99m : 66m, CardState.OnTrack), (67m, CardState.OnTrack),
                     (money ? 67.01m : 68m, CardState.CapClose) })
            yield return new("PREC-03-" + (money ? "money-" : "count-") + N(used), money ? "claude" : "copilot",
                money ? Money(used) : Pool(used), 60, state, used, 70, CardLayout.Pool, 240);
        foreach (var hours in new[] { 4, 6, 10 })
        {
            var friday = Now.AddDays(2);
            yield return new("WRUSH-01-" + hours, "claude", Week("claude", 45, 35, true, friday.AddHours(hours),
                    shortEndOverride: friday.AddHours(3)), 40, CardState.Rush, 45, 100, CardLayout.FiveHourAndPeriod,
                Time: friday, Estimate: true, Fit: hours / 5);
        }
        foreach (var (id, time, end) in new[]
        {
            ("spring-before", new DateTimeOffset(2026, 3, 29, 0, 59, 59, TimeSpan.Zero), 100m),
            ("spring-at", new DateTimeOffset(2026, 3, 29, 1, 0, 0, TimeSpan.Zero), 100m),
            ("fall-before", new DateTimeOffset(2026, 10, 25, 0, 59, 59, TimeSpan.Zero), 50m),
            ("fall-at", new DateTimeOffset(2026, 10, 25, 1, 0, 0, TimeSpan.Zero), 50m)
        }) yield return new("CAL-03-" + id, "claude", Money(0), 0, CardState.DayOff, 0, end, CardLayout.Pool, Time: time, Zone: "Europe/Lisbon");
    }

    private static ProviderSessionState Parse(string provider, string json, DateTimeOffset fetched)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        if (provider == "claude")
        {
            Assert.True(ClaudeQuotaParser.TryParse(bytes,fetched,out var reading));
            return new(ProviderSessionStatus.QuotaAvailable,reading!.Quota,ExtraUsage:reading.ExtraUsage);
        }
        var quota = provider switch
        {
            "codex" => CodexQuotaParser.Parse(bytes,fetched),
            "copilot" => CopilotQuotaParser.Parse(bytes,fetched),
            _ => AntigravityQuotaParser.Parse(bytes,fetched,"synthetic-tier")
        };
        return new(ProviderSessionStatus.QuotaAvailable,quota);
    }

    private static AuditInput Input(Case item, int number)
    {
        var now = item.Time ?? Now;
        var id = new Guid(number,0,0,new byte[8]);
        var fetched = item.Fetched ?? (item.Health is AccountHealth.SyncFailedStale or AccountHealth.SignInExpired ? now.AddHours(-1) : now);
        var session = Parse(item.Provider,item.Json,fetched);
        session = item.Health switch
        {
            AccountHealth.SyncFailedFresh or AccountHealth.SyncFailedStale => session with { Failure = ProviderFailureKind.NetworkFailure },
            AccountHealth.SignInExpired => session with { Status = ProviderSessionStatus.ReauthenticationRequired,Failure=ProviderFailureKind.AuthenticationRequired },
            AccountHealth.ProviderError => session with { Status = ProviderSessionStatus.RecoveryRequired,Failure=ProviderFailureKind.RecoveryRequired },
            _ => session
        };
        var account = new AccountSnapshot(id,item.Provider,item.Health!=AccountHealth.SignedOut,session,false,
            item.Health is AccountHealth.SyncFailedFresh or AccountHealth.SyncFailedStale ? now : null);
        var facts = session.Quota!.Limits!.Limits;
        var observations = new List<ReadingObservation>();
        foreach (var fact in facts)
        {
            var series = new ReadingSeriesKey(id.ToString("N"),fact.Key);
            var value = fact.Used;
            if (value is null || fact.Duration==TimeSpan.FromHours(5)) continue;
            var baseline = item.Baseline is { } start ? value is MoneyQuantity m ? (Quantity)new MoneyQuantity(checked((long)(start*(decimal)Math.Pow(10,m.Exponent!.Value))),m.Exponent,m.Currency) : new CountQuantity(start,fact.Unit) : null;
            if (baseline is not null) observations.Add(new(series,baseline,WorkCalendar.Midnight(WorkCalendar.Date(now,TimeZoneInfo.FindSystemTimeZoneById(item.Zone)),TimeZoneInfo.FindSystemTimeZoneById(item.Zone)))
            { ResetAt=fact.Reset?.At,PeriodStartedAt=fact.PeriodStart<=now?fact.PeriodStart:null,PlanType=session.Quota.PlanType });
        }
        if(item.Estimate && facts.FirstOrDefault(f=>f.Duration==TimeSpan.FromHours(5)) is { } shortFact &&
            facts.FirstOrDefault(f=>f.Duration==TimeSpan.FromDays(7)) is { } weekly)
        {
            for(var day=0;day<3;day++)
            {
                var start=Now.Date.AddDays(day-2);
                var at=new DateTimeOffset(start,TimeSpan.Zero);
                var s=new ReadingSeriesKey(id.ToString("N"),shortFact.Key);
                var w=new ReadingSeriesKey(id.ToString("N"),weekly.Key);
                foreach(var (time,shortPercent,weekPercent) in new[] { (at,0m,10m+15*day),(at.AddHours(2),50m,15m+15*day) })
                {
                    observations.Add(new(s,new CountQuantity(shortPercent,"percent"),time) { ResetAt=at.AddHours(5),PeriodStartedAt=at,PlanType=session.Quota.PlanType });
                    if(time != new DateTimeOffset(now.Date,TimeSpan.Zero)) observations.Add(new(w,new CountQuantity(weekPercent,"percent"),time)
                        { ResetAt=weekly.Reset?.At,PeriodStartedAt=weekly.PeriodStart,PlanType=session.Quota.PlanType });
                }
            }
        }
        var capFact = facts.FirstOrDefault(f=>f.Kind!=LimitKind.PercentWindow);
        var caps = item.Cap is { } cap && capFact is not null ? new[] { new StoredPersonalCap(new(id.ToString("N"),capFact.Key),
            new(capFact.Kind==LimitKind.MonetaryPool ? new MoneyQuantity((long)(cap*100),2,"USD") : new CountQuantity(cap,capFact.Unit),now)) } : [];
        return new(now,item.Zone,[account],new() { [id.ToString("N")]= "SYNTHETIC · " + item.Id },observations.ToArray(),BudgetConfiguration.Default with { Caps=caps });
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task ParserRecorderBudgetAndProjectionMatchIndependentExpectations(string id)
    {
        var item=AllCases().Single(c=>c.Id==id);
        var number = AllCases().Select(c=>c.Id).ToList().IndexOf(item.Id)+1;
        var input = Input(item,number);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(input.ZoneId);
        var root = Path.Combine(Path.GetTempPath(),"aiu-audit-corpus-"+Guid.NewGuid().ToString("N"));
        using var store = new LocalBudgetStore(root);
        try
        {
            await store.AppendAsync(input.Observations,CancellationToken.None);
            var account = input.Accounts[0];
            var capture = new CapturingStore(store);
            await new QuotaObservationRecorder(capture).RecordAsync(account.AccountId.ToString("N"),item.Provider,account.Session,CancellationToken.None);
            var data = new List<LedgerLimit>();
            foreach (var fact in account.Session.Quota!.Limits!.Limits)
            {
                var series = new ReadingSeriesKey(account.AccountId.ToString("N"),fact.Key);
                data.Add(new(fact,series,(await store.ReadAsync(series,CancellationToken.None)).Value));
            }
            var model = LiveLedgerProjection.Account(account,input.Labels[account.AccountId.ToString("N")],data,input.Configuration,input.Now,zone,null);
            var card = Assert.Single(model.Cards);
            Assert.Equal(item.State,card.State);
            Assert.Equal(item.Used,card.Figures.Used);
            Assert.Equal(item.TodayEnd,card.Figures.TodayEnd);
            Assert.Equal(item.Layout,card.Layout);
            Assert.Equal(item.Health,model.Health);
            if (item.Fit is { } fit) Assert.Equal(fit,card.FiveHour!.FitBeforeReset);
            if (item.Id.StartsWith("WRESET-01-",StringComparison.Ordinal) && !item.Id.EndsWith("before",StringComparison.Ordinal))
                Assert.Contains(card.Marks,m=>m.Kind==MarkKind.PastReset && m.ScopeLabel=="5h");
            if(item.Estimate && card.FiveHour is not null)
            {
                Assert.Equal(10,card.FiveHour.WindowShare);
                Assert.Equal((int)decimal.Floor((100-item.Used!.Value)/10),card.FiveHour.WindowsLeftInPeriod);
            }
            // Startup must replay the actual recorded observations, including the first observation today.
            // A cached startup intentionally does not recapture the current account snapshot.
            input = input with { Observations = [.. input.Observations, .. capture.Observations] };
            var roundtrip = JsonSerializer.Deserialize(JsonSerializer.Serialize(input,AuditJson.Default.AuditInput),AuditJson.Default.AuditInput)!;
            Assert.Equal(account.Session.Quota.Limits.Limits,roundtrip.Accounts[0].Session.Quota!.Limits!.Limits);
            using var replayStore = new LocalBudgetStore(Path.Combine(root,"replayed"));
            await replayStore.AppendAsync(roundtrip.Observations,CancellationToken.None);
            var replayData = new List<LedgerLimit>();
            foreach(var fact in roundtrip.Accounts[0].Session.Quota!.Limits!.Limits)
            {
                var series = new ReadingSeriesKey(account.AccountId.ToString("N"),fact.Key);
                replayData.Add(new(fact,series,(await replayStore.ReadAsync(series,CancellationToken.None)).Value));
            }
            var replayCard = Assert.Single(LiveLedgerProjection.Account(roundtrip.Accounts[0],input.Labels[account.AccountId.ToString("N")],replayData,
                input.Configuration,input.Now,zone,null).Cards);
            Assert.Equal(item.State,replayCard.State);
            Assert.Equal(item.Used,replayCard.Figures.Used);
            Assert.Equal(item.TodayEnd,replayCard.Figures.TodayEnd);
            Assert.Equal(item.Layout,replayCard.Layout);
            replayStore.Dispose();
            Export(item,input with { ExpectedStates = new() { [card.CardId]=item.State } });
        }
        finally { store.Dispose(); if(Directory.Exists(root)) Directory.Delete(root,true); }
    }
    private sealed class CapturingStore(IReadingSeriesStore store) : IReadingSeriesStore
    {
        public List<ReadingObservation> Observations { get; } = [];
        public async Task<StoreWrite> AppendAsync(IReadOnlyList<ReadingObservation> observations,CancellationToken token)
        {
            var result = await store.AppendAsync(observations,token);
            Observations.AddRange(observations);
            return result;
        }
        public Task<StoreRead<IReadOnlyList<ReadingRun>>> ReadAsync(ReadingSeriesKey series,CancellationToken token) => store.ReadAsync(series,token);
    }
    private static void Export(Case item,AuditInput input)
    {
        var directory=Environment.GetEnvironmentVariable("AIU_AUDIT_INPUT_DIRECTORY");
        if(string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory,item.Id+".json"),JsonSerializer.Serialize(input,AuditJson.Default.AuditInput));
        File.WriteAllText(Path.Combine(directory,item.Id+".provider.json"),item.Json);
        File.WriteAllText(Path.Combine(directory,item.Id+".expectation.json"),JsonSerializer.Serialize(new
        { item.Id,item.Provider,input.Now,item.Zone,item.Baseline,State=item.State.ToString(),item.Used,item.TodayEnd,
            Layout=item.Layout.ToString(),item.Cap,Health=item.Health.ToString(),item.Estimate,
            Test="AuditScenarioTests.ParserRecorderBudgetAndProjectionMatchIndependentExpectations",Deterministic="PASS" }));
    }
}
