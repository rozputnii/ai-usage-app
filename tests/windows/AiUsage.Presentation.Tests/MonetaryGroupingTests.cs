using AiUsage.Features.Ledger;
using AiUsage.Features.Ledger.Contract;
using AiUsage.Features.Ledger.Demo;
using Xunit;

namespace AiUsage.Presentation.Tests;

public sealed class MonetaryGroupingTests
{
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
        Assert.Contains("250.00 USD", money.MonetarySummary);
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
