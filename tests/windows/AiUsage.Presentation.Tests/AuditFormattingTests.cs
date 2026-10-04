using AiUsage.Features.Ledger;
using AiUsage.Features.Ledger.Contract;
using Xunit;

namespace AiUsage.Presentation.Tests;

public sealed class AuditFormattingTests
{
    [Theory]
    [InlineData(0, "XTS 1")]
    [InlineData(3, "XTS 1.000")]
    [InlineData(8, "XTS 1.00000000")]
    [InlineData(18, "XTS 1.000000000000000000")]
    public void SupportedMoneyPrecisionIsNotTruncated(int exponent, string expected) =>
        Assert.Equal(expected, LedgerFormat.Value(ScaleModel.Money("XTS", exponent), 1m));

    [Fact]
    public void TinyKnownMoneyMustNotDisplayAsZero() =>
        Assert.Equal("XTS 0.00000001", LedgerFormat.Value(ScaleModel.Money("XTS", 8), .00000001m));

    [Fact]
    public void CapEditingKeepsNativePrecision()
    {
        var scale = ScaleModel.Money("XTS", 8);
        Assert.Equal("0.00000001", LedgerFormat.EditText(scale, .00000001m));
        Assert.True(LedgerFormat.TryParseAmount("0.00000001", scale, out var value));
        Assert.Equal(.00000001m, value);
    }

    [Theory]
    [InlineData("€12.50", "USD")]
    [InlineData("$12.50", "EUR")]
    [InlineData("12USD34", "USD")]
    [InlineData("1,2", "USD")]
    [InlineData("12,,34", "USD")]
    public void InvalidOrForeignCapAmountsAreRejected(string text, string currency) =>
        Assert.False(LedgerFormat.TryParseAmount(text, ScaleModel.Money(currency, 2), out _));

    [Fact]
    public void AbstractCreditsCannotAcceptACurrencySymbol() =>
        Assert.False(LedgerFormat.TryParseAmount("$100", ScaleModel.Count("credits"), out _));

    [Theory]
    [InlineData(false, "$90,071,992,547,409.93 of $100,000,000,000,000.00 used", "$0.03 of $0.10 used")]
    [InlineData(true, "$9,928,007,452,590.07 of $100,000,000,000,000.00 left", "$0.07 of $0.10 left")]
    public void CardTextKeepsNativeMoneyPrecisionWhenGeometryNeedsDoubles(bool left, string footer, string today)
    {
        var card = new LimitCardModel("synthetic-money", null, CardLayout.Pool, ScaleModel.Money("USD", 2), PeriodModel.Month(false),
            CardState.OnTrack, Freshness.Fresh(), [],
            new(90071992547409.93m, 90071992547409.90m, 90071992547410m, 10m, LimitValue.Known(100000000000000m),
                100000000000000m, null, null, null), null, null, null, "synthetic-money", CardAction.None, null);
        var account = new AccountModel("synthetic", ProviderKind.Claude, "SYNTHETIC", AccountHealth.Ok, null, null, null, [card]);
        var visual = CardVisuals.Build(card, account, left ? ValueMode.Left : ValueMode.Used, DateTimeOffset.UnixEpoch);
        Assert.Equal(footer, visual.Footer);
        Assert.Contains(today, visual.Cells.Single().Tip);
    }

    [Fact]
    public void SmallOverageOnALargeMoneyAmountKeepsItsOverflowLabel()
    {
        var card = new LimitCardModel("synthetic-money", null, CardLayout.Pool, ScaleModel.Money("USD", 2), PeriodModel.Month(false),
            CardState.OverToday, Freshness.Fresh(), [],
            new(90071992547409.93m, 90071992547409.90m, 90071992547409.92m, 10m, LimitValue.Known(100000000000000m),
                100000000000000m, null, null, null), null, null, null, "synthetic-money", CardAction.None, null);
        var account = new AccountModel("synthetic", ProviderKind.Claude, "SYNTHETIC", AccountHealth.Ok, null, null, null, [card]);
        Assert.Equal("+$0.01", CardVisuals.Build(card, account, ValueMode.Used, DateTimeOffset.UnixEpoch).OverLabel);
        Assert.Equal("−$0.01", CardVisuals.Build(card, account, ValueMode.Left, DateTimeOffset.UnixEpoch).OverLabel);
    }
}
