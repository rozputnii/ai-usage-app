using AiUsage.Core.Providers.Copilot;
using System.Globalization;

namespace AiUsage.Features.Dashboard;

/// <summary>
/// Documented billing consumption in provider units. Lines with different unit types are never
/// added together, and no remaining allowance or percentage is derived.
/// </summary>
internal static class CopilotUsageText
{
    internal static string Format(CopilotUsageReading? reading, Func<string, string> resource)
    {
        if (reading is null) return string.Empty;
        return string.Join(Environment.NewLine, new[]
        {
            Report(reading.AiCredits, "CopilotAiCredits/Text", resource),
            // A legacy report is shown only when GitHub returned one with usage.
            reading.PremiumRequests is { Items.Count: > 0 } ? Report(reading.PremiumRequests, "CopilotPremiumRequests/Text", resource) : null
        }.OfType<string>());
    }

    private static string Report(CopilotUsageReport? report, string label, Func<string, string> resource)
    {
        var name = resource(label);
        if (report is null)
            return string.Format(CultureInfo.CurrentCulture, resource("CopilotReportMissing/Text"), name);
        var period = report.Month is { } month
            ? report.Day is { } day ? $"{report.Year:D4}-{month:D2}-{day:D2}" : $"{report.Year:D4}-{month:D2}"
            : report.Year.ToString("D4", CultureInfo.InvariantCulture);
        var lines = new List<string> { string.Format(CultureInfo.CurrentCulture, resource("CopilotReportHeaderFormat/Text"), name, period) };
        if (report.Items.Count == 0)
            lines.Add(resource("CopilotNoUsage/Text"));
        foreach (var unit in report.Items.GroupBy(item => item.UnitType, StringComparer.Ordinal).OrderBy(group => group.Key, StringComparer.Ordinal))
            lines.Add(string.Format(CultureInfo.CurrentCulture, resource("CopilotUsageLineFormat/Text"), unit.Key,
                Number(unit.Sum(item => item.GrossQuantity)), Number(unit.Sum(item => item.DiscountQuantity)),
                Number(unit.Sum(item => item.NetQuantity)), Number(unit.Sum(item => item.NetAmount))));
        return string.Join(Environment.NewLine, lines);
    }

    private static string Number(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero).ToString("#,##0.##", CultureInfo.CurrentCulture);
}
