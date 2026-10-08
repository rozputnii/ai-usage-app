using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AiUsage.Features.Ledger.Contract;

namespace AiUsage.Features.Ledger;

/// <summary>
/// Display formatting for the new presentation (spec 4.6). Interface text is English. Percentages are whole numbers (D-186),
/// counts use thousands separators and money keeps its currency's minor units. Local times are shown as given: the source
/// supplies each time with its own local offset, so a reset after a daylight-saving change still reads as its local clock.
/// </summary>
internal static class LedgerFormat
{
    private static readonly CultureInfo En = CultureInfo.InvariantCulture;
    public const string Minus = "−";

    public static decimal Round(decimal value, int decimals = 0) => Math.Round(value, decimals, MidpointRounding.AwayFromZero);

    /// <summary>A value in the card's scale: "47 %", "1,210", "$218.00". Negative values use a true minus sign.</summary>
    public static string Value(ScaleModel scale, decimal value)
    {
        var sign = value < 0 ? Minus : string.Empty;
        var magnitude = Math.Abs(value);
        return scale.Kind switch
        {
            ScaleKind.Percent => sign + Round(magnitude).ToString("0", En) + " %",
            ScaleKind.Money => sign + Money(scale.Currency, scale.Exponent, magnitude),
            _ => sign + Round(magnitude).ToString("#,0", En),
        };
    }

    public static string Money(string? currency, int? exponent, decimal amount)
    {
        var digits = Math.Clamp(exponent ?? 2, 0, 18);
        var number = Round(amount, digits).ToString("#,0." + new string('0', digits), En).TrimEnd('.');
        return currency switch
        {
            "USD" => "$" + number,
            "EUR" => "€" + number,
            "GBP" => "£" + number,
            null or "" => number,
            _ => currency + " " + number,
        };
    }

    public static string NativeMoney(MonetaryAmount? money)
    {
        if (money is null) return "unknown";
        if (money.Exponent is not (>= 0 and <= 18))
            return money.MinorUnits.ToString(En) + " minor units (" + (money.Currency ?? "currency unknown") + "; exponent unknown)";
        decimal scale = 1;
        for (int i = 0; i < money.Exponent; i++) scale *= 10;
        return (money.MinorUnits / scale).ToString("F" + money.Exponent, En) + " " + (money.Currency ?? "(currency unknown)");
    }

    /// <summary>A percentage with one decimal, as used for day-off share previews ("16.7 %").</summary>
    public static string Percent1(decimal value) => Round(value, 1).ToString("0.0", En) + " %";

    public static string Unit(ScaleModel scale) => scale.Kind switch
    {
        ScaleKind.Money => scale.Currency ?? "money",
        ScaleKind.Count => scale.UnitName ?? "units",
        _ => "%",
    };

    public static string PeriodLabel(PeriodModel period) => period.Kind switch
    {
        PeriodKind.CalendarMonth => "month",
        PeriodKind.Unknown => "window",
        _ when period.Duration is { } d && d.TotalDays >= 1 => ((int)Math.Round(d.TotalDays)).ToString(En) + "d",
        _ => "window",
    };

    public static string PeriodWords(PeriodModel period) => period.Kind switch
    {
        PeriodKind.CalendarMonth => "month",
        PeriodKind.Unknown => "window",
        _ when period.Duration is { } d && d.TotalDays >= 1 => ((int)Math.Round(d.TotalDays)).ToString(En) + " day",
        _ => "window",
    };

    public static string Clock(DateTimeOffset time) => time.ToString("HH:mm", En);
    public static string DayDate(DateTimeOffset time) => time.ToString("ddd d MMM", En);
    public static string DayMonth(DateOnly date) => date.ToString("d MMM", En);
    public static string WeekdayName(DayOfWeek day) => En.DateTimeFormat.GetDayName(day);
    public static string WeekdayShort(DayOfWeek day) => En.DateTimeFormat.GetAbbreviatedDayName(day);

    /// <summary>"Mon 09:00" within the coming six days, "21:00" today, otherwise "1 Nov" (with the time unless midnight).</summary>
    public static string When(DateTimeOffset at, DateTimeOffset now)
    {
        var days = at.Date.Subtract(now.Date).TotalDays;
        if (days == 0)
            return Clock(at);
        if (days is > 0 and <= 6)
            return at.ToString("ddd", En) + " " + Clock(at);
        var date = at.ToString("d MMM", En);
        return at.TimeOfDay == TimeSpan.Zero ? date : date + " " + Clock(at);
    }

    /// <summary>"in 1 d 19 h 10 min" or "20 min ago"; zero parts are left out.</summary>
    public static string Relative(DateTimeOffset at, DateTimeOffset now)
    {
        var span = at - now;
        var past = span < TimeSpan.Zero;
        var text = Duration(past ? -span : span);
        return past ? text + " ago" : "in " + text;
    }

