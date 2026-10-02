using AiUsage.Core.Usage;

namespace AiUsage.Core.Budget;

public enum LimitBinding { None, Provider, PersonalCap }
public sealed record EffectiveLimit(Quantity? Value, LimitBinding Binding, bool CapRejected)
{
    public static EffectiveLimit Resolve(LimitFacts facts, PersonalCap? cap)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (facts.Kind == LimitKind.PercentWindow)
            return new(new CountQuantity(100, "percent"), LimitBinding.Provider, cap is not null);
        var provider = facts.Limit.State == LimitValueState.Finite && QuantityMath.IsValidFor(facts.Limit.Value, facts)
            ? facts.Limit.Value : null;
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
