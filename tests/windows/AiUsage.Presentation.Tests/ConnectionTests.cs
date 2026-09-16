using AiUsage.Features.Connection;
using AiUsage.Features.Demo;
using AiUsage.Features.Presentation;
using Xunit;

namespace AiUsage.Presentation.Tests;

/// <summary>F05 connection stages, outcomes and the transient manual code.</summary>
public sealed class ConnectionTests
{
    [Fact]
    public void DemoProviderHelpUsesAnExistingResource()
    {
        using var host = new TestHost();
        Assert.Contains("Choose a provider", host.AddAccount().ProviderHelp);
    }
    private static async Task<(TestHost Host, AddAccountViewModel Sheet, Task Running)> StartWaiting(string provider = "codex")
    {
        var host = new TestHost(autoDelays: false);
        var sheet = host.AddAccount();
        sheet.Open(new(AddAccountTab.SignIn));
        Assert.True(sheet.IsPick);
        Assert.Equal(["Available", "Available", "Planned · demo only", "Planned · demo only"], sheet.ProviderOptions.Select(p => p.Availability));
        sheet.PickProviderCommand.Execute(sheet.ProviderOptions.Single(p => p.ProviderId == provider));
        Assert.True(sheet.IsMethod);
        var running = sheet.StartCommand.ExecuteAsync(null);
        Assert.True(sheet.IsConnecting);
        Assert.Equal($"Connecting to {sheet.Provider!.Name}…", sheet.ConnectingText);
        await host.Delays.Advance();
        Assert.True(sheet.IsWaiting);
        Assert.True(sheet.IsBusy);
        return (host, sheet, running);
    }

    [Fact]
    public async Task WaitingNeverImpliesCompletionAndApproveAddsTheAccount()
    {
        var (host, sheet, running) = await StartWaiting();
        using var _ = host;
        Assert.Equal(5, host.Usage.Current.Accounts.Count);
        Assert.Equal("Finish signing in to Codex in your browser. Nothing is connected until the provider confirms.", sheet.WaitingText);
        Assert.True(sheet.SupportsManualCode);
        host.Controller.ResolveConnection(DemoConnectOutcome.Approve);
        await running;
        Assert.True(sheet.IsResult);
        Assert.Equal(("Connected", true), (sheet.ResultTitle, sheet.ResultPositive));
        Assert.Equal("demo-codex-new3", sheet.ResultAccountId);
        Assert.Equal(6, host.Usage.Current.Accounts.Count);
        sheet.OpenResultCommand.Execute(null);
        Assert.Equal(new NavigationRequest(PageKey.Accounts, "demo-codex-new3"), host.Navigation.Last);
    }

    [Theory]
    [InlineData(DemoConnectOutcome.Deny, "Authorization was denied. Nothing was changed.")]
    [InlineData(DemoConnectOutcome.Expire, "The authorization request expired. Try again when ready.")]
    public async Task DeniedAndExpiredReturnToMethodWithCriticalNoteAndNoChange(DemoConnectOutcome outcome, string note)
    {
        var (host, sheet, running) = await StartWaiting();
        using var _ = host;
        host.Controller.ResolveConnection(outcome);
        await running;
        Assert.True(sheet.IsMethod);
        Assert.Equal((note, NoteTone.Critical), (sheet.Note, sheet.NoteSeverity));
        Assert.Equal(5, host.Usage.Current.Accounts.Count);
    }

    [Fact]
    public async Task CancelIsNeutralAndNotAFailure()
    {
        var (host, sheet, running) = await StartWaiting();
        using var _ = host;
        sheet.CancelCommand.Execute(null);
        await running;
        Assert.True(sheet.IsMethod);
        Assert.Equal(("Cancelled. Nothing was changed.", NoteTone.Neutral), (sheet.Note, sheet.NoteSeverity));
        Assert.Equal(5, host.Usage.Current.Accounts.Count);
    }

