using AiUsage.Core.Budget;
using AiUsage.Core.Usage;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class LimitModelTests
{
    [Fact]
    public void MoneyAlignsUpExactlyAndRejectsOverflowOrDifferentCurrency()
    {
        Assert.Equal(new MoneyQuantity(25, 2, "USD"), QuantityMath.Subtract(
            new MoneyQuantity(125, 2, "USD"), new MoneyQuantity(1, 0, "USD")));
        Assert.Null(QuantityMath.Subtract(new MoneyQuantity(1, 0, "USD"), new MoneyQuantity(1, 2, "usd")));
        Assert.Null(QuantityMath.Subtract(new MoneyQuantity(long.MaxValue, 0, "USD"), new MoneyQuantity(1, 2, "USD")));
        Assert.Null(QuantityMath.Subtract(new MoneyQuantity(1, null, "USD"), new MoneyQuantity(1, 2, "USD")));
        Assert.Null(QuantityMath.Subtract(new CountQuantity(1, "credits"), new CountQuantity(1, "requests")));
        Assert.Null(QuantityMath.Subtract(new CountQuantity(1, "unknown"), new CountQuantity(1, "unknown")));
        Assert.Equal(new CountQuantity(-3, "credits"), QuantityMath.Subtract(new CountQuantity(2, "credits"), new CountQuantity(5, "credits")));
    }

    [Fact]
    public void CapSelectionPreservesZeroNullUnlimitedAndProviderTie()
    {
        var facts = Pool(LimitValue.Finite(new CountQuantity(50, "requests")));
        Assert.Equal(LimitBinding.Provider, EffectiveLimit.Resolve(facts, new(new CountQuantity(50, "requests"), DateTimeOffset.MinValue)).Binding);
        Assert.Equal(LimitBinding.PersonalCap, EffectiveLimit.Resolve(facts, new(new CountQuantity(20, "requests"), DateTimeOffset.MinValue)).Binding);
        foreach (var state in new[] { LimitValue.Unknown, LimitValue.ExplicitNull, LimitValue.Unlimited })
        {
            Assert.Null(EffectiveLimit.Resolve(facts with { Limit = state }, null).Value);
            Assert.Equal(new CountQuantity(0, "requests"), EffectiveLimit.Resolve(facts with { Limit = state }, new(new CountQuantity(0, "requests"), DateTimeOffset.MinValue)).Value);
        }
        var zero = EffectiveLimit.Resolve(facts with { Limit = LimitValue.Finite(new CountQuantity(0, "requests")) }, null);
        Assert.Equal(new CountQuantity(0, "requests"), zero.Value);
    }

    [Fact]
    public void PercentCapRules()
    {
        var window = Pool(LimitValue.NotApplicable) with { Kind = LimitKind.PercentWindow, Unit = "percent", Duration = TimeSpan.FromDays(7) };
        EffectiveLimit Cap(Quantity amount) => EffectiveLimit.Resolve(window, new(amount, DateTimeOffset.MinValue));
        Assert.Equal(new(new CountQuantity(90, "percent"), LimitBinding.PersonalCap, false), Cap(new CountQuantity(90, "percent")));
        Assert.Equal(new(new CountQuantity(100, "percent"), LimitBinding.Provider, false), Cap(new CountQuantity(100, "percent")));
        Assert.Equal(new(new CountQuantity(100, "percent"), LimitBinding.Provider, true), Cap(new CountQuantity(90, "requests")));
        Assert.Equal(new(new CountQuantity(100, "percent"), LimitBinding.Provider, true), Cap(new CountQuantity(101, "percent")));
        Assert.Equal(new(new CountQuantity(100, "percent"), LimitBinding.Provider, true), Cap(new MoneyQuantity(90, 2, "USD")));
        Assert.Equal(new(new CountQuantity(100, "percent"), LimitBinding.Provider, false), EffectiveLimit.Resolve(window, null));
        // A percent cap never applies to a count pool.
        Assert.True(EffectiveLimit.Resolve(Pool(LimitValue.Finite(new CountQuantity(50, "requests"))), new(new CountQuantity(20, "percent"), DateTimeOffset.MinValue)).CapRejected);
    }

    [Fact]
    public void IncompatibleCapIsNotConvertedOrApplied()
    {
        var facts = Pool(LimitValue.Finite(new MoneyQuantity(500, 2, "USD"))) with { Kind = LimitKind.MonetaryPool, Unit = "USD" };
        var result = EffectiveLimit.Resolve(facts, new(new MoneyQuantity(100, 2, "EUR"), DateTimeOffset.MinValue));
        Assert.Equal(facts.Limit.Value, result.Value);
        Assert.True(result.CapRejected);
        Assert.Null(EffectiveLimit.Resolve(Pool(LimitValue.Unknown) with { Unit = "unknown" }, new(new CountQuantity(1, "unknown"), DateTimeOffset.MinValue)).Value);
    }

    [Fact]
    public void NegativeProviderEntitlementCannotBecomeABudget()
    {
        Assert.Null(EffectiveLimit.Resolve(Pool(LimitValue.Finite(new CountQuantity(-5, "requests"))), null).Value);
    }

    internal static LimitFacts Pool(LimitValue value) => new(new("test", "pool", "opaque"), LimitKind.CountablePool, "requests", value);
}
