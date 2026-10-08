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
        mismatch.OpenSettings();
        Assert.Equal(string.Empty, mismatch.Settings!.Cap!.Text);
        Assert.False(mismatch.Settings.Cap.HasCap);
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
        money.OpenSettings();
        money.Settings!.Cap!.Text = "250";
        await money.Settings.Cap.SaveAsync();
        Assert.Equal("250.00 USD", LedgerFormat.NativeMoney(money.Model.Monetary!.PersonalCap));
        var only = Assert.Single(model.Cards, c => c.CardId == "money-only");
        Assert.True(only.CanSignOut);
        Assert.True(only.CanOpenHistory);
        Assert.False(only.CanEditCap);
        var rows = LedgerTrayViewModel.Project(source.Current, source.Preferences);
        Assert.Equal(2, rows.Count);
        // The tray shows the main limit only: no spending or model limit, and no bar for a note-only account.
        Assert.Equal("claude-week", rows[0].Strip!.CardId);
        Assert.Null(rows[1].Strip);
    }

    [Fact]
    public async Task ModelLimitIsASectionOfTheSubscriptionCardWithoutRepeatingAccountMarks()
    {
        var scheduler = new ManualScheduler();
        var source = new DemoLedgerSource(scheduler);
        source.LoadScenario(DemoLedgerScenarios.Monetary);
        using var model = new LedgerViewModel(source, scheduler);
        var mixed = model.Cards.Where(c => c.Account.DisplayName == "Mixed account").ToArray();
        Assert.Equal([false, true, true], mixed.Select(c => c.IsAccountSection));
        var fable = Assert.Single(mixed, c => c.CardId == "money-fable");
        Assert.Equal(string.Empty, fable.HeaderName);
        Assert.Equal("Fable", fable.Tag);
        Assert.False(fable.CanSignOut);
        Assert.True(fable.CanOpenHistory);
        fable.BeginRename();
        Assert.False(fable.IsRenaming);
        await fable.ToggleHistoryAsync();
        Assert.Equal(fable.CardId, model.History!.CardId);

        CardMark[] accountMarks = [new(MarkKind.SyncFailed), new(MarkKind.SignInExpired)];
        var host = new LimitCardViewModel(model, mixed[0].Model with { Marks = accountMarks, Action = CardAction.SignIn },
            mixed[0].Account, ValueMode.Used, source.Current.LocalNow);
        var section = new LimitCardViewModel(model, fable.Model with { Marks = [.. accountMarks, new(MarkKind.PastReset)], Action = CardAction.SignIn },
            fable.Account, ValueMode.Used, source.Current.LocalNow);
        Assert.Equal(2, host.Visual.Marks.Count);
        Assert.True(host.Visual.HasAction);
        Assert.Equal([MarkKind.PastReset], section.Visual.Marks.Select(m => m.Kind));
        Assert.False(section.Visual.HasAction);
    }
}
