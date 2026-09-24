using AiUsage.Features.Accounts;
using AiUsage.Features.Presentation;
using Xunit;

namespace AiUsage.Presentation.Tests;

/// <summary>F03 and F08 semantics plus the D1/D2/D4 resolutions.</summary>
public sealed class QuotaSemanticsTests
{
    private static QuotaWindowViewModel Display(TestHost host, string accountId, string windowId)
    {
        var account = host.Account(accountId);
        var group = account.Contexts.SelectMany(c => c.Groups).First(g => g.Windows.Any(w => w.Id == windowId));
        var vm = new QuotaWindowViewModel(windowId);
        vm.Update(account, group, group.Windows.First(w => w.Id == windowId), host.Usage.Current.Preferences, host.Format);
        return vm;
    }

    [Fact]
    public void F03MeasurementStatesStayDistinctAndUnknownIsNeverAZeroMeter()
    {
        using var host = new TestHost("F03");
        var known = Display(host, "demo-sem-1", "sem-1");
        var unknown = Display(host, "demo-sem-1", "sem-2");
        var unlimited = Display(host, "demo-sem-1", "sem-3");
        var exhausted = Display(host, "demo-sem-1", "sem-4");
        var unavailable = Display(host, "demo-sem-1", "sem-5");

        Assert.Equal((MeterKind.Bar, "72 %", "left"), (known.Kind, known.ValueText, known.UnitText));
        Assert.Equal(0.72, known.Fraction, 3);
        Assert.Equal((MeterKind.Hatch, "Unknown", 0d), (unknown.Kind, unknown.ValueText, unknown.Fraction));
        Assert.Null(host.Window("demo-sem-1", "sem-2").RemainingPercent);
        Assert.Equal((MeterKind.Unlimited, "Unlimited"), (unlimited.Kind, unlimited.ValueText));
        Assert.DoesNotContain("%", unlimited.ValueText);
        Assert.Equal((MeterKind.Bar, "0 %", QuotaSeverity.Exhausted, "●"), (exhausted.Kind, exhausted.ValueText, exhausted.Severity, exhausted.Glyph));
        Assert.Equal((MeterKind.Hatch, "Unavailable"), (unavailable.Kind, unavailable.ValueText));
        Assert.NotEqual(unknown.StateWord, unavailable.StateWord);
        // Status never relies on colour alone: the accessible name carries the state word.
        Assert.Contains("Exhausted", exhausted.AccessibleName);
        Assert.Contains("No measurement", unknown.AccessibleName);
    }

    [Fact]
    public void F03StaleAccountKeepsItsObservationTimestamp()
    {
        using var host = new TestHost("F03");
        var pill = new StatusPillViewModel();
        pill.Update(host.Account("demo-sem-1"), host.Format);
        Assert.Equal(PillTone.Warning, pill.Tone);
        Assert.Equal("◷ Stale · cached Sep 15, 6:00 AM", pill.Text);
        Assert.False(pill.IsQuietFresh);
    }

    [Fact]
    public void D1DefaultThresholdsAreRemaining25_10_0WithSmallestNonzeroCritical()
    {
        Assert.Equal([25, 10, 0], Preferences.DefaultRemainingThresholds);
        WindowItem Known(double remaining) => new("w", "w", remaining, 100 - remaining, ValueState.Known, null, null, null, false);
        int[] defaults = [25, 10, 0];
        Assert.Equal(QuotaSeverity.Normal, QuotaRules.Classify(Known(26), defaults));
        Assert.Equal(QuotaSeverity.Warning, QuotaRules.Classify(Known(25), defaults));
        Assert.Equal(QuotaSeverity.Warning, QuotaRules.Classify(Known(20), defaults));
        Assert.Equal(QuotaSeverity.Critical, QuotaRules.Classify(Known(10), defaults));
        Assert.Equal(QuotaSeverity.Critical, QuotaRules.Classify(Known(8), defaults));
        Assert.Equal(QuotaSeverity.Exhausted, QuotaRules.Classify(Known(0), defaults));
        Assert.Equal(QuotaSeverity.None, QuotaRules.Classify(new("u", "u", null, null, ValueState.Unknown, null, null, null, false), defaults));
        // A single nonzero threshold is critical, following the design's "last threshold is critical" rule.
        Assert.Equal(QuotaSeverity.Critical, QuotaRules.Classify(Known(40), [50, 0]));
    }

    [Fact]
    public void D1RuleResolutionFollowsWindowAccountWindowTypeProviderGlobalWithoutMutatingParents()
    {
        using var host = new TestHost();
        var account = host.Account("demo-codex-1");
        var window = host.Window("demo-codex-1", "c1-w1");
        List<NotificationRule> rules = [Preferences.DefaultGlobalRule];
        Assert.Equal(RuleScope.Global, QuotaRules.Resolve(rules, account, window).Source);
        rules.Add(new(RuleScope.Provider, "codex", false, [40], true));
        Assert.Equal(RuleScope.Provider, QuotaRules.Resolve(rules, account, window).Source);
        rules.Add(new(RuleScope.WindowType, QuotaRules.WindowTypeKey("codex", "5-hour window"), false, [60, 30], true));
        Assert.Equal(RuleScope.WindowType, QuotaRules.Resolve(rules, account, window).Source);
        rules.Add(new(RuleScope.Account, account.Id, false, [70], true));
        Assert.Equal(RuleScope.Account, QuotaRules.Resolve(rules, account, window).Source);
        rules.Add(new(RuleScope.Window, window.Id, false, [80, 5], true));
        Assert.Equal([80, 5], QuotaRules.Resolve(rules, account, window).Remaining);
        Assert.Equal([25, 10, 0], rules[0].RemainingThresholds);
        rules.Add(new(RuleScope.Window, window.Id, true, [], true));
        rules.RemoveAt(rules.Count - 2);
        Assert.Equal(RuleScope.Account, QuotaRules.Resolve(rules, account, window).Source);
    }

