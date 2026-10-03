using System.Globalization;
using System.Text.Json;
using AiUsage.Core.Budget;
using AiUsage.Core.Providers.Claude;
using AiUsage.Core.Usage;

namespace AiUsage.Infrastructure.Providers;

/// <summary>Normalized facts shared by live parsers and the conservative v1 reader.</summary>
internal static class QuotaLimitMapping
{
    internal static LimitSnapshot FromLegacy(string provider, QuotaSnapshot quota, ClaudeExtraUsage? extra = null)
    {
        List<LimitFacts> facts = [];
        foreach (var group in quota.Groups)
        foreach (var window in group.Windows)
        {
            var family = provider switch
            {
                "codex" => group.Id == "codex" ? window.Id == "primary" ? "CX-P" : "CX-S" : "CX-A",
                "copilot" => group.Id switch { "chat" => "GH-C", "completions" => "GH-I", "premium_interactions" => "GH-P", _ => "GH-unknown" },
                "antigravity" => window.Duration == TimeSpan.FromHours(5) ? "AG-5" : window.Duration == TimeSpan.FromDays(7) ? "AG-W" : "AG-unknown",
                "claude" => group.Id switch { "claude:5h" => "CL-S", "claude:7d" => "CL-W", _ => "CL-M" },
                _ => throw new ArgumentException("Unsupported provider.", nameof(provider))
            };
            var pair = Pair(group.Id, window.Id);
            var scope = group.Id switch { "claude:7d:opus" => "Opus", "claude:7d:sonnet" => "Sonnet", _ => pair };
            var key = new LimitKey(provider, family, provider == "claude" && family == "CL-M" ? scope : pair);
            var reset = window.ResetsAt is { } at ? new ResetFact(at, ValueOrigin.Provider, ResetMeaning.Replenish,
                ResetPrecision.Unknown, ResetZone.Unknown) : null;
            var used = Percent(window.UsedPercent);
            var remaining = Percent(window.RemainingPercent);
            var amount = window.Amount;
            var countable = provider == "copilot";
            var limit = window.Unlimited == true ? LimitValue.Unlimited : amount?.Limit is { } maximum
                ? LimitValue.Finite(new CountQuantity(maximum, amount.Unit)) : LimitValue.Unknown;
            facts.Add(new(key, countable ? LimitKind.CountablePool : LimitKind.PercentWindow,
                countable ? amount?.Unit ?? "unknown" : "percent", countable ? limit : LimitValue.NotApplicable)
            {
                Used = countable ? Count(amount?.Used, amount?.Unit ?? "unknown") : Count(used, "percent"),
                Remaining = countable ? Count(amount?.Remaining, amount?.Unit ?? "unknown") : Count(remaining, "percent"),
                UsedPercent = used, RemainingPercent = remaining,
                RemainingOrigin = provider is "claude" or "codex" ? ValueOrigin.Derived : ValueOrigin.Provider,
                ReportedLimit = countable ? Count(amount?.Limit, amount?.Unit ?? "unknown") : null,
                SecondaryAmount = provider == "antigravity" ? Count(amount?.Remaining, "unknown") : null,
                Reset = reset, Duration = window.Duration, IsMonthly = countable,
                Allowed = group.Allowed, LimitReached = group.LimitReached,
                OveragePermitted = window.SourceDetails?.OveragePermitted, SourceDetails = window.SourceDetails,
                LegacyKey = new(provider, "legacy-window-v1", pair)
            });
        }
        if (provider == "codex" && quota.Credits is { } credits)
            facts.Add(new(new(provider, "CX-B", "credits"), LimitKind.CountablePool, "credits",
                credits.Unlimited == true ? LimitValue.Unlimited : LimitValue.Unknown)
            {
                Remaining = Count(credits.Balance, "credits"), HasCredits = credits.HasCredits,
                AllowsCalendarFallback = true, LegacyKey = new(provider, "legacy-balance-v1", "credits")
            });
        if (provider == "claude" && extra is not null)
        {
            var used = Money(extra.Used);
            var limit = Money(extra.Limit);
            facts.Add(new(new(provider, "CL-X", "extra-usage"), LimitKind.MonetaryPool,
                used?.Currency ?? limit?.Currency ?? "unknown", extra.HasExplicitNullLimit ? LimitValue.ExplicitNull :
                    limit is not null ? LimitValue.Finite(limit) : LimitValue.Unknown)
            {
                Used = used, Remaining = QuantityMath.Subtract(limit, used), RemainingOrigin = ValueOrigin.Derived,
                Enabled = extra.Enabled, AllowsCalendarFallback = true,
                LegacyKey = new(provider, "legacy-extra-v1", "extra-usage")
            });
        }
        return new(quota.FetchedAt, quota.PlanType, SnapshotSource.ProviderApi, "quota-v1-migrated", facts);
    }

