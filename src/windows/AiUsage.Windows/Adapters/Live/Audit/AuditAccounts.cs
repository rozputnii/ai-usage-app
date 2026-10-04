using AiUsage.Core.Accounts;
using AiUsage.Core.Usage;
using AiUsage.Features.Ledger.Contract;

namespace AiUsage.Adapters.Live.Audit;

/// <summary>Offline account boundary. Requests have synthetic target receipts; no HTTP/browser/credential access exists.</summary>
internal sealed class AuditAccounts(AuditInput input, AuditClock clock, Action<string> receipt, Func<bool>? initializationReleased = null) : IAccountService, IDisposable
{
    private readonly CancellationTokenSource shutdown = new();
    private TaskCompletionSource<AccountResult>? login;
    private Guid? attempt;
    private readonly List<Task> pending = [];
    private int added;
    public IReadOnlyList<AccountSnapshot> Current { get; private set; } = input.Accounts;
    public event EventHandler? Changed;
    public Task InitializeAsync(CancellationToken token)
    {
        var task = InitializeCoreAsync(token);
        pending.Add(task);
        return task;
    }
    private async Task InitializeCoreAsync(CancellationToken token)
    {
        receipt("InitializeStarted");
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, shutdown.Token);
        try
        {
            while (input.BlockInitialization && initializationReleased?.Invoke() != true)
                await Task.Delay(50, linked.Token);
            receipt("InitializeCompleted");
        }
        catch (OperationCanceledException) { receipt("InitializeCancelled"); }
    }

    public async Task<AccountResult> ConnectAsync(string provider, Guid? reconnectAccountId, Guid attemptId,
        Action<AuthorizationChallenge> authorize, CancellationToken token)
    {
        receipt("Connect:" + provider + ":" + reconnectAccountId?.ToString("N"));
        attempt = attemptId;
        login = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var completion = login;
        authorize(new(new Uri("https://synthetic.invalid/authorization"), "SYNTHETIC-ONLY", clock.GetUtcNow().AddMinutes(10)));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, shutdown.Token);
        using var cancel = linked.Token.Register(() => completion.TrySetResult(new(AccountOutcome.Cancelled)));
        if (!input.ManualCode)
        {
            var automatic = CompleteAfterDelayAsync(completion, linked.Token);
            pending.Add(automatic);
        }
        var result = await completion.Task;
        if (attempt != attemptId || result.Outcome != AccountOutcome.Done) return result;
        var existing = Current.FirstOrDefault(a => a.AccountId == reconnectAccountId);
        var id = reconnectAccountId ?? new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, (byte)(++added + 100), 1);
        var template = input.Accounts.FirstOrDefault(a => a.Provider == provider)?.Session
            ?? input.NextAccounts?.FirstOrDefault(a => a.Provider == provider)?.Session ?? ProviderSessionState.NotConnected;
        var account = existing is null ? new AccountSnapshot(id, provider, true, template, false, null)
            : existing with { Connected = true, Session = existing.Session with { Status = ProviderSessionStatus.QuotaAvailable, Failure = null } };
        Current = existing is null ? [.. Current, account] : [.. Current.Select(a => a.AccountId == id ? account : a)];
        Changed?.Invoke(this, EventArgs.Empty);
        return new(AccountOutcome.Done, id);
    }
    private async Task CompleteAfterDelayAsync(TaskCompletionSource<AccountResult> completion, CancellationToken token)
    {
        try { await Task.Delay(1500, token); completion.TrySetResult(Result()); }
        catch (OperationCanceledException) { completion.TrySetResult(new(AccountOutcome.Cancelled)); }
    }
    private AccountResult Result() => input.SignInFailure switch
    {
        null => new(AccountOutcome.Done),
        SignInFailure.Duplicate => new(AccountOutcome.Duplicate),
        var reason => new(AccountOutcome.Failed, Failure: reason switch
        {
            SignInFailure.WrongAccount => ProviderFailureKind.AccountMismatch,
            SignInFailure.Storage => ProviderFailureKind.StorageUnavailable,
            SignInFailure.AccessDenied => ProviderFailureKind.AccessDenied,
            SignInFailure.Expired => ProviderFailureKind.LoginAttemptExpired,
            SignInFailure.Browser => ProviderFailureKind.BrowserCallbackUnavailable,
            SignInFailure.Registration => ProviderFailureKind.RegistrationUnavailable,
            _ => ProviderFailureKind.ProviderUnavailable
        })
    };
    public Task CancelConnectAsync(Guid attemptId)
    {
        receipt("CancelConnect");
        if (attempt == attemptId) login?.TrySetResult(new(AccountOutcome.Cancelled));
        return Task.CompletedTask;
    }
    public bool TrySubmitCode(Guid attemptId, string code)
    {
        var accepted = attempt == attemptId && code == "synthetic-code" && login?.TrySetResult(Result()) == true;
        receipt("SubmitCode:" + (accepted ? "Accepted" : "Rejected"));
        return accepted;
    }
    public async Task<AccountResult> RefreshAsync(Guid accountId, CancellationToken token)
    {
        receipt("Refresh:" + accountId.ToString("N"));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, shutdown.Token);
        if (input.BlockRefresh)
        {
            var wait = Task.Delay(Timeout.Infinite, linked.Token);
            pending.Add(wait);
            try { await wait; } catch (OperationCanceledException) { receipt("RefreshCancelled:" + accountId.ToString("N")); return new(AccountOutcome.Cancelled); }
        }
        if (input.NextNow is { } next) clock.Now = next;
        var replacement = input.NextAccounts?.FirstOrDefault(a => a.AccountId == accountId);
        if (replacement is not null) Current = [.. Current.Select(a => a.AccountId == accountId ? replacement : a)];
        Changed?.Invoke(this, EventArgs.Empty);
        return new(AccountOutcome.Done, accountId);
    }
    public Task<AccountResult> DisconnectAsync(Guid accountId, CancellationToken token)
    {
        receipt("Disconnect:" + accountId.ToString("N"));
        Current = [.. Current.Select(a => a.AccountId == accountId ? a with { Connected = false } : a)];
        Changed?.Invoke(this, EventArgs.Empty);
        return Task.FromResult(new AccountResult(AccountOutcome.Done, accountId));
    }
    public void Clear() { Current = []; Changed?.Invoke(this, EventArgs.Empty); }
    public async Task StopAsync()
    {
        await shutdown.CancelAsync();
        try { await Task.WhenAll(pending); } catch (OperationCanceledException) { }
        receipt("Stopped");
    }
    public void Dispose() => shutdown.Dispose();
}

internal sealed class AuditClock(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;
    public override DateTimeOffset GetUtcNow() => Now;
}
