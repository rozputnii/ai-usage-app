using AiUsage.Features.Connection;
using AiUsage.Features.Demo;
using AiUsage.Features.Presentation;
using Xunit;

namespace AiUsage.Presentation.Tests;

/// <summary>F05 inline connection: one provider click signs in, outcomes, and the transient manual code.</summary>
public sealed class ConnectionTests
{
    private static async Task<(TestHost Host, AddAccountViewModel Connect, Task Running)> StartWaiting(string provider = "codex")
    {
        var host = new TestHost(autoDelays: false);
        var connect = host.AddAccount();
        Assert.False(connect.ShowStrip);
        Assert.Equal(["", "", "Planned · demo only", "Planned · demo only"], connect.ProviderOptions.Select(p => p.Availability));
        var option = connect.ProviderOptions.Single(p => p.ProviderId == provider);
        // One click: no provider, method or confirmation step before the browser opens.
        var running = connect.ConnectCommand.ExecuteAsync(option);
        Assert.True(connect.IsConnecting);
        Assert.True(connect.ShowStrip);
        Assert.Equal($"Connecting to {option.Name}…", connect.StatusText);
        Assert.Equal(("Signing in…", false), (option.Availability, option.IsEnabled));
        await host.Delays.Advance();
        Assert.True(connect.IsWaiting);
        Assert.True(connect.IsBusy);
        return (host, connect, running);
    }

    [Fact]
    public async Task OneClickWaitsForTheBrowserAndApproveAddsTheAccountWithoutConfirmation()
    {
        var (host, connect, running) = await StartWaiting();
        using var _ = host;
        Assert.Equal(5, host.Usage.Current.Accounts.Count);
        Assert.Equal("Finish signing in to Codex in your browser. Nothing is connected until the provider confirms.", connect.StatusText);
        Assert.True(connect.CanEnterCode);
        host.Controller.ResolveConnection(DemoConnectOutcome.Approve);
        await running;
        Assert.False(connect.ShowStrip);
        Assert.Empty(connect.Note);
        Assert.Equal(6, host.Usage.Current.Accounts.Count);
        Assert.Equal("Codex account 3 connected", host.Announcer.Last);
        Assert.Equal(PageKey.Overview, host.Navigation.Current);
        Assert.Equal(("", true), (connect.ProviderOptions[0].Availability, connect.ProviderOptions[0].IsEnabled));
    }

    [Theory]
    [InlineData(DemoConnectOutcome.Deny, "Authorization was denied. Nothing was changed.")]
    [InlineData(DemoConnectOutcome.Expire, "The authorization request expired. Try again when ready.")]
    public async Task DeniedAndExpiredShowACriticalNoteWithTryAgainAndNoChange(DemoConnectOutcome outcome, string note)
    {
        var (host, connect, running) = await StartWaiting();
        using var _ = host;
        host.Controller.ResolveConnection(outcome);
        await running;
        Assert.False(connect.IsActive);
        Assert.Equal((note, NoteTone.Critical, true), (connect.Note, connect.NoteSeverity, connect.CanRetry));
        Assert.Equal(5, host.Usage.Current.Accounts.Count);

        var retry = connect.RetryCommand.ExecuteAsync(null);
        Assert.True(connect.IsConnecting);
        Assert.Empty(connect.Note);
        connect.CancelCommand.Execute(null);
        await host.Delays.Drain();
        await retry;
    }

    [Fact]
    public async Task CancelIsNeutralAndLeavesNoNote()
    {
        var (host, connect, running) = await StartWaiting();
        using var _ = host;
        connect.CancelCommand.Execute(null);
        await running;
        Assert.False(connect.ShowStrip);
        Assert.Empty(connect.Note);
        Assert.Equal("Cancelled. Nothing was changed.", host.Announcer.Last);
        Assert.Equal(5, host.Usage.Current.Accounts.Count);
    }

    [Fact]
    public async Task SuccessWithoutQuotaIsConnectedAndUnavailableNotDisconnected()
    {
        var (host, connect, running) = await StartWaiting("copilot");
        using var _ = host;
        Assert.False(connect.SupportsManualCode);
        Assert.False(connect.CanEnterCode);
        host.Controller.ResolveConnection(DemoConnectOutcome.ApproveWithoutQuota);
        await running;
        Assert.False(connect.ShowStrip);
        var account = host.Account("demo-copilot-new2");
        Assert.Equal(ConnectionState.Connected, account.Connection);
        var pill = new StatusPillViewModel();
        pill.Update(account, host.Format);
        Assert.Equal("Connected · quota unavailable", pill.Text);
    }

