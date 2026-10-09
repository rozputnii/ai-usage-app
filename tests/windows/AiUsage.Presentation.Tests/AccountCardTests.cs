using AiUsage.Features.Ledger;
using AiUsage.Features.Ledger.Contract;
using AiUsage.Features.Ledger.Demo;
using Xunit;

namespace AiUsage.Presentation.Tests;

/// <summary>R-191: one card per account, with sections the owner can hide and show again.</summary>
public sealed class AccountCardTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task EveryLimitOfAnAccountIsASectionOfOneCardAndASectionCanBeHiddenAndShown()
    {
        var scheduler = new ManualScheduler();
        var source = new DemoLedgerSource(scheduler);
        source.LoadScenario(DemoLedgerScenarios.Brief);
        using var model = new LedgerViewModel(source, scheduler);
        var copilot = model.Cards.Where(c => c.Account.DisplayName == "Copilot Free").ToArray();
        Assert.Equal([false, true, true], copilot.Select(c => c.IsAccountSection));
        var (primary, chat) = (copilot[0], copilot[1]);
        Assert.True(primary.CanSignOut);
        Assert.False(primary.CanHide);
        Assert.False(chat.CanSignOut);
        Assert.True(chat.CanHide);
        string?[] TrayStrips() => [.. LedgerTrayViewModel.Project(source.Current, source.Preferences).Select(r => r.Strip?.CardId)];
        var strips = TrayStrips();

        await chat.HideAsync();
        Assert.True(chat.IsHiddenSection);
        Assert.Equal(1, primary.HiddenCount);
        Assert.Equal("1 hidden", primary.HiddenText);
        Assert.Equal("Hidden: " + chat.Tag, primary.HiddenTip);
        Assert.Contains(chat.CardId + "-", model.SectionLayout);
        // Hiding a section changes only the window; the tray keeps showing the main limit.
        Assert.Equal(strips, TrayStrips());

        await primary.ShowHiddenAsync();
        Assert.False(chat.IsHiddenSection);
        Assert.False(primary.HasHidden);
        Assert.Equal(CommandOutcome.Rejected, await source.SetCardHiddenAsync(primary.CardId, true, Token));
    }

    [Fact]
    public void HiddenCountKeepsTheMostUrgentHiddenColour()
    {
        var source = new DemoLedgerSource(new ManualScheduler());
        source.LoadScenario(DemoLedgerScenarios.Brief);
        var account = source.Current.Accounts.Single(a => a.DisplayName == "Copilot Free");
        var over = account.Cards[1] with { State = CardState.OverToday, Hidden = true };
        account = account with { Cards = [account.Cards[0], over, account.Cards[2] with { Hidden = true }] };
        using var model = new LedgerViewModel(source, new ManualScheduler());
        var primary = new LimitCardViewModel(model, account.Cards[0], account, ValueMode.Used, source.Current.LocalNow);
        Assert.Equal(2, primary.HiddenCount);
        Assert.Equal(Tone.Critical, primary.HiddenTone);
        Assert.Equal("Show 2 hidden limits", primary.HiddenName);
    }

    [Fact]
    public void WhenOnlyHiddenLimitsRemainTheFirstOneHeadsTheCard()
    {
        var source = new DemoLedgerSource(new ManualScheduler());
        source.LoadScenario(DemoLedgerScenarios.Brief);
        var account = source.Current.Accounts.Single(a => a.DisplayName == "Copilot Free");
        account = account with { Cards = [.. account.Cards.Skip(1).Select(c => c with { Hidden = true })] };
        Assert.Equal(account.Cards[0].CardId, AccountCard.Primary(account)!.CardId);
    }
}
