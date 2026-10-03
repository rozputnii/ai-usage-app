using AiUsage.Core.Usage;

namespace AiUsage.Core.Accounts;

public enum AccountOutcome { Done, Duplicate, Busy, Cancelled, Failed }
public sealed record AccountResult(AccountOutcome Outcome, Guid? AccountId = null, ProviderFailureKind? Failure = null);
public sealed record AccountSnapshot(Guid AccountId, string Provider, bool Connected,
    ProviderSessionState Session, bool Busy, DateTimeOffset? LastFailureAt);

/// <summary>Account operations expose app-owned references, never provider identities or credentials.</summary>
public interface IAccountService
{
    IReadOnlyList<AccountSnapshot> Current { get; }
    event EventHandler? Changed;
    Task InitializeAsync(CancellationToken token);
    Task<AccountResult> ConnectAsync(string provider, Guid? reconnectAccountId, Guid attemptId,
        Action<AuthorizationChallenge> authorize, CancellationToken token);
    Task CancelConnectAsync(Guid attemptId);
    bool TrySubmitCode(Guid attemptId, string code);
    Task<AccountResult> RefreshAsync(Guid accountId, CancellationToken token);
    Task<AccountResult> DisconnectAsync(Guid accountId, CancellationToken token);
    Task StopAsync();
}
