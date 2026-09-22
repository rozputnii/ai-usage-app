namespace AiUsage.Features.Presentation;

public enum QuotaSeverity { None, Normal, Warning, Critical, Exhausted }

/// <summary>Tray and preview priority (D-126 as resolved in D9): higher is more urgent.</summary>
public enum AttentionLevel { Normal = 0, Stale = 1, Warning = 2, Critical = 3, Exhausted = 4, Failure = 5, ReauthRequired = 6 }

public sealed record ResolvedThresholds(IReadOnlyList<int> Remaining, RuleScope Source);

/// <summary>Quota semantics shared by every surface. Pure functions over contract records.</summary>
public static class QuotaRules
{
    public static string WindowTypeKey(string providerId, string windowLabel) => providerId + "::" + windowLabel;

    /// <summary>Window → Account → WindowType → Provider → Global; an inheriting rule is skipped.</summary>
    public static ResolvedThresholds Resolve(IReadOnlyList<NotificationRule> rules, AccountItem account, WindowItem window)
    {
        NotificationRule? Find(RuleScope scope, string? target) =>
            rules.FirstOrDefault(rule => rule.Scope == scope && rule.TargetId == target && !rule.Inherit);
        var rule = Find(RuleScope.Window, window.Id)
            ?? Find(RuleScope.Account, account.Id)
            ?? Find(RuleScope.WindowType, WindowTypeKey(account.ProviderId, window.Label))
            ?? Find(RuleScope.Provider, account.ProviderId)
            ?? Find(RuleScope.Global, null);
        return rule is null
            ? new(Preferences.DefaultRemainingThresholds, RuleScope.Global)
            : new(rule.RemainingThresholds, rule.Scope);
    }

    /// <summary>Parent value for a rule at <paramref name="scope"/>, i.e. what it inherits when it has no override.</summary>
    public static ResolvedThresholds ResolveParent(IReadOnlyList<NotificationRule> rules, RuleScope scope, AccountItem? account, WindowItem? window, string? providerId)
    {
        NotificationRule? Find(RuleScope s, string? target) => rules.FirstOrDefault(rule => rule.Scope == s && rule.TargetId == target && !rule.Inherit);
        var chain = new List<(RuleScope Scope, string? Target)>();
        if (scope == RuleScope.Window && account is not null)
            chain.Add((RuleScope.Account, account.Id));
        if (scope is RuleScope.Window or RuleScope.Account && account is not null && window is not null)
            chain.Add((RuleScope.WindowType, WindowTypeKey(account.ProviderId, window.Label)));
        if (scope is RuleScope.Window or RuleScope.Account or RuleScope.WindowType)
            chain.Add((RuleScope.Provider, account?.ProviderId ?? providerId));
        if (scope != RuleScope.Global)
            chain.Add((RuleScope.Global, null));
        foreach (var (s, target) in chain)
            if (Find(s, target) is { } rule)
                return new(rule.RemainingThresholds, rule.Scope);
        return new(Preferences.DefaultRemainingThresholds, RuleScope.Global);
    }

    /// <summary>
    /// Exhausted at 0 % remaining or an explicit Exhausted state. Otherwise, of the nonzero thresholds crossed
    /// (remaining ≤ threshold), the smallest nonzero threshold is critical and larger ones are warnings.
    /// Unknown, Unavailable and Unlimited carry no quota severity.
    /// </summary>
    public static QuotaSeverity Classify(WindowItem window, IReadOnlyList<int> remainingThresholds)
    {
        if (window.ValueState == ValueState.Exhausted || (window.ValueState == ValueState.Known && window.RemainingPercent is <= 0))
            return QuotaSeverity.Exhausted;
        if (window.ValueState != ValueState.Known || window.RemainingPercent is not { } remaining)
            return QuotaSeverity.None;
        var nonzero = remainingThresholds.Where(t => t > 0).ToArray();
        if (nonzero.Length == 0)
            return QuotaSeverity.Normal;
        var crossed = nonzero.Where(t => remaining <= t).ToArray();
        if (crossed.Length == 0)
            return QuotaSeverity.Normal;
        return crossed.Contains(nonzero.Min()) ? QuotaSeverity.Critical : QuotaSeverity.Warning;
    }

    public static IEnumerable<GroupItem> VisibleGroups(ContextItem? context, Preferences preferences) =>
        context is null ? [] : context.Groups.Where(group => preferences.ShowHidden || !preferences.HiddenTargets.Contains(group.Id));

    public static ContextItem? SelectedContext(AccountItem account, Preferences preferences)
    {
        var available = account.Contexts.Where(context => context.Available).ToArray();
        if (preferences.ContextSelection.TryGetValue(account.Id, out var selected) && available.FirstOrDefault(c => c.Id == selected) is { } match)
            return match;
        return available.FirstOrDefault() ?? (account.Contexts.Count == 0 ? null : account.Contexts[0]);
    }

    public static WindowItem? PrimaryWindow(AccountItem account, Preferences preferences)
    {
        var groups = VisibleGroups(SelectedContext(account, preferences), preferences).ToArray();
        return groups.SelectMany(group => group.Windows).FirstOrDefault(window => window.Primary)
            ?? (groups.Length == 0 || groups[0].Windows.Count == 0 ? null : groups[0].Windows[0]);
    }