    internal static LimitSnapshot Live(string provider, QuotaSnapshot quota, JsonElement root, ClaudeExtraUsage? extra = null)
    {
        var snapshot = FromLegacy(provider, quota, extra);
        var facts = snapshot.Limits.Select(f => WithStart(f with
        {
            Reset = f.Reset is { } reset ? reset with { Precision = ResetPrecision.Instant, Zone = ResetZone.Explicit } : null
        })).ToList();
        if (provider == "copilot")
        {
            var reset = ParseReset(Text(Property(root, "quota_reset_date")));
            for (int i = 0; i < facts.Count; i++)
            {
                var group = quota.Groups[i];
                var entitlement = Property(Property(Property(root, "quota_snapshots"), group.Id), "entitlement");
                var remaining = Number(Property(Property(Property(root, "quota_snapshots"), group.Id), "percent_remaining"));
                remaining = remaining is >= 0 and <= 100 ? remaining : null;
                facts[i] = WithStart(facts[i] with
                {
                    RemainingPercent = remaining, UsedPercent = remaining is { } percent ? 100 - percent : null,
                    Reset = reset, Limit = facts[i].Limit.State == LimitValueState.Unknown && entitlement.ValueKind == JsonValueKind.Null
                        ? LimitValue.ExplicitNull : facts[i].Limit
                });
            }
        }
        if (provider == "antigravity")
        {
            var groups = Property(root, "groups");
            var buckets = groups.ValueKind == JsonValueKind.Array && groups.GetArrayLength() > 0
                ? groups.EnumerateArray().SelectMany(g => Elements(Property(g, "buckets"))) : Elements(Property(root, "buckets"));
            var active = buckets.Where(b => Property(b, "disabled").ValueKind != JsonValueKind.True).ToArray();
            for (int i = 0; i < facts.Count; i++)
                facts[i] = WithStart(facts[i] with { Reset = ParseReset(Text(Property(active[i], "resetTime"))) });
        }
        if (provider == "codex")
        {
            var individual = Property(Property(root, "spend_control"), "individual_limit");
            if (individual.ValueKind == JsonValueKind.Object)
            {
                var used = Number(Property(individual, "used_percent"));
                var remaining = Number(Property(individual, "remaining_percent"));
                used = used is >= 0 and <= 100 ? used : null;
                remaining = remaining is >= 0 and <= 100 ? remaining : used is { } u ? 100 - u : null;
                facts.Add(WithStart(new(new("codex", "CX-I", "individual_limit"), LimitKind.PercentWindow, "percent", LimitValue.NotApplicable)
                {
                    UsedPercent = used, RemainingPercent = remaining, Used = Count(used, "percent"), Remaining = Count(remaining, "percent"),
                    RemainingOrigin = Number(Property(individual, "remaining_percent")) is >= 0 and <= 100 ? ValueOrigin.Provider : ValueOrigin.Derived,
                    SecondaryAmounts = new(Text(Property(individual, "used")), Text(Property(individual, "limit")), Text(Property(individual, "remaining"))),
                    Reset = EpochReset(individual, quota.FetchedAt), IsMonthly = true
                }));
            }
        }
        if (provider == "claude")
        {
            // Slugged v1 IDs are not native scope identities. Rebuild scoped facts from
            // the source's exact name and replace only the matching known legacy scope.
            facts.RemoveAll(f => f.Key.Family == "CL-M" && f.Key.NativeDiscriminator is not ("Opus" or "Sonnet"));
            var ordinal = 0;
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in Elements(Property(root, "limits")))
            {
                ordinal++;
                var kind = Text(Property(item, "kind"));
                if (kind is "session" or "weekly_all") continue;
                var scope = Text(Property(Property(Property(item, "scope"), "model"), "display_name"));
                var family = kind == "weekly_scoped" ? "CL-M" : "CL-unknown";
                var discriminator = scope is null ? Pair(kind ?? "unknown", ordinal.ToString(CultureInfo.InvariantCulture))
                    : kind == "weekly_scoped" ? scope : Pair(kind ?? "unknown", scope);
                // Duplicate opaque scopes have ambiguous identities; keep each separate.
                var native = discriminator;
                while (!keys.Add(Pair(family, native))) native = Pair(discriminator, ordinal++.ToString(CultureInfo.InvariantCulture));
                if (kind == "weekly_scoped") facts.RemoveAll(f => f.Key.Family == "CL-M" && f.Key.NativeDiscriminator == native);
                var used = Number(Property(item, "percent"));
                used = used is >= 0 and <= 100 ? used : null;
                facts.Add(WithStart(new(new("claude", family, native),
                    LimitKind.PercentWindow, "percent", LimitValue.NotApplicable)
                {
                    UsedPercent = used, RemainingPercent = used is { } u ? 100 - u : null,
                    Used = Count(used, "percent"), Remaining = Count(used is { } p ? 100 - p : null, "percent"),
                    RemainingOrigin = ValueOrigin.Derived,
                    Duration = kind == "weekly_scoped" ? TimeSpan.FromDays(7) : null,
                    Reset = ParseReset(Text(Property(item, "resets_at")), requireZone: true)
                }));
            }
        }
        return snapshot with { SourceVersion = "quota-v2", Limits = facts.AsReadOnly() };
    }

    internal static LimitFacts WithStart(LimitFacts facts)
    {
        if (facts.Reset is not { } reset) return facts;
        DateTimeOffset? start = null;
        try { start = facts.IsMonthly ? reset.At.ToUniversalTime().AddMonths(-1) : facts.Duration is { } duration ? reset.At - duration : null; }
        catch (ArgumentOutOfRangeException) { }
        return facts with { PeriodStart = start, PeriodStartOrigin = start is null ? null : facts.IsMonthly ? ValueOrigin.Assumed : ValueOrigin.Derived };
    }

    internal static ResetFact? ParseReset(string? text, bool requireZone = false)
    {
        if (text is null) return null;
        var dateOnly = DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        var timeStart = text.IndexOf('T');
        if (timeStart < 0) timeStart = text.IndexOf(' ');
        var explicitZone = text.EndsWith('Z') || text.EndsWith('z') ||
            (timeStart >= 0 && (text.LastIndexOf('+') > timeStart || text.LastIndexOf('-') > timeStart));
        if (requireZone && !explicitZone) return null;
        return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var at)
            ? new(at, ValueOrigin.Provider, ResetMeaning.Replenish, dateOnly ? ResetPrecision.Date : ResetPrecision.Instant,
                explicitZone ? ResetZone.Explicit : ResetZone.AssumedUtc) : null;
    }

    private static ResetFact? EpochReset(JsonElement value, DateTimeOffset fetchedAt)
    {
        DateTimeOffset? at = null;
        var absolute = Number(Property(value, "reset_at"));
        if (absolute is >= -62135596800 and <= 253402300799 && decimal.Truncate(absolute.Value) == absolute)
            at = DateTimeOffset.FromUnixTimeSeconds((long)absolute.Value);
        var relative = Number(Property(value, "reset_after_seconds"));
        if (at is null && relative is >= 0)
        {
            try { at = fetchedAt.AddSeconds((double)relative.Value); }
            catch (ArgumentOutOfRangeException) { }
        }
        return at is { } reset ? new(reset, ValueOrigin.Provider, ResetMeaning.Replenish) : null;
    }

    internal static string Pair(string first, string second) => JsonSerializer.Serialize(new[] { first, second });
    private static IEnumerable<JsonElement> Elements(JsonElement value) => value.ValueKind == JsonValueKind.Array ? value.EnumerateArray() : [];
    private static CountQuantity? Count(decimal? amount, string unit) => amount is { } value ? new(value, unit) : null;
    private static MoneyQuantity? Money(ClaudeMoneyAmount? money) => money?.AmountMinor is { } value ? new(value, money.Exponent, money.Currency) : null;
    private static decimal? Percent(double? value) => value is >= 0 and <= 100 ? (decimal)value : null;
    private static JsonElement Property(JsonElement root, string key) => root.ValueKind == JsonValueKind.Object && root.TryGetProperty(key, out var value) ? value : default;
    private static string? Text(JsonElement value) => value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static decimal? Number(JsonElement value) => value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number) ? number : null;
}