    [Fact]
    public void D2UsedDisplayFlipsValuesAndTicksWithoutChangingMeasurements()
    {
        using var host = new TestHost();
        var remaining = Display(host, "demo-codex-2", "c2-w1");
        Assert.Equal(("20 %", "left", "▲"), (remaining.ValueText, remaining.UnitText, remaining.Glyph));
        Assert.Equal([0.25, 0.10], remaining.Ticks);
        host.State.World.UsageDisplay = UsageDisplay.Used;
        host.State.Publish();
        var used = Display(host, "demo-codex-2", "c2-w1");
        Assert.Equal(("80 %", "used", 0.80), (used.ValueText, used.UnitText, used.Fraction));
        Assert.Equal([0.75, 0.90], used.Ticks);
        Assert.Equal(20, host.Window("demo-codex-2", "c2-w1").RemainingPercent);
        Assert.Equal(QuotaSeverity.Warning, used.Severity);
    }

    [Fact]
    public void F08PassedResetReadsAwaitingUpdateAndStaysExhaustedUntilANewObservation()
    {
        using var host = new TestHost();
        var before = Display(host, "demo-antigravity-1", "ag-w1");
        Assert.Equal("Resets in 1 m", before.ResetRelative);
        host.Controller.AdvanceClock(TimeSpan.FromHours(1));
        var passed = Display(host, "demo-antigravity-1", "ag-w1");
        Assert.Equal("Reset passed · Awaiting update", passed.ResetRelative);
        Assert.Equal(QuotaSeverity.Exhausted, passed.Severity);
        Assert.True(passed.ResetIsCritical);
        Assert.Equal(0, host.Window("demo-antigravity-1", "ag-w1").RemainingPercent);
    }

    [Fact]
    public async Task F08NewObservationAfterResetReports100()
    {
        using var host = new TestHost();
        host.Controller.AdvanceClock(TimeSpan.FromHours(1));
        var result = await host.Usage.ExecuteAsync(host.Context.Command(UiCommandKind.RefreshAccount, "demo-antigravity-1"), CancellationToken.None);
        Assert.Equal(CommandStatus.Succeeded, result.Status);
        var window = host.Window("demo-antigravity-1", "ag-w1");
        Assert.Equal((100d, ValueState.Known), (window.RemainingPercent!.Value, window.ValueState));
        Assert.True(window.ResetsAt > host.Clock.UtcNow);
    }

    [Fact]
    public void F08MoneyIsFormattedOnlyWithCurrencyAndExponent()
    {
        using var host = new TestHost();
        var research = host.Account("demo-claude-1");
        var extra = ExtensionViewModel.From(research.Extensions[0], host.Format);
        var credits = ExtensionViewModel.From(research.Extensions[1], host.Format);
        Assert.Equal("$12.50", extra.Value);
        Assert.Contains("limit not set", extra.Note);
        Assert.Equal("1,250", credits.Value);
        Assert.DoesNotContain("$", credits.Value + credits.Note + credits.Title);
        Assert.Contains("currency not reported", credits.Title);
        Assert.Null(host.Format.Money("1250", null, null));
        Assert.Null(host.Format.Money("1250", 2, null));
    }

    [Fact]
    public void NativeAmountsKeepProviderUnitsAndNeverInventALimit()
    {
        using var host = new TestHost();
        var pool = Display(host, "demo-claude-1", "demo-shared-1-w");
        Assert.Equal("1,250 tokens remaining · limit not reported", pool.AbsoluteText);
    }

    [Fact]
    public void CapabilityResolutionPrefersExactTargetAndTreatsAbsentAsUnavailable()
    {
        var snapshot = new UiSnapshot(1, UiMode.Live, DateTimeOffset.UnixEpoch, [], [
            new("RefreshAccount", null, Availability.Available, null, CapabilityOrigin.Existing),
            new("RefreshAccount", "a", Availability.Unavailable, "Capability_NotImplemented", CapabilityOrigin.Planned),
        ], new([], [], [], false, false, true, [Preferences.DefaultGlobalRule]), new("b", "s", HealthState.Idle, UpdateState.Current, RecoveryState.None, CompatibilityState.Normal));
        Assert.True(QuotaRules.IsAvailable(snapshot, "RefreshAccount", "b"));
        Assert.False(QuotaRules.IsAvailable(snapshot, "RefreshAccount", "a"));
        Assert.False(QuotaRules.IsAvailable(snapshot, "Disconnect"));
    }
}
