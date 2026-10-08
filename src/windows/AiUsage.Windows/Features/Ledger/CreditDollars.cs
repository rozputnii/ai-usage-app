using AiUsage.Features.Ledger.Contract;

namespace AiUsage.Features.Ledger;

/// <summary>D-199: a credit pool shown in US dollars. Dollars only display the credit facts; caps stay in credits.</summary>
internal static class CreditDollars
{
    public static bool ValidRate(decimal rate) => rate > 0 && rate <= 1000 && decimal.Round(rate, 6) == rate;

    /// <summary>Credits times the rate, rounded down to the cent.</summary>
    public static decimal Dollars(decimal credits, decimal rate) => decimal.Floor(credits * rate * 100) / 100;

    /// <summary>A cap entered in dollars is kept as whole credits that never exceed it.</summary>
    public static decimal CapCredits(decimal dollars, decimal rate) => decimal.Floor(dollars / rate);

    /// <summary>Today's use entered in dollars becomes the nearest whole credit.</summary>
    public static decimal TodayCredits(decimal dollars, decimal rate) => decimal.Round(dollars / rate, 0, MidpointRounding.AwayFromZero);

    /// <summary>A cap shown under one unit choice, expressed under another (for undo after a unit or rate change).</summary>
    public static decimal Convert(decimal amount, UnitModel? from, UnitModel? to)
    {
        var credits = from is { Usd: true } ? CapCredits(amount, from.Rate) : amount;
        return to is { Usd: true } ? Dollars(credits, to.Rate) : credits;
    }

    /// <summary>Applies the owner's stored choice to a convertible card; every other card is returned unchanged.</summary>
    public static LimitCardModel Apply(LimitCardModel card, UnitModel? stored)
    {
        if (card.Units is null)
            return card;
        var units = stored ?? card.Units;
        card = card with { Units = units };
        if (!units.Usd)
            return card;
        decimal? D(decimal? value) => value is { } credits ? Dollars(credits, units.Rate) : null;
        var f = card.Figures;
        return card with
        {
            Scale = ScaleModel.Money("USD", 2),
            Figures = f with
            {
                Used = D(f.Used), DayStart = D(f.DayStart), TodayEnd = D(f.TodayEnd), UsualShare = D(f.UsualShare),
                ProviderLimit = f.ProviderLimit is { Kind: LimitValueKind.Known, Amount: { } limit } ? LimitValue.Known(Dollars(limit, units.Rate)) : f.ProviderLimit,
                EffectiveLimit = D(f.EffectiveLimit), ProviderRemaining = D(f.ProviderRemaining), ProviderBalance = D(f.ProviderBalance),
            },
            Cap = card.Cap is { } cap ? cap with { Amount = Dollars(cap.Amount, units.Rate) } : null,
            TodayUse = card.TodayUse is { } today ? today with { Tracked = D(today.Tracked) } : null,
        };
    }

    public static HistoryModel ToDollars(HistoryModel history, decimal rate) => history with
    {
        Days = [.. history.Days.Select(d => d with { Used = d.Used is { } used ? Dollars(used, rate) : null })],
        BaselinePerWorkDay = history.BaselinePerWorkDay is { } baseline ? Dollars(baseline, rate) : null,
    };
}
