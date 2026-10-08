using AiUsage.Core.Usage;

namespace AiUsage.Core.Budget;

public enum LimitBinding { None, Provider, PersonalCap }
public sealed record EffectiveLimit(Quantity? Value, LimitBinding Binding, bool CapRejected)
{
    /// <summary>D-202: a percent window of at least a day, or a monthly one, takes a percent cap; a five-hour window never does.</summary>
    public static bool TakesPercentCap(LimitFacts facts) =>
        facts.Kind == LimitKind.PercentWindow && (facts.IsMonthly || facts.Duration >= TimeSpan.FromDays(1));

    public static EffectiveLimit Resolve(LimitFacts facts, PersonalCap? cap)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (facts.Kind == LimitKind.PercentWindow)
        {
            // D-202: a whole-window cap in percent of the provider's window, at most 100.
            var full = new CountQuantity(100, "percent");
            if (cap is null) return new(full, LimitBinding.Provider, false);
            if (!TakesPercentCap(facts) || cap.Amount is not CountQuantity { Unit: "percent", Value: >= 0 and <= 100 } percent)
                return new(full, LimitBinding.Provider, true);
            return percent.Value < 100 ? new(percent, LimitBinding.PersonalCap, false) : new(full, LimitBinding.Provider, false);
        }
        var provider = facts.Limit.State == LimitValueState.Finite && QuantityMath.IsValidFor(facts.Limit.Value, facts)
            ? facts.Limit.Value : null;
        if (provider is not null && (!QuantityMath.TryAlign(provider, provider, out var entitlement, out _, out _) || entitlement < 0)) provider = null;
        if (cap is null) return new(provider, provider is null ? LimitBinding.None : LimitBinding.Provider, false);
        bool valid = QuantityMath.IsValidFor(cap.Amount, facts) &&
            QuantityMath.TryAlign(cap.Amount, cap.Amount, out var amount, out _, out _) && amount >= 0;
        if (!valid) return new(provider, provider is null ? LimitBinding.None : LimitBinding.Provider, true);
        if (provider is null) return new(cap.Amount, LimitBinding.PersonalCap, false);
        if (!QuantityMath.TryAlign(provider, cap.Amount, out var p, out var c, out _))
            return new(provider, LimitBinding.Provider, true);
        return c < p ? new(cap.Amount, LimitBinding.PersonalCap, false) : new(provider, LimitBinding.Provider, false);
    }
}
