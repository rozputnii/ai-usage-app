using System.Globalization;
using AiUsage.Core.Usage;
using AiUsage.Features.Presentation;

namespace AiUsage.Adapters.Live;

internal static class LiveMapping
{
    public static AccountItem Map(string provider, ProviderSessionState state, bool connected, ProviderCatalog? providers = null)
    {
        var quota = state.Quota;
        var connection = state.Status switch
        {
            ProviderSessionStatus.ReauthenticationRequired => ConnectionState.ReauthRequired,
            ProviderSessionStatus.RecoveryRequired => ConnectionState.RecoveryRequired,
            _ => connected ? ConnectionState.Connected : ConnectionState.NotConnected
        };
        var extensions = new List<ExtensionItem>();
        if (quota?.Credits is { } credits)
            extensions.Add(new(ExtensionKind.Credits, "Credits", credits.HasCredits,
                credits.Balance?.ToString(CultureInfo.InvariantCulture), null, null, null, null, credits.Unlimited));
        if (state.ExtraUsage is { } extra)
        {
            // A single presentation currency/exponent is safe only when both reported amounts agree.
            var compatible = extra.Used is null || extra.Limit is null ||
                (extra.Used.Currency == extra.Limit.Currency && extra.Used.Exponent == extra.Limit.Exponent);
            var money = extra.Used ?? extra.Limit;
            extensions.Add(new(ExtensionKind.ExtraUsage, "Extra usage", extra.Enabled, extra.Used?.AmountMinor?.ToString(CultureInfo.InvariantCulture),
                compatible ? money?.Exponent : null, compatible ? money?.Currency : null,
                extra.Used?.AmountMinor?.ToString(CultureInfo.InvariantCulture),
                extra.Limit?.AmountMinor?.ToString(CultureInfo.InvariantCulture), null)
                { HasExplicitNullLimit = extra.HasExplicitNullLimit });
        }
        var groups = quota?.Groups.Select(group => new GroupItem(Qualify(provider, group.Id), group.Name ?? group.Id, null, false,
            ExpansionPreference.Auto, group.Windows.Select(window => new WindowItem(Qualify(provider, group.Id, window.Id), window.Id,
                window.RemainingPercent, window.UsedPercent,
                window.Unlimited == true ? ValueState.Unlimited : window.Amount?.Limit == 0 ? ValueState.Unknown : window.RemainingPercent == 0 ? ValueState.Exhausted :
                window.RemainingPercent is not null || window.UsedPercent is not null ? ValueState.Known : ValueState.Unknown,
                window.Amount is { } amount ? new NativeAmount(amount.Remaining?.ToString(CultureInfo.InvariantCulture),
                    amount.Used?.ToString(CultureInfo.InvariantCulture), amount.Limit?.ToString(CultureInfo.InvariantCulture), amount.Unit) : null,
                window.Duration?.TotalSeconds, window.ResetsAt, false)).ToArray())
            { Allowed = group.Allowed, LimitReached = group.LimitReached }).ToArray() ?? [];
        return new(provider, provider, (providers ?? ProviderCatalog.Default).Get(provider).Name, quota?.PlanType, connection,
            AccountOperation.Idle,
            state.Failure is not null || connection is ConnectionState.ReauthRequired or ConnectionState.RecoveryRequired
                ? Freshness.Stale : state.FromCache ? Freshness.Cached : quota is null ? Freshness.Unknown : Freshness.Fresh,
            quota?.FetchedAt ?? state.RetrievedAt, Failure(state.Failure),
            [new(provider + ":account", "Account", ContextKind.Account, false, groups)], extensions)
        {
            AvailableResetCredits = quota?.AvailableResetCredits,
            SpendControlReached = quota?.SpendControlReached,
            LimitReachedType = quota?.LimitReachedType
        };
    }

    // Length-prefixing preserves arbitrary provider IDs without collisions or normalization.
    private static string Qualify(params string[] parts) => string.Concat(parts.Select(part => part.Length + ":" + part));

    public static FailureItem? Failure(ProviderFailureKind? kind) => kind is null ? null : new(
        kind == ProviderFailureKind.AuthenticationRequired ? FailureKinds.InvalidGrant : kind.ToString()!,
        kind switch
        {
            ProviderFailureKind.AuthenticationRequired => "Failure_InvalidGrant",
            ProviderFailureKind.NetworkFailure => "Failure_NetworkFailure",
            ProviderFailureKind.RateLimited => "Failure_RateLimited",
            ProviderFailureKind.ProjectUnavailable => "Failure_ProjectUnavailable",
            ProviderFailureKind.RegistrationUnavailable => "Failure_RegistrationUnavailable",
            ProviderFailureKind.InternalError => "Failure_InternalError",
            _ => "Dialog_OperationFailed"
        }, null, kind is not (ProviderFailureKind.RecoveryRequired or ProviderFailureKind.StorageUnavailable or
            // Retrying cannot help until this device is configured; the action is not in the app.
            ProviderFailureKind.RegistrationUnavailable or ProviderFailureKind.InternalError));
}