    /// <param name="includeFailures">False ranks by quota state only, as notification previews do; the tray includes refresh failures.</param>
    public static AttentionLevel Attention(AccountItem account, Preferences preferences, bool includeFailures = true)
    {
        if (account.Connection == ConnectionState.ReauthRequired)
            return AttentionLevel.ReauthRequired;
        if (includeFailures && account.Failure is not null)
            return AttentionLevel.Failure;
        var worst = QuotaSeverity.None;
        foreach (var group in VisibleGroups(SelectedContext(account, preferences), preferences))
            foreach (var window in group.Windows)
            {
                var severity = Classify(window, Resolve(preferences.NotificationRules, account, window).Remaining);
                if (severity > worst)
                    worst = severity;
            }
        return worst switch
        {
            QuotaSeverity.Exhausted => AttentionLevel.Exhausted,
            QuotaSeverity.Critical => AttentionLevel.Critical,
            QuotaSeverity.Warning => AttentionLevel.Warning,
            _ => account.Freshness == Freshness.Stale ? AttentionLevel.Stale : AttentionLevel.Normal,
        };
    }

    public static bool IsVisible(AccountItem account, Preferences preferences) =>
        (preferences.ShowHidden || !preferences.HiddenTargets.Contains(account.Id))
        && (preferences.ShowDisconnected || account.Connection != ConnectionState.NotConnected);

    public static IReadOnlyList<AccountItem> Ordered(UiSnapshot snapshot)
    {
        var byId = snapshot.Accounts.ToDictionary(account => account.Id);
        var ordered = snapshot.Preferences.AccountOrder.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
        ordered.AddRange(snapshot.Accounts.Where(account => !snapshot.Preferences.AccountOrder.Contains(account.Id)));
        return ordered;
    }

    public static bool IsAvailable(UiSnapshot snapshot, string key, string? targetId = null)
    {
        var exact = targetId is null ? null : snapshot.Capabilities.FirstOrDefault(c => c.Key == key && c.TargetId == targetId);
        var entry = exact ?? snapshot.Capabilities.FirstOrDefault(c => c.Key == key && c.TargetId is null);
        return entry?.Availability == Availability.Available;
    }
}

/// <summary>Summary for Overview (D-115) and the tray. Compares known percentages only; shared pools count once.</summary>
public sealed record OverviewSummary(
    int AccountCount,
    int ProviderCount,
    double? LowestRemaining,
    AccountItem? LowestAccount,
    WindowItem? LowestWindow,
    DateTimeOffset? NearestReset,
    AccountItem? NearestAccount,
    WindowItem? NearestWindow,
    int Warning,
    int Critical,
    int Exhausted,
    int ReauthRequired)
{
    public int NeedAttention => Warning + Critical + Exhausted + ReauthRequired;

    public static OverviewSummary Compute(IReadOnlyList<AccountItem> visible, Preferences preferences, DateTimeOffset now)
    {
        var seenPools = new HashSet<string>(StringComparer.Ordinal);
        double? lowest = null;
        AccountItem? lowestAccount = null;
        WindowItem? lowestWindow = null;
        DateTimeOffset? nearest = null;
        AccountItem? nearestAccount = null;
        WindowItem? nearestWindow = null;
        int warning = 0, critical = 0, exhausted = 0, reauth = 0;
        foreach (var account in visible)
        {
            if (account.Connection == ConnectionState.ReauthRequired)
                reauth++;
            var worst = QuotaSeverity.None;
            foreach (var group in QuotaRules.VisibleGroups(QuotaRules.SelectedContext(account, preferences), preferences))
            {
                if (group.SharedPoolId is not null && !seenPools.Add(group.SharedPoolId))
                    continue;
                foreach (var window in group.Windows)
                {
                    double? remaining = window.ValueState switch
                    {
                        ValueState.Exhausted => 0,
                        ValueState.Known => window.RemainingPercent,
                        _ => null,
                    };
                    if (remaining is { } value && (lowest is null || value < lowest))
                    {
                        lowest = value;
                        lowestAccount = account;
                        lowestWindow = window;
                    }
                    if (window.ResetsAt is { } reset && reset > now && (nearest is null || reset < nearest))
                    {
                        nearest = reset;
                        nearestAccount = account;
                        nearestWindow = window;
                    }
                    var severity = QuotaRules.Classify(window, QuotaRules.Resolve(preferences.NotificationRules, account, window).Remaining);
                    if (severity > worst)
                        worst = severity;
                }
            }
            if (worst == QuotaSeverity.Exhausted) exhausted++;
            else if (worst == QuotaSeverity.Critical) critical++;
            else if (worst == QuotaSeverity.Warning) warning++;
        }
        return new(visible.Count, visible.Select(a => a.ProviderId).Distinct().Count(), lowest, lowestAccount, lowestWindow,
            nearest, nearestAccount, nearestWindow, warning, critical, exhausted, reauth);
    }
}
