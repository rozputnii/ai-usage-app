namespace AiUsage.Features.Ledger;

/// <summary>D-NEW: a credit pool shown in US dollars. Dollars only display the credit facts; caps stay in credits.</summary>
internal static class CreditDollars
{
    public static bool ValidRate(decimal rate) => rate > 0 && rate <= 1000 && decimal.Round(rate, 6) == rate;
}
