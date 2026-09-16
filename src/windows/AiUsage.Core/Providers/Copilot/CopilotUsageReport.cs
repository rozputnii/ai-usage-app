namespace AiUsage.Core.Providers.Copilot;

public enum CopilotUsageReportKind
{
    /// <summary>Usage-based billing, measured in GitHub AI Credits.</summary>
    AiCredits,
    /// <summary>Legacy request-based billing retained by some annual plans.</summary>
    PremiumRequests
}

/// <summary>
/// One documented billing usage line. Product, SKU, model and unit names stay opaque.
/// Gross is consumption, discount is the included allowance applied, net is billed usage.
/// </summary>
public sealed record CopilotUsageItem(
    string Product,
    string Sku,
    string? Model,
    string UnitType,
    decimal PricePerUnit,
    decimal GrossQuantity,
    decimal GrossAmount,
    decimal DiscountQuantity,
    decimal DiscountAmount,
    decimal NetQuantity,
    decimal NetAmount);

/// <summary>
/// A documented personal billing usage report for one period. It reports consumption,
/// not a remaining allowance; no entitlement or percentage is derived from it.
/// </summary>
public sealed record CopilotUsageReport(
    CopilotUsageReportKind Kind,
    int Year,
    int? Month,
    int? Day,
    IReadOnlyList<CopilotUsageItem> Items,
    DateTimeOffset ObservedAt);

/// <summary>
/// The last documented reports for one account. Either report may be absent when GitHub does not
/// provide it; absence is unknown, never zero usage.
/// </summary>
public sealed record CopilotUsageReading(
    CopilotUsageReport? AiCredits,
    CopilotUsageReport? PremiumRequests,
    DateTimeOffset FetchedAt);
