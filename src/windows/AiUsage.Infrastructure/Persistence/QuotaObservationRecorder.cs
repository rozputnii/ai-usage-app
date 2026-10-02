using System.Text.Json;
using AiUsage.Core.Budget;
using AiUsage.Core.Usage;

namespace AiUsage.Infrastructure.Persistence;

/// <summary>
/// Compatibility capture of already normalized product values. AIU-037 owns native limit-key
/// mapping; legacy keys are explicit and must not be silently merged with its future keys.
/// </summary>
public sealed class QuotaObservationRecorder(IReadingSeriesStore store) : IQuotaObservationRecorder
{
    public Task RecordAsync(string accountTarget, string provider, ProviderSessionState state, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Status != ProviderSessionStatus.QuotaAvailable || state.FromCache || state.Failure is not null || state.Quota is not { } quota)
            return Task.CompletedTask;
        List<ReadingObservation> observations = [];
        foreach (var group in quota.Groups)
        foreach (var window in group.Windows)
        {
            var percent = window.UsedPercent is >= 0 and <= 100 ? (decimal?)window.UsedPercent : null;
            Quantity? value = window.Amount is { Used: { } used, Unit: not "unknown" and not "" } amount
                ? new CountQuantity(used, amount.Unit) : percent is { } p ? new CountQuantity(p, "percent") : null;
            if (value is null) continue;
            var observation = Create("legacy-window-v1", JsonSerializer.Serialize(new[] { group.Id, window.Id }), value) with
            { UsedPercent = percent, ResetAt = window.ResetsAt };
            if (window.ResetsAt is { } reset && window.Duration is { Ticks: > 0 } duration &&
                reset - DateTimeOffset.MinValue >= duration && reset - duration <= quota.FetchedAt)
                observation = observation with { PeriodStartedAt = reset - duration };
            observations.Add(observation);
        }
        if (provider == "codex" && quota.Credits?.Balance is { } balance)
            observations.Add(Create("legacy-balance-v1", "credits", new CountQuantity(balance, "credits")) with { IsBalance = true });
        if (provider == "claude" && state.ExtraUsage?.Used is { AmountMinor: { } minor } money)
            observations.Add(Create("legacy-extra-v1", "extra-usage", new MoneyQuantity(minor, money.Exponent, money.Currency)));
        return observations.Count == 0 ? Task.CompletedTask : store.AppendAsync(observations, token);

        ReadingObservation Create(string family, string discriminator, Quantity value) => new(new(accountTarget,
            new(provider, family, discriminator)), value, quota.FetchedAt)
        {
            PlanType = quota.PlanType, Source = SnapshotSource.ProviderApi, SourceVersion = "quota-v1",
            RoundingUnit = value is CountQuantity count ? RoundingUnit(count.Value) : 1
        };
    }

    private static decimal RoundingUnit(decimal value)
    {
        int scale = (decimal.GetBits(value)[3] >> 16) & 0x7f;
        decimal unit = 1;
        for (int i = 0; i < scale; i++) unit /= 10;
        return unit;
    }
}
