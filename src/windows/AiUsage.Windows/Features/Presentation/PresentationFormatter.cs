using System.Globalization;

namespace AiUsage.Features.Presentation;

/// <summary>Culture-aware text for times, amounts and money. Uses the Windows culture and the clock's time zone.</summary>
internal sealed class PresentationFormatter(ITextResources text, IClock clock)
{
    private static CultureInfo Culture => CultureInfo.CurrentCulture;

    public ITextResources Text => text;
    public IClock Clock => clock;

    public string T(string key) => text.Get(key);

    public string F(string key, params object?[] args) => string.Format(Culture, text.Get(key), args);

    public string Percent(double value) => F("Format_Percent", Math.Round(value, MidpointRounding.AwayFromZero).ToString("0", Culture));

    public string Count(long value) => value.ToString("N0", Culture);

    private DateTime Local(DateTimeOffset value) => TimeZoneInfo.ConvertTime(value, clock.TimeZone).DateTime;

    public string Time(DateTimeOffset value) => Local(value).ToString(Culture.DateTimeFormat.ShortTimePattern, Culture);

    public string MonthDay(DateTimeOffset value)
    {
        var pattern = Culture.DateTimeFormat.MonthDayPattern.Replace("MMMM", "MMM", StringComparison.Ordinal);
        return Local(value).ToString(pattern, Culture);
    }

    /// <summary>Exact local timestamp, e.g. "Sep 15, 3:00 PM".</summary>
    public string DateTime(DateTimeOffset value) => F("Format_DateTime", MonthDay(value), Time(value));

    public string FullDateTime(DateTimeOffset value) => Local(value).ToString("F", Culture);

    /// <summary>Relative future time such as "in 2 h 14 m"; null when the instant is not in the future.</summary>
    public string? Relative(DateTimeOffset target)
    {
        var delta = target - clock.UtcNow;
        if (delta <= TimeSpan.Zero)
            return null;
        var minutes = Math.Max(1, (int)Math.Round(delta.TotalMinutes, MidpointRounding.AwayFromZero));
        if (minutes < 60)
            return F("Time_InMinutes", minutes);
        var hours = minutes / 60;
        if (hours < 24)
            return minutes % 60 == 0 ? F("Time_InHours", hours) : F("Time_InHoursMinutes", hours, minutes % 60);
        var days = hours / 24;
        return hours % 24 == 0 ? F("Time_InDays", days) : F("Time_InDaysHours", days, hours % 24);
    }

    public string Duration(double seconds)
    {
        var whole = (long)Math.Round(seconds);
        if (whole > 0 && whole % 86400 == 0)
            return F("Duration_Days", whole / 86400);
        if (whole > 0 && whole % 3600 == 0)
            return F("Duration_Hours", whole / 3600);
        return F("Duration_Minutes", Math.Max(1, (long)Math.Round(seconds / 60)));
    }

    /// <summary>Formats a native decimal string with culture grouping, or returns it verbatim when it is not numeric.</summary>
    public string NativeNumber(string value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
            ? number.ToString("#,0.############", Culture)
            : value;

    /// <summary>Money only when currency and exponent are both present; never inferred.</summary>
    public string? Money(string? minor, int? exponent, string? currency)
    {
        if (minor is null || exponent is null || string.IsNullOrWhiteSpace(currency) || exponent < 0 || exponent > 8)
            return null;
        if (!decimal.TryParse(minor, NumberStyles.Integer, CultureInfo.InvariantCulture, out var amountMinor))
            return null;
        var amount = amountMinor / (decimal)Math.Pow(10, exponent.Value);
        var format = (NumberFormatInfo)Culture.NumberFormat.Clone();
        format.CurrencyDecimalDigits = exponent.Value;
        format.CurrencySymbol = CurrencySymbol(currency) ?? currency + " ";
        return amount.ToString("C", format);
    }

    private static string? CurrencySymbol(string isoCode)
    {
        var region = new RegionInfo(Culture.Name.Length > 0 ? Culture.Name : "en-US");
        if (string.Equals(region.ISOCurrencySymbol, isoCode, StringComparison.OrdinalIgnoreCase))
            return region.CurrencySymbol;
        foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
        {
            try
            {
                var candidate = new RegionInfo(culture.Name);
                if (string.Equals(candidate.ISOCurrencySymbol, isoCode, StringComparison.OrdinalIgnoreCase))
                    return candidate.CurrencySymbol;
            }
            catch (ArgumentException) { }
        }
        return null;
    }
}
