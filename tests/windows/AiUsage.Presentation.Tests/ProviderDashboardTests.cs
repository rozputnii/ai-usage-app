using AiUsage.Core.Dashboard;
using AiUsage.Core.Providers.Claude;
using AiUsage.Core.Usage;
using AiUsage.Features.Dashboard;
using Xunit;

namespace AiUsage.Presentation.Tests;

public sealed class ProviderDashboardTests
{
    [Fact]
    public async Task ProviderSelectionKeepsFailuresAndCommandsIndependent()
    {
        var codex = new FakeSession { HasStoredGrant = true };
        var claude = new FakeSession { HasStoredGrant = true,
            Operation = _ => Task.FromResult(new ProviderSessionState(ProviderSessionStatus.ReauthenticationRequired, Failure: ProviderFailureKind.AuthenticationRequired)) };
        using var codexWorkflow = new DashboardWorkflow(codex);
        using var claudeWorkflow = new DashboardWorkflow(claude);
        var codexCard = Card(codexWorkflow, "Codex");
        var claudeCard = Card(claudeWorkflow, "Claude");
        var shell = new DashboardShellViewModel(codexCard, claudeCard, () => Task.CompletedTask, () => { });
        shell.Selected = claudeCard;
        await shell.Selected.RefreshCommand.ExecuteAsync(null);
        Assert.Equal("ReauthenticationRequired/Text", claudeCard.StatusText);
        Assert.Equal("FailureAuthenticationRequired/Text", claudeCard.FailureText);
        Assert.Equal("EmptyState/Text", codexCard.StatusText);
        Assert.True(codexCard.RefreshCommand.CanExecute(null));
        shell.Selected = codexCard;
        await shell.Selected.RefreshCommand.ExecuteAsync(null);
        Assert.Equal(1, codex.Calls);
        Assert.Equal(1, claude.Calls);
        Assert.Equal("ReauthenticationRequired/Text", claudeCard.StatusText);
        await shell.StopAsync();
        Assert.False(codexCard.ConnectCommand.CanExecute(null));
        Assert.False(claudeCard.ConnectCommand.CanExecute(null));
    }

    [Fact]
    public async Task ManualEntryAndCancelExistOnlyDuringClaudeConnection()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var session = new FakeSession { Operation = async token =>
        {
            started.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return ProviderSessionState.NotConnected;
        } };
        using var workflow = new DashboardWorkflow(session);
        var card = Card(workflow, "Claude");
        Assert.False(card.ManualEntryVisible);
        var connect = card.ConnectCommand.ExecuteAsync(null);
        await started.Task.WaitAsync(TestContext.Current.CancellationToken);
        Assert.True(card.ManualEntryVisible);
        Assert.True(card.CancelCommand.CanExecute(null));
        card.SubmitCode("synthetic-invalid-code");
        Assert.Equal("ManualCodeInvalid/Text", card.FailureText);
        card.CancelCommand.Execute(null);
        await connect;
        Assert.False(card.ManualEntryVisible);
        Assert.False(card.Busy);
        Assert.False(card.CancelCommand.CanExecute(null));
    }

    [Fact]
    public async Task StartupDoesNotOfferACancelButtonThatCannotCancelItsWork()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var session = new FakeSession { HasStoredGrant = true, Operation = async token =>
        {
            started.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return ProviderSessionState.NotConnected;
        } };
        using var workflow = new DashboardWorkflow(session);
        var card = Card(workflow, "Claude");
        var load = card.LoadAsync(TestContext.Current.CancellationToken);
        await started.Task.WaitAsync(TestContext.Current.CancellationToken);
        Assert.True(card.Busy);
        Assert.False(card.CancelCommand.CanExecute(null));
        Assert.False(card.ManualEntryVisible);
        await card.StopAsync();
        Assert.True(load.IsCompletedSuccessfully);
    }

    [Fact]
    public void ExtraSpendUsesItsOwnUnitsAndKeepsNullCapsAndUnknownAmountsDistinct()
    {
        string Resource(string key) => key == "ExtraUsageAmountsFormat/Text" ? "Used {0}; cap {1}" : key;
        var extra = new ClaudeExtraUsage(false, ClaudeExtraUsageSource.Current, new(1250, 2, "EUR"), new(10, 0, "USD"), false);
        var text = ClaudeExtraUsageText.Format(extra, Resource);
        Assert.StartsWith("ExtraUsageDisabled/Text", text);
        Assert.Contains("EUR", text);
        Assert.Contains("10 USD", text);
        var unknown = ClaudeExtraUsageText.Format(extra with { Used = new(1250, null, null), Limit = null }, Resource);
        Assert.Contains("MoneyUnknown/Text", unknown);
        Assert.DoesNotContain("SpendingCapNotSet/Text", unknown);
        Assert.Contains("SpendingCapNotSet/Text", ClaudeExtraUsageText.Format(extra with { Limit = null, HasExplicitNullLimit = true }, Resource));
        Assert.Equal(string.Empty, ClaudeExtraUsageText.Format(null, Resource));
    }

    private static DashboardViewModel Card(DashboardWorkflow workflow, string name) =>
        new(workflow, _ => { }, key => key, action => { action(); return Task.CompletedTask; }, name, supportsManualCode: name == "Claude");
}
