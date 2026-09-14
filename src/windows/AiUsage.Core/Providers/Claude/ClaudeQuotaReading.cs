using AiUsage.Core.Usage;

namespace AiUsage.Core.Providers.Claude;

public enum ClaudeExtraUsageSource
{
    Unknown,
    Legacy,
    Current
}

/// <summary>
/// A provider monetary value kept in the units reported by Claude.
/// Missing components stay unknown; no currency or exponent is inferred.
/// </summary>
public sealed record ClaudeMoneyAmount(
    long? AmountMinor,
    int? Exponent,
    string? Currency);

/// <summary>
/// Claude's optional extra-usage state, separate from subscription percentage windows.
/// </summary>
public sealed record ClaudeExtraUsage(
    bool? Enabled,
    ClaudeExtraUsageSource Source,
    ClaudeMoneyAmount? Used,
    ClaudeMoneyAmount? Limit,
    bool HasExplicitNullLimit);

public sealed record ClaudeQuotaReading(
    QuotaSnapshot Quota,
    ClaudeExtraUsage? ExtraUsage);
