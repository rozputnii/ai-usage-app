using AiUsage.Features.Ledger;
using AiUsage.Features.Ledger.Contract;
using AiUsage.Features.Ledger.Demo;
using Xunit;

namespace AiUsage.Presentation.Tests;

public sealed class MonetaryGroupingTests
{
    [Fact]
    public async Task MismatchedRetainedCapsAreDisplayedNativelyButReplacedInTheCurrentScale()
    {
        var scheduler = new ManualScheduler();
        var source = new DemoLedgerSource(scheduler);
        source.LoadScenario(DemoLedgerScenarios.Monetary);
        using var window = new LedgerViewModel(source, scheduler);
        var money = window.Cards.Single(c => c.CardId == "money-mixed");
        var row = new CapRow(window.Settings, new("saved", money.CardId, "Mixed account", "Spending",
            ScaleModel.Money("EUR", 2), 200, CapStatus.CurrencyMismatch, false, LimitValue.Known(500), "USD", null));
        Assert.Equal("€200.00", row.AmountText);
        await row.ActAsync();
        Assert.Equal("USD", row.Editor!.Scale.Currency);
        Assert.Equal(string.Empty, row.Editor.Text);
        Assert.False(row.Editor.HasCap);
        await row.Editor.RemoveAsync();
        Assert.NotNull(row.Editor.Error);
        Assert.False(window.HasUndo);
        row.Editor.Text = "250";
        await row.Editor.SaveAsync();
        Assert.Equal(250, money.Model.Cap!.Amount);
        Assert.Equal("USD", money.Model.Monetary!.PersonalCap!.Currency);
        Assert.False(window.HasUndo);

        var mismatch = new LimitCardViewModel(window, money.Model with { Cap = new(200, false, CapStatus.CurrencyMismatch) },
            money.Account, ValueMode.Used, source.Current.LocalNow);
        mismatch.BeginCapEdit();
        Assert.Equal(string.Empty, mismatch.CapEditor!.Text);
        Assert.False(mismatch.CapEditor.HasCap);
    }

    [Fact]
    public async Task SpendingSharesAccountActionsAndKeepsItsOwnHistoryAndCapTarget()
    {
        var scheduler = new ManualScheduler();
        var source = new DemoLedgerSource(scheduler);
        source.LoadScenario(DemoLedgerScenarios.Monetary);
        using var model = new LedgerViewModel(source, scheduler);
        var money = Assert.Single(model.Cards, c => c.CardId == "money-mixed");
        Assert.False(money.CanSignOut);
        Assert.True(money.CanOpenHistory);
        money.BeginRename();
        Assert.False(money.IsRenaming);
        await money.ToggleHistoryAsync();
        Assert.Equal(money.CardId, model.History!.CardId);
        Assert.Equal("Mixed account", model.History.Title);
        money.BeginCapEdit();
        Assert.NotNull(money.CapEditor);
        money.CapEditor!.Text = "250";
        await money.CapEditor.SaveAsync();
        Assert.Equal("250.00 USD", LedgerFormat.NativeMoney(money.Model.Monetary!.PersonalCap));
        var only = Assert.Single(model.Cards, c => c.CardId == "money-only");
        Assert.True(only.CanSignOut);
        Assert.True(only.CanOpenHistory);
        Assert.False(only.CanEditCap);
        var rows = LedgerTrayViewModel.Project(source.Current, source.Preferences);
        Assert.Equal(2, rows.Count);
        Assert.Equal(2, rows[0].Strips.Count);
        Assert.Empty(rows[1].Strips);
    }
}
