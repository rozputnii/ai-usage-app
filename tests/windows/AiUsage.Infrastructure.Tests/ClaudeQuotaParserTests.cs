using System.Text;
using System.Text.Json;
using AiUsage.Core.Providers.Claude;
using AiUsage.Infrastructure.Providers.Claude;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class ClaudeQuotaParserTests
{
    private static readonly DateTimeOffset FetchedAt = new(2026, 9, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ParsesSourceDerivedMixedLegacyAndCurrentUsage()
    {
        var reading = ParseFixture("claude-usage.synthetic.json");

        Assert.Equal(FetchedAt, reading.Quota.FetchedAt);
        Assert.Null(reading.Quota.PlanType);
        Assert.Null(reading.Quota.Credits);
        Assert.Null(reading.Quota.AvailableResetCredits);
        Assert.Null(reading.Quota.SpendControlReached);
        Assert.Null(reading.Quota.LimitReachedType);

        var fiveHour = Assert.Single(reading.Quota.Groups, group => group.Id == "claude:5h");
        var fiveHourWindow = Assert.Single(fiveHour.Windows);
        Assert.Equal(25, fiveHourWindow.UsedPercent);
        Assert.Equal(75, fiveHourWindow.RemainingPercent);
        Assert.Equal(TimeSpan.FromHours(5), fiveHourWindow.Duration);
        Assert.Equal(new DateTimeOffset(2026, 9, 14, 14, 0, 0, TimeSpan.FromHours(2)), fiveHourWindow.ResetsAt);
        Assert.False(fiveHour.Allowed);

        var weekly = Assert.Single(reading.Quota.Groups, group => group.Id == "claude:7d");
        Assert.Equal(40, Assert.Single(weekly.Windows).UsedPercent);
        Assert.Equal(60, Assert.Single(weekly.Windows).RemainingPercent);

        var scoped = Assert.Single(reading.Quota.Groups, group => group.Name == "Family / Fable");
        Assert.Equal("weekly_scoped", scoped.MeteredFeature);
        Assert.False(scoped.Allowed);
        Assert.Equal(55, Assert.Single(scoped.Windows).UsedPercent);
        Assert.Equal(TimeSpan.FromDays(7), Assert.Single(scoped.Windows).Duration);

        Assert.NotNull(reading.ExtraUsage);
        Assert.True(reading.ExtraUsage!.Enabled);
        Assert.Equal(ClaudeExtraUsageSource.Current, reading.ExtraUsage.Source);
        Assert.Equal(12345, reading.ExtraUsage.Used!.AmountMinor);
        Assert.Equal(2, reading.ExtraUsage.Used.Exponent);
        Assert.Equal("EUR", reading.ExtraUsage.Used.Currency);
        Assert.True(reading.ExtraUsage.HasExplicitNullLimit);
        Assert.Null(reading.ExtraUsage.Limit);
    }

    [Fact]
    public void JsonElementOverloadUsesTheSameNormalizedContract()
    {
        using var document = JsonDocument.Parse("""
            { "five_hour": { "utilization": 1, "resets_at": "2026-09-14T13:00:00Z" } }
            """);

        Assert.True(ClaudeQuotaParser.TryParse(document.RootElement, FetchedAt, out var reading));
        Assert.NotNull(reading);
        var window = Assert.Single(Assert.Single(reading.Quota.Groups).Windows);
        Assert.Equal(1, window.UsedPercent);
        Assert.Equal(99, window.RemainingPercent);
    }

    [Fact]
    public void LegacyKnownPoolWinsOverModernDuplicate()
    {
        var reading = Parse("""
            {
              "five_hour": { "utilization": 12, "resets_at": "2026-09-14T13:00:00Z" },
              "seven_day": null,
              "limits": [
                { "kind": "session", "percent": 88, "resets_at": "2026-09-14T14:00:00Z", "is_active": false },
                { "kind": "weekly_all", "percent": 33, "resets_at": "2026-09-20T00:00:00Z" }
              ]
            }
            """);

        var fiveHour = Assert.Single(reading.Quota.Groups, group => group.Id == "claude:5h");
        Assert.Equal(12, Assert.Single(fiveHour.Windows).UsedPercent);
        Assert.DoesNotContain(reading.Quota.Groups, group => group.MeteredFeature == "session");

        var weekly = Assert.Single(reading.Quota.Groups, group => group.Id == "claude:7d");
        Assert.Equal(33, Assert.Single(weekly.Windows).UsedPercent);
    }

    [Fact]
    public void PreservesScopedUnknownInactiveAndCollidingRows()
    {
        var reading = Parse("""
            {
              "limits": [
                { "kind": "weekly_scoped", "percent": 5, "scope": { "model": { "display_name": "Same Name" } }, "is_active": false },
                { "kind": "weekly_scoped", "percent": 6, "scope": { "model": { "display_name": "Same Name" } }, "is_active": true },
                { "kind": "weekly_scoped", "percent": 7, "scope": { "model": { "display_name": "Same/Name" } } },
                { "kind": "future_kind", "percent": 8, "scope": { "model": { "display_name": "Opaque ?? Name" } } },
                { "kind": "future_kind", "percent": 9, "scope": { "model": { "display_name": "Opaque ?? Name" } } }
              ]
            }
            """);

        var sameName = reading.Quota.Groups.Where(group => group.Name == "Same Name").ToArray();
        Assert.Equal(2, sameName.Length);
        Assert.Equal(2, sameName.Select(group => group.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(sameName, group => group.Allowed == false && Assert.Single(group.Windows).UsedPercent == 5);
        Assert.Contains(sameName, group => group.Allowed == true && Assert.Single(group.Windows).UsedPercent == 6);

        var collidingSlug = Assert.Single(reading.Quota.Groups, group => group.Name == "Same/Name");
        Assert.Equal(7, Assert.Single(collidingSlug.Windows).UsedPercent);

        var unknown = reading.Quota.Groups.Where(group => group.MeteredFeature == "future_kind").ToArray();
        Assert.Equal(2, unknown.Length);
        Assert.Equal(2, unknown.Select(group => group.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.All(unknown, group => Assert.Null(Assert.Single(group.Windows).Duration));
        Assert.Contains(unknown, group => group.Name == "Opaque ?? Name" && Assert.Single(group.Windows).UsedPercent == 8);
    }

    [Fact]
    public void KeepsNullMissingInvalidAndExhaustedPercentagesDistinct()
    {
        var reading = Parse("""
            {
              "five_hour": null,
              "seven_day": { "resets_at": "2026-09-20T00:00:00Z" },
              "seven_day_opus": { "utilization": null },
              "seven_day_sonnet": { "utilization": 100 },
              "limits": [
                { "kind": "future_a", "percent": -1 },
                { "kind": "future_b", "percent": 101 },
                { "kind": "future_c", "percent": "50" },
                { "kind": "future_d", "percent": 0 }
              ]
            }
            """);

        Assert.DoesNotContain(reading.Quota.Groups, group => group.Id == "claude:5h");
        var weekly = Assert.Single(reading.Quota.Groups, group => group.Id == "claude:7d");
        var weeklyWindow = Assert.Single(weekly.Windows);
        Assert.Null(weeklyWindow.UsedPercent);
        Assert.Null(weeklyWindow.RemainingPercent);
        Assert.Equal(new DateTimeOffset(2026, 9, 20, 0, 0, 0, TimeSpan.Zero), weeklyWindow.ResetsAt);

        var opus = Assert.Single(reading.Quota.Groups, group => group.Id == "claude:7d:opus");
        var opusWindow = Assert.Single(opus.Windows);
        Assert.Null(opusWindow.UsedPercent);
        Assert.Null(opusWindow.RemainingPercent);
        var sonnet = Assert.Single(reading.Quota.Groups, group => group.Id == "claude:7d:sonnet");
        var sonnetWindow = Assert.Single(sonnet.Windows);
        Assert.Equal(100, sonnetWindow.UsedPercent);
        Assert.Equal(0, sonnetWindow.RemainingPercent);

        Assert.All(new[] { "future_a", "future_b", "future_c" }, kind =>
        {
            var window = Assert.Single(Assert.Single(reading.Quota.Groups, group => group.MeteredFeature == kind).Windows);
            Assert.Null(window.UsedPercent);
            Assert.Null(window.RemainingPercent);
        });
        Assert.Equal(0, Assert.Single(reading.Quota.Groups, group => group.MeteredFeature == "future_d").Windows[0].UsedPercent);
        Assert.Equal(100, Assert.Single(reading.Quota.Groups, group => group.MeteredFeature == "future_d").Windows[0].RemainingPercent);
    }

    [Fact]
    public void RequiresTimezoneAndPreservesResetInstant()
    {
        var reading = Parse("""
            {
              "five_hour": { "utilization": 10, "resets_at": "2026-09-14T14:00:00+02:30" },
              "seven_day": { "utilization": 20, "resets_at": "2026-09-20T00:00:00" },
              "limits": [
                { "kind": "future_valid", "percent": 30, "resets_at": "2026-09-21T00:00:00-07:00" },
                { "kind": "future_bad", "percent": 40, "resets_at": "not-a-reset" }
              ]
            }
            """);

        Assert.Equal(new DateTimeOffset(2026, 9, 14, 14, 0, 0, TimeSpan.FromHours(2.5)),
            Assert.Single(reading.Quota.Groups, group => group.Id == "claude:5h").Windows[0].ResetsAt);
        Assert.Null(Assert.Single(reading.Quota.Groups, group => group.Id == "claude:7d").Windows[0].ResetsAt);
        Assert.Equal(new DateTimeOffset(2026, 9, 21, 0, 0, 0, TimeSpan.FromHours(-7)),
            Assert.Single(reading.Quota.Groups, group => group.MeteredFeature == "future_valid").Windows[0].ResetsAt);
        Assert.Null(Assert.Single(reading.Quota.Groups, group => group.MeteredFeature == "future_bad").Windows[0].ResetsAt);
    }

    [Fact]
    public void PreservesCurrentMoneyWhenDisabledAndNullLimit()
    {
        var reading = Parse("""
            {
              "limits": [],
              "spend": {
                "enabled": false,
                "used": { "amount_minor": 1250, "currency": "EUR", "exponent": 2 },
                "limit": null
              },
              "extra_usage": {
                "is_enabled": true,
                "used_credits": 9,
                "monthly_limit": 10,
                "decimal_places": 0,
                "currency": "USD"
              }
            }
            """);

        var extra = reading.ExtraUsage;
        Assert.NotNull(extra);
        Assert.False(extra.Enabled);
        Assert.Equal(ClaudeExtraUsageSource.Current, extra.Source);
        Assert.Equal(1250, extra.Used!.AmountMinor);
        Assert.Equal(2, extra.Used.Exponent);
        Assert.Equal("EUR", extra.Used.Currency);
        Assert.True(extra.HasExplicitNullLimit);
        Assert.Null(extra.Limit);
    }

    [Fact]
    public void KeepsLegacyMoneyUnitsUnknownWhenOmittedAndDoesNotMergeMismatchedAmounts()
    {
        var reading = Parse("""
            {
              "extra_usage": { "is_enabled": true, "used_credits": 1250, "monthly_limit": 2500 },
              "limits": [],
              "spend": null
            }
            """);

        var extra = reading.ExtraUsage;
        Assert.NotNull(extra);
        Assert.True(extra.Enabled);
        Assert.Equal(ClaudeExtraUsageSource.Legacy, extra.Source);
        Assert.Equal(1250, extra.Used!.AmountMinor);
        Assert.Null(extra.Used.Exponent);
        Assert.Null(extra.Used.Currency);
        Assert.Equal(2500, extra.Limit!.AmountMinor);
        Assert.Null(extra.Limit.Exponent);
        Assert.Null(extra.Limit.Currency);
        Assert.False(extra.HasExplicitNullLimit);

        var mismatched = Parse("""
            {
              "spend": {
                "enabled": true,
                "used": { "amount_minor": 100, "currency": "USD", "exponent": 2 },
                "limit": { "amount_minor": 200, "currency": "EUR", "exponent": 3 }
              }
            }
            """);
        Assert.Equal("USD", mismatched.ExtraUsage!.Used!.Currency);
        Assert.Equal("EUR", mismatched.ExtraUsage.Limit!.Currency);
        Assert.Equal(2, mismatched.ExtraUsage.Used.Exponent);
        Assert.Equal(3, mismatched.ExtraUsage.Limit.Exponent);
        Assert.Null(mismatched.Quota.Groups.SingleOrDefault(group => group.Id == "claude:extra"));

        var invalid = Parse("""
            {
              "spend": { "enabled": true, "used": { "amount_minor": -1, "currency": "USD", "exponent": 2 } }
            }
            """);
        Assert.Null(invalid.ExtraUsage!.Used!.AmountMinor);
        Assert.Equal(2, invalid.ExtraUsage.Used.Exponent);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("{\"limits\":\"synthetic-secret\"}")]
    [InlineData("{\"five_hour\":\"synthetic-secret\"}")]
    [InlineData("{not-json-synthetic-secret")]
    [InlineData("{\"spend\":42}")]
    [InlineData("{\"limits\":[{\"kind\":\"session\"},\"synthetic-secret\"]}")]
    [InlineData("{\"limits\":[{\"percent\":50}]}")]
    public void CriticalMalformedResponsesReturnFalseWithoutEchoingInput(string payload)
    {
        var parsed = ClaudeQuotaParser.TryParse(Encoding.UTF8.GetBytes(payload), FetchedAt, out var reading);

        Assert.False(parsed);
        Assert.Null(reading);
    }

    [Fact]
    public void RejectsOversizedAndTooDeepPayloads()
    {
        var oversized = Encoding.UTF8.GetBytes("{\"limits\":[] ,\"padding\":\"" + new string('x', 1024 * 1024) + "\"}");
        Assert.False(ClaudeQuotaParser.TryParse(oversized, FetchedAt, out var oversizedReading));
        Assert.Null(oversizedReading);

        var deep = "{\"limits\":[{\"kind\":\"future\",\"scope\":" +
            string.Concat(Enumerable.Repeat("{\"model\":", 40)) + "null" +
            string.Concat(Enumerable.Repeat("}", 40)) + "]}";
        Assert.False(ClaudeQuotaParser.TryParse(Encoding.UTF8.GetBytes(deep), FetchedAt, out var deepReading));
        Assert.Null(deepReading);
    }

    private static ClaudeQuotaReading ParseFixture(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
        return Parse(File.ReadAllBytes(path));
    }

    private static ClaudeQuotaReading Parse(string json)
    {
        Assert.True(ClaudeQuotaParser.TryParse(Encoding.UTF8.GetBytes(json), FetchedAt, out var reading));
        Assert.NotNull(reading);
        return reading;
    }

    private static ClaudeQuotaReading Parse(byte[] json)
    {
        Assert.True(ClaudeQuotaParser.TryParse(json, FetchedAt, out var reading));
        Assert.NotNull(reading);
        return reading;
    }
}
