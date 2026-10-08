using AiUsage.Features.Ledger;
using AiUsage.Features.Ledger.Contract;
using AiUsage.Features.Ledger.Demo;
using Xunit;

namespace AiUsage.Presentation.Tests;

/// <summary>AIU-055 R-10: the owner orders subscriptions; the order applies to the window and the tray.</summary>
public sealed class AccountOrderTests
{
    private static (LedgerViewModel Window, DemoLedgerSource Source) Start()
    {
        var scheduler = new ManualScheduler();
        var source = new DemoLedgerSource(scheduler);
        source.LoadScenario(DemoLedgerScenarios.Brief);
        return (new LedgerViewModel(source, scheduler), source);
    }

    private static string[] Accounts(LedgerViewModel window) => [.. window.Cards.Select(c => c.Account.AccountId).Distinct()];
    private static LimitCardViewModel Card(LedgerViewModel window, string id) => window.Cards.Single(c => c.CardId == id);

    [Fact]
    public async Task KeyboardMovesTheAccountAndSectionsStayInside()
    {
        var (window, source) = Start();
        string[] moved = ["acct-codex", "acct-claude", "acct-copilot", "acct-antigravity"];

        await Card(window, "claude-week").MoveDownAsync();
        Assert.Equal(moved, Accounts(window));

        await Card(window, "claude-extra").MoveUpAsync();
        Assert.Equal(moved, Accounts(window));
        Assert.Equal(["claude-extra", "claude-week"], source.Current.Accounts.Single(a => a.AccountId == "acct-claude").Cards.Select(c => c.CardId));

        Assert.Equal(moved, LedgerTrayViewModel.Project(source.Current, source.Preferences).Select(r => r.AccountId));
    }

    [Fact]
    public async Task MoveToLastAndBack()
    {
        var (window, _) = Start();
        var focused = new List<string>();
        window.FocusCardRequested += (_, id) => focused.Add(id);

        await window.MoveAccountAsync("acct-claude", null);
        Assert.Equal(["acct-codex", "acct-copilot", "acct-antigravity", "acct-claude"], Accounts(window));
        // The last account cannot move further down, nor the first further up.
        await Card(window, "claude-week").MoveDownAsync();
        await Card(window, "codex-week").MoveUpAsync();
        Assert.Equal(["acct-codex", "acct-copilot", "acct-antigravity", "acct-claude"], Accounts(window));

        await window.MoveAccountAsync("acct-claude", "acct-codex");
        Assert.Equal(["acct-claude", "acct-codex", "acct-copilot", "acct-antigravity"], Accounts(window));
        Assert.Equal(["claude-week", "claude-week"], focused);
    }

    [Fact]
    public async Task HiddenSignedOutAccountsKeepTheirPlace()
    {
        var (window, source) = Start();
        await source.SignOutAsync("acct-codex", TestContext.Current.CancellationToken);
        Assert.Equal(["acct-claude", "acct-copilot", "acct-antigravity"], Accounts(window));

        // Down among the visible accounts: before the account two places on.
        await Card(window, "claude-week").MoveDownAsync();
        Assert.Equal(["acct-copilot", "acct-claude", "acct-antigravity"], Accounts(window));
        Assert.Equal(["acct-codex", "acct-copilot", "acct-claude", "acct-antigravity"], source.Current.Accounts.Select(a => a.AccountId));

        Assert.Equal(CommandOutcome.Rejected, await source.MoveAccountAsync("acct-missing", null, TestContext.Current.CancellationToken));
        Assert.Equal(CommandOutcome.Rejected, await source.MoveAccountAsync("acct-claude", "acct-claude", TestContext.Current.CancellationToken));
        Assert.Equal(CommandOutcome.Rejected, await source.MoveAccountAsync("acct-claude", "acct-missing", TestContext.Current.CancellationToken));
    }
}
