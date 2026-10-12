using AiUsage.Features.Ledger;
using AiUsage.Features.Ledger.Demo;
using Xunit;

namespace AiUsage.Presentation.Tests;

/// <summary>T-055 R-10: dragging an account card by its grip places it among the other account cards.</summary>
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
    public void TheDraggedCardLandsAmongTheOthersWithItsHistoryAfterIt()
    {
        // Cards a, b (with its history panel) and c.
        bool[] isCard = [true, true, false, true];
        Assert.Equal([1, 2, 3, 0], ReorderMath.LandingOrder(isCard, 0, 2));
        Assert.Equal([3, 0, 1, 2], ReorderMath.LandingOrder(isCard, 3, 0));
        Assert.Equal([0, 3, 1, 2], ReorderMath.LandingOrder(isCard, 1, 2));
        Assert.Equal([1, 2, 0, 3], ReorderMath.LandingOrder(isCard, 1, 0));
        Assert.Equal([1, 2, 0, 3], ReorderMath.LandingOrder(isCard, 0, 1));
        Assert.Equal([0, 1, 2, 3], ReorderMath.LandingOrder(isCard, 0, 0));
    }

    [Fact]
    public void TheOtherCardsSlideByTheDraggedCardsHeightAndGap()
    {
        // Heights 100, 50 (b), 80 (b's history), 60 (c) with 12 px gaps: tops 0, 112, 174, 266.
        double[] heights = [100, 50, 80, 60];
        // a goes last: the others slide up by 112; a lands at the bottom (226).
        Assert.Equal([226, -112, -112, -112], ReorderMath.Offsets(heights, 12, [1, 2, 3, 0]));
        // c goes first: the others slide down by 72; c rises to the top.
        Assert.Equal([72, 72, 72, -266], ReorderMath.Offsets(heights, 12, [3, 0, 1, 2]));
        // The order as it is moves nothing.
        Assert.Equal([0, 0, 0, 0], ReorderMath.Offsets(heights, 12, [0, 1, 2, 3]));
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
