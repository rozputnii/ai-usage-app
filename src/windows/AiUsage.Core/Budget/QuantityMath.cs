using AiUsage.Core.Usage;

namespace AiUsage.Core.Budget;

public static class QuantityMath
{
    public static bool TryAlign(Quantity? left, Quantity? right, out decimal a, out decimal b, out Quantity? scale)
    {
        a = b = 0;
        scale = null;
        if (left is CountQuantity x && right is CountQuantity y &&
            !string.IsNullOrEmpty(x.Unit) && x.Unit != "unknown" && x.Unit == y.Unit)
        {
            a = x.Value; b = y.Value; scale = x;
            return true;
        }
        if (left is not MoneyQuantity m || right is not MoneyQuantity n ||
            string.IsNullOrEmpty(m.Currency) || m.Currency != n.Currency ||
            m.Exponent is not >= 0 || n.Exponent is not >= 0) return false;
        try
        {
            int exponent = Math.Max(m.Exponent.Value, n.Exponent.Value);
            a = Scale(m.MinorUnits, exponent - m.Exponent.Value);
            b = Scale(n.MinorUnits, exponent - n.Exponent.Value);
            scale = m with { Exponent = exponent };
            return true;
        }
        catch (OverflowException) { return false; }
    }

    public static Quantity? Subtract(Quantity? left, Quantity? right)
    {
        if (!TryAlign(left, right, out var a, out var b, out var scale)) return null;
        try
        {
            var value = checked(a - b);
            return scale is MoneyQuantity money ? money with { MinorUnits = checked((long)value) }
                : new CountQuantity(value, ((CountQuantity)scale!).Unit);
        }
        catch (OverflowException) { return null; }
    }

    public static bool IsValidFor(Quantity? value, LimitFacts facts) => value switch
    {
        MoneyQuantity money => facts.Kind == LimitKind.MonetaryPool && money.Currency == facts.Unit &&
            !string.IsNullOrEmpty(money.Currency) && money.Exponent is >= 0,
        CountQuantity count => facts.Kind != LimitKind.MonetaryPool && count.Unit == facts.Unit &&
            !string.IsNullOrEmpty(count.Unit) && count.Unit != "unknown",
        _ => false
    };

    private static long Scale(long value, int places)
    {
        if (value == 0) return 0;
        if (places > 18) throw new OverflowException();
        for (int i = 0; i < places; i++) value = checked(value * 10);
        return value;
    }
}
