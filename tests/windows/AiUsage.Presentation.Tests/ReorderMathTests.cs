using AiUsage.Features.Ledger;
using AiUsage.Features.Ledger.Demo;
using Xunit;

namespace AiUsage.Presentation.Tests;

/// <summary>AIU-055 R-10: dragging an account card by its grip places it among the other account cards.</summary>
public sealed class ReorderMathTests
{
    [Fact]
    public void ThePointerPassesTheMidpointsAboveIt()
    {
        double[] midpoints = [50, 150, 250];
        Assert.Equal(0, ReorderMath.InsertionIndex(midpoints, 10));
        Assert.Equal(1, ReorderMath.InsertionIndex(midpoints, 100));
        Assert.Equal(3, ReorderMath.InsertionIndex(midpoints, 300));
        Assert.Equal(0, ReorderMath.InsertionIndex([], 100));
    }

    [Fact]
    public void TheMovedAccountGoesBeforeTheAccountAtItsPlaceOrLast()
    {
        string[] order = ["a", "b", "c", "d"];
        Assert.Equal("a", ReorderMath.BeforeId(order, "b", 0));
        Assert.Equal("c", ReorderMath.BeforeId(order, "b", 1));
        Assert.Null(ReorderMath.BeforeId(order, "b", 3));
    }

    [Fact]
    public async Task OnlyAccountCardsHaveAGripAndOnlyWithAnotherAccountShown()
    {
        var scheduler = new ManualScheduler();
        var source = new DemoLedgerSource(scheduler);
        source.LoadScenario(DemoLedgerScenarios.Brief);
        var window = new LedgerViewModel(source, scheduler);

        Assert.Contains(window.Cards, c => c.IsAccountSection);
        Assert.All(window.Cards, c => Assert.Equal(!c.IsAccountSection, c.CanReorder));
        Assert.Equal("Reorder Claude Pro", window.Cards.Single(c => c.CardId == "claude-week").ReorderName);

        // Signed-out accounts are hidden, so Claude is the only account left to show.
        foreach (var account in new[] { "acct-codex", "acct-copilot", "acct-antigravity" })
            await source.SignOutAsync(account, TestContext.Current.CancellationToken);
        Assert.Equal(["acct-claude"], window.Cards.Select(c => c.Account.AccountId).Distinct());
        Assert.All(window.Cards, c => Assert.False(c.CanReorder));
    }
}
