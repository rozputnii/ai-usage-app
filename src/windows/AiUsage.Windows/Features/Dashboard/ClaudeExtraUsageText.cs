using AiUsage.Core.Providers.Claude;
using System.Globalization;

namespace AiUsage.Features.Dashboard;

internal static class ClaudeExtraUsageText
{
    internal static string Format(ClaudeExtraUsage? extra, Func<string, string> resource)
    {
        if (extra is null) return string.Empty;
        var enabled = resource(extra.Enabled switch { true => "ExtraUsageEnabled/Text", false => "ExtraUsageDisabled/Text", _ => "ExtraUsageUnknown/Text" });
        var used = Money(extra.Used, resource);
        var limit = extra.HasExplicitNullLimit ? resource("SpendingCapNotSet/Text") : Money(extra.Limit, resource);
        return enabled + " " + string.Format(CultureInfo.CurrentCulture, resource("ExtraUsageAmountsFormat/Text"), used, limit);
    }

    private static string Money(ClaudeMoneyAmount? money, Func<string, string> resource)
    {
        if (money is not { AmountMinor: >= 0, Exponent: >= 0 and <= 28, Currency.Length: > 0 })
            return resource("MoneyUnknown/Text");
        var factor = 1m;
        for (var index = 0; index < money.Exponent; index++) factor *= 10;
        return (money.AmountMinor.Value / factor).ToString("G29", CultureInfo.CurrentCulture) + " " + money.Currency;
    }
}