    [Fact]
    public async Task SuccessWithoutQuotaIsConnectedAndUnavailableNotDisconnected()
    {
        var (host, sheet, running) = await StartWaiting("copilot");
        using var _ = host;
        Assert.False(sheet.SupportsManualCode);
        host.Controller.ResolveConnection(DemoConnectOutcome.ApproveWithoutQuota);
        await running;
        Assert.Equal("Connected · quota unavailable", sheet.ResultTitle);
        var account = host.Account(sheet.ResultAccountId!);
        Assert.Equal(ConnectionState.Connected, account.Connection);
        var pill = new StatusPillViewModel();
        pill.Update(account, host.Format);
        Assert.Equal("Connected · quota unavailable", pill.Text);
    }

    [Fact]
    public async Task DuplicateIdentityFocusesTheExistingAccountWithoutCreatingOne()
    {
        var (host, sheet, running) = await StartWaiting("claude");
        using var _ = host;
        host.Controller.ResolveConnection(DemoConnectOutcome.Duplicate);
        await running;
        Assert.Equal(("Already connected", false), (sheet.ResultTitle, sheet.ResultPositive));
        Assert.Equal("This identity is already connected as “Research”. No duplicate was created.", sheet.ResultBody);
        Assert.Equal(5, host.Usage.Current.Accounts.Count);
        sheet.OpenResultCommand.Execute(null);
        Assert.Equal("demo-claude-1", host.Navigation.Last!.AccountId);
    }

    [Fact]
    public async Task ManualCodeValidatesLengthIsClearedOnSubmitAndIsNeverInTheSnapshot()
    {
        using var host = new TestHost(autoDelays: false);
        var sheet = host.AddAccount();
        sheet.Open(new(AddAccountTab.SignIn, "claude"));
        sheet.SelectMethodCommand.Execute(sheet.Methods.Single(m => m.Method == ConnectionMethod.ManualCode));
        Assert.Equal("Continue", sheet.StartLabel);
        await sheet.StartCommand.ExecuteAsync(null);
        Assert.True(sheet.IsCode);
        sheet.Code = "short";
        await sheet.SubmitCodeCommand.ExecuteAsync(null);
        Assert.Equal("Enter the full code shown by the provider (at least 8 characters).", sheet.CodeError);
        Assert.True(sheet.IsCode);
        sheet.Code = "SYNTHETIC-CODE-1234";
        Assert.Empty(sheet.CodeError);
        var submit = sheet.SubmitCodeCommand.ExecuteAsync(null);
        Assert.Empty(sheet.Code);
        Assert.True(sheet.IsVerifying);
        await host.Delays.Drain();
        await submit;
        Assert.Equal("Connected", sheet.ResultTitle);
        Assert.DoesNotContain("SYNTHETIC-CODE-1234", System.Text.Json.JsonSerializer.Serialize(host.Usage.Current));
    }

    [Fact]
    public async Task EnterACodeInsteadLeavesWaitingWithoutACancellationNote()
    {
        var (host, sheet, running) = await StartWaiting();
        using var _ = host;
        sheet.EnterCodeInsteadCommand.Execute(null);
        await running;
        Assert.True(sheet.IsCode);
        Assert.Empty(sheet.Note);
        sheet.CancelCommand.Execute(null);
        Assert.True(sheet.IsMethod);
        Assert.Equal("Cancelled. Nothing was changed.", sheet.Note);
    }

    [Fact]
    public async Task SecurityBlockDisablesConnectionsWithAScopedNote()
    {
        using var host = new TestHost("F14b");
        var sheet = host.AddAccount();
        sheet.Open(new(AddAccountTab.SignIn, "codex"));
        await sheet.StartCommand.ExecuteAsync(null);
        Assert.True(sheet.IsMethod);
        Assert.Equal("Connections are disabled by a security block until you update.", sheet.Note);
        Assert.Equal(5, host.Usage.Current.Accounts.Count);
    }
}
