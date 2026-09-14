using System.Globalization;
using AiUsage.Core.Usage;

namespace AiUsage.Features.Dashboard;

/// <summary>
/// One displayed quota window. An absent percentage or reset time renders as an explicit unknown
/// label, never as zero, and an exhausted window is stated as exhausted rather than as an empty bar.
/// </summary>
public sealed class QuotaWindowItem
{
    internal QuotaWindowItem(QuotaGroup group, QuotaWindow window)
    {
        Name = string.IsNullOrWhiteSpace(group.Name) ? group.Id : group.Name!;
        if (group.Windows.Count > 1)
            Name += " - " + DescribeWindow(window);
        Remaining = window.RemainingPercent is { } remaining
            ? string.Format(CultureInfo.CurrentCulture, App.Resource("RemainingFormat/Text"), Math.Round(remaining))
            : App.Resource("RemainingUnknown/Text");
        if (group.LimitReached == true || window.RemainingPercent == 0)
            Remaining = App.Resource("RemainingExhausted/Text");
        Resets = window.ResetsAt is { } resets
            ? string.Format(CultureInfo.CurrentCulture, App.Resource("ResetsFormat/Text"), resets.ToLocalTime().ToString("g", CultureInfo.CurrentCulture))
            : App.Resource("ResetsUnknown/Text");
    }

    public string Name { get; }
    public string Remaining { get; }
    public string Resets { get; }

    private static string DescribeWindow(QuotaWindow window) => window.Duration is { } duration
        ? duration.TotalHours >= 24
            ? string.Format(CultureInfo.CurrentCulture, App.Resource("WindowDaysFormat/Text"), Math.Round(duration.TotalDays))
            : string.Format(CultureInfo.CurrentCulture, App.Resource("WindowHoursFormat/Text"), Math.Round(duration.TotalHours))
        : window.Id;
}