    [Fact]
    public async Task DuplicateIdentityExplainsTheExistingAccountWithoutCreatingOne()
    {
        var (host, connect, running) = await StartWaiting("claude");
        using var _ = host;
        host.Controller.ResolveConnection(DemoConnectOutcome.Duplicate);
        await running;
        Assert.Equal(("This identity is already connected as “Research”. No duplicate was created.", NoteTone.Neutral, false),
            (connect.Note, connect.NoteSeverity, connect.CanRetry));
        Assert.Equal(5, host.Usage.Current.Accounts.Count);
        connect.DismissNoteCommand.Execute(null);
        Assert.False(connect.ShowStrip);
    }

    [Fact]
    public async Task ManualCodeValidatesLengthIsClearedOnSubmitAndIsNeverInTheSnapshot()
    {
        var (host, connect, running) = await StartWaiting("claude");
        using var _ = host;
        connect.EnterCodeInsteadCommand.Execute(null);
        await running;
        Assert.True(connect.IsCode);
        Assert.Empty(connect.Note);
        Assert.Equal("Enter the code shown by Claude", connect.StatusText);
        connect.Code = "short";
        await connect.SubmitCodeCommand.ExecuteAsync(null);
        Assert.Equal("Enter the full code shown by the provider (at least 8 characters).", connect.CodeError);
        Assert.True(connect.IsCode);
        connect.Code = "SYNTHETIC-CODE-1234";
        Assert.Empty(connect.CodeError);
        var submit = connect.SubmitCodeCommand.ExecuteAsync(null);
        Assert.Empty(connect.Code);
        Assert.True(connect.IsVerifying);
        await host.Delays.Drain();
        await submit;
        Assert.False(connect.ShowStrip);
        Assert.Equal(6, host.Usage.Current.Accounts.Count);
        Assert.DoesNotContain("SYNTHETIC-CODE-1234", System.Text.Json.JsonSerializer.Serialize(host.Usage.Current));
    }

    [Fact]
    public async Task CancellingCodeEntryIsNeutral()
    {
        var (host, connect, running) = await StartWaiting("claude");
        using var _ = host;
        connect.EnterCodeInsteadCommand.Execute(null);
        await running;
        connect.Code = "SYNTHETIC-CODE-1234";
        connect.CancelCommand.Execute(null);
        Assert.False(connect.ShowStrip);
        Assert.Empty(connect.Code);
        Assert.Equal(5, host.Usage.Current.Accounts.Count);
    }

    [Fact]
    public async Task AnotherProviderClickReplacesTheRunningAttemptWithoutACancellationNote()
    {
        var (host, connect, running) = await StartWaiting();
        using var _ = host;
        var claude = connect.ProviderOptions.Single(p => p.ProviderId == "claude");
        var second = connect.ConnectCommand.ExecuteAsync(claude);
        await running;
        Assert.Equal(claude, connect.Provider);
        Assert.True(connect.IsConnecting);
        Assert.Empty(connect.Note);
        Assert.True(connect.ProviderOptions[0].IsEnabled);
        connect.CancelCommand.Execute(null);
        await host.Delays.Drain();
        await second;
    }

    [Fact]
    public async Task SecurityBlockDisablesConnectionsWithAScopedNote()
    {
        using var host = new TestHost("F14b");
        var connect = host.AddAccount();
        await connect.ConnectCommand.ExecuteAsync(connect.ProviderOptions.Single(p => p.ProviderId == "codex"));
        Assert.False(connect.IsActive);
        Assert.Equal(("Connections are disabled by a security block until you update.", NoteTone.Critical), (connect.Note, connect.NoteSeverity));
        Assert.Equal(5, host.Usage.Current.Accounts.Count);
    }

    [Fact]
    public void CliImportOpensInlineOnlyWhereTheCapabilityExists()
    {
        using var host = new TestHost();
        var connect = host.AddAccount();
        Assert.True(connect.CanImportFromCli);
        connect.OpenCliImportCommand.Execute(null);
        Assert.True(connect.IsCliOpen);
        connect.CloseCliImportCommand.Execute(null);
        Assert.False(connect.IsCliOpen);
    }
}