    /// <summary>AIU-046 Settings status line. A target version is never shown: Windows does not report it.</summary>
    public static string UpdateText(UpdateStatus status) => status.State switch
    {
        UpdateState.NotPackaged => "Updates unavailable in development build",
        UpdateState.NoFeed => "Updates unavailable without an update feed",
        UpdateState.Checking => "Checking…",
        UpdateState.UpToDate => "Up to date" + (status.CheckedAt is { } at ? " · checked " + at.ToLocalTime().ToString("HH:mm", En) : string.Empty),
        UpdateState.Available => "New version available",
        UpdateState.Ready => "Update ready",
        UpdateState.Installing => "Downloading and installing · the app will restart",
        UpdateState.CheckFailed => "Check failed · " + (status.ErrorCode is { } code ? "0x" + code.ToString("X8", En) : "will retry"),
        UpdateState.InstallFailed => "Install failed" + (status.ErrorCode is { } code ? " · 0x" + code.ToString("X8", En) : string.Empty),
        UpdateState.NotApplied => "Automatic update didn't apply · install manually",
        _ => "Not checked yet",
    };

    public static string Duration(TimeSpan span)
    {
        var minutes = (long)Math.Floor(span.TotalMinutes);
        var d = minutes / 1440;
        var h = minutes % 1440 / 60;
        var m = minutes % 60;
        var parts = new List<string>(3);
        if (d > 0) parts.Add(d.ToString(En) + " d");
        if (h > 0) parts.Add(h.ToString(En) + " h");
        if (m > 0 || parts.Count == 0) parts.Add(m.ToString(En) + " min");
        return string.Join(' ', parts);
    }

    /// <summary>"42 min old" below an hour, then whole hours.</summary>
    public static string Age(DateTimeOffset reading, DateTimeOffset now)
    {
        var minutes = Math.Max(0, (int)Math.Floor((now - reading).TotalMinutes));
        return minutes < 60 ? minutes.ToString(En) + " min old" : (minutes / 60).ToString(En) + " h old";
    }

    public static string AgeWords(DateTimeOffset reading, DateTimeOffset now)
    {
        var minutes = Math.Max(0, (int)Math.Floor((now - reading).TotalMinutes));
        return minutes < 60 ? minutes.ToString(En) + " minutes old" : (minutes / 60).ToString(En) + (minutes / 60 == 1 ? " hour old" : " hours old");
    }

    public static string ProviderName(ProviderKind provider) => provider switch
    {
        ProviderKind.Copilot => "GitHub Copilot",
        _ => provider.ToString(),
    };

    /// <summary>Spoken form of a short figure: "47 %" becomes "47 percent", "$2.00" becomes "2 dollars".</summary>
    public static string Spoken(string text)
    {
        var builder = new StringBuilder(text.Replace(" %", " percent", StringComparison.Ordinal).Replace(Minus, "minus ", StringComparison.Ordinal));
        var value = builder.ToString();
        if (value.Contains('$', StringComparison.Ordinal))
            value = value.Replace("$", string.Empty, StringComparison.Ordinal).Replace(".00", string.Empty, StringComparison.Ordinal) + " dollars";
        return value.Replace("≈ ", "about ", StringComparison.Ordinal).Replace(" × 5h", " five-hour windows", StringComparison.Ordinal);
    }

    public static bool TryParseAmount(string text, ScaleModel scale, out decimal amount)
    {
        amount = 0;
        var cleaned = text.Trim();
        if (scale.Kind == ScaleKind.Money && scale.Currency is { Length: > 0 } code)
        {
            var symbol = code switch { "USD" => "$", "EUR" => "€", "GBP" => "£", _ => code };
            if (cleaned.StartsWith(symbol, StringComparison.OrdinalIgnoreCase)) cleaned = cleaned[symbol.Length..].Trim();
            else if (cleaned.StartsWith(code, StringComparison.OrdinalIgnoreCase)) cleaned = cleaned[code.Length..].Trim();
            else if (cleaned.EndsWith(code, StringComparison.OrdinalIgnoreCase)) cleaned = cleaned[..^code.Length].Trim();
        }
        if (!Regex.IsMatch(cleaned, @"\A(?:(?:[0-9]+|[0-9]{1,3}(?:,[0-9]{3})+)(?:\.[0-9]*)?|\.[0-9]+)\z", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)) ||
            !decimal.TryParse(cleaned, NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands, En, out var parsed) || parsed < 0)
            return false;
        var digits = scale.Kind == ScaleKind.Money ? Math.Clamp(scale.Exponent ?? 2, 0, 18) : 0;
        if (decimal.Round(parsed, digits) != parsed)
            return false;
        amount = parsed;
        return true;
    }

    /// <summary>The editable form of an amount: digits and a decimal point only, which the cap editor accepts as typed.</summary>
    public static string EditText(ScaleModel scale, decimal amount) => scale.Kind == ScaleKind.Money
        ? Round(amount, Math.Clamp(scale.Exponent ?? 2, 0, 18)).ToString("0." + new string('0', Math.Clamp(scale.Exponent ?? 2, 0, 18)), En).TrimEnd('.')
        : Round(amount).ToString("0", En);
}
