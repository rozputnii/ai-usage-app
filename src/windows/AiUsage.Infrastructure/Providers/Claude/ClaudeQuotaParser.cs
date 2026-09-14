using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using AiUsage.Core.Providers.Claude;
using AiUsage.Core.Usage;

namespace AiUsage.Infrastructure.Providers.Claude;

/// <summary>
/// Parses the subscription usage payload returned by Claude's OAuth usage endpoint.
/// The parser is deliberately independent of transport and never retains the input.
/// </summary>
public static class ClaudeQuotaParser
{
    private const int MaxPayloadBytes = 1024 * 1024;
    private const int MaxJsonDepth = 32;

    public static bool TryParse(
        ReadOnlyMemory<byte> utf8,
        DateTimeOffset fetchedAt,
        [NotNullWhen(true)] out ClaudeQuotaReading? reading)
    {
        reading = null;
        if (utf8.Length > MaxPayloadBytes)
            return false;

        try
        {
            using var document = JsonDocument.Parse(utf8, new JsonDocumentOptions
            {
                MaxDepth = MaxJsonDepth,
                CommentHandling = JsonCommentHandling.Disallow,
                AllowTrailingCommas = false
            });
            return TryParse(document.RootElement, fetchedAt, out reading);
        }
        catch (JsonException)
        {
            // A provider body is untrusted input. Do not expose parser exception text,
            // which can include excerpts from a body containing a credential-like value.
            reading = null;
            return false;
        }
    }

    internal static bool TryParse(
        JsonElement root,
        DateTimeOffset fetchedAt,
        [NotNullWhen(true)] out ClaudeQuotaReading? reading)
    {
        reading = null;
        try
        {
            if (!IsWithinMaxDepth(root) || root.ValueKind != JsonValueKind.Object || HasDuplicateRootSchemaProperties(root))
                return false;

            if (!HasKnownSchemaProperty(root) || !ValidateRootShapes(root))
                return false;

            if (!TryReadLegacyBucket(root, "five_hour", out var legacyFiveHour) ||
                !TryReadLegacyBucket(root, "seven_day", out var legacySevenDay) ||
                !TryReadLegacyBucket(root, "seven_day_opus", out var legacyOpus) ||
                !TryReadLegacyBucket(root, "seven_day_sonnet", out var legacySonnet))
                return false;

            var modernEntries = ReadModernEntries(root, out var validLimitsShape);
            if (!validLimitsShape)
                return false;

            var groups = new List<QuotaGroup>();
            var ids = new HashSet<string>(StringComparer.Ordinal);

            var session = FirstModernEntry(modernEntries, "session");
            var weeklyAll = FirstModernEntry(modernEntries, "weekly_all");

            AddSharedGroup(
                groups,
                ids,
                legacyFiveHour,
                session,
                "claude:5h",
                "Claude 5 Hour",
                "5h",
                TimeSpan.FromHours(5));
            AddSharedGroup(
                groups,
                ids,
                legacySevenDay,
                weeklyAll,
                "claude:7d",
                "Claude 7 Day",
                "7d",
                TimeSpan.FromDays(7));

            AddLegacyFamilyGroup(groups, ids, legacyOpus, "claude:7d:opus", "Claude 7 Day (Opus)");
            AddLegacyFamilyGroup(groups, ids, legacySonnet, "claude:7d:sonnet", "Claude 7 Day (Sonnet)");

            foreach (var entry in modernEntries)
            {
                // The legacy account-wide buckets are the canonical representation when
                // they contain data. The modern session/weekly_all rows are fallbacks;
                // retaining both would display one entitlement twice. Other scoped and
                // unknown rows are independent and are always retained.
                if (entry.Kind is "session" or "weekly_all")
                    continue;

                var idBase = entry.Kind == "weekly_scoped"
                    ? "claude:7d:scoped:" + Slug(entry.DisplayName, "entry-" + entry.Ordinal.ToString(CultureInfo.InvariantCulture))
                    : "claude:kind:" + Slug(entry.Kind, "entry-" + entry.Ordinal.ToString(CultureInfo.InvariantCulture));
                var id = UniqueId(ids, idBase);
                var duration = entry.Kind == "weekly_scoped" ? TimeSpan.FromDays(7) : (TimeSpan?)null;
                var windowId = entry.Kind == "weekly_scoped" ? "7d" : "entry-" + entry.Ordinal.ToString(CultureInfo.InvariantCulture);
                groups.Add(CreateGroup(
                    id,
                    entry.DisplayName ?? entry.Kind,
                    entry.Kind,
                    entry.Bucket,
                    windowId,
                    duration,
                    entry.IsActive));
            }

            var extraUsage = ParseExtraUsage(root);
            var quota = new QuotaSnapshot(
                fetchedAt,
                PlanType: null,
                groups.AsReadOnly(),
                Credits: null,
                AvailableResetCredits: null,
                SpendControlReached: null,
                LimitReachedType: null);

            reading = new ClaudeQuotaReading(quota, extraUsage);
            return true;
        }
        catch (JsonException)
        {
            reading = null;
            return false;
        }
    }

    private static bool HasKnownSchemaProperty(JsonElement root)
    {
        return root.TryGetProperty("five_hour", out _) ||
            root.TryGetProperty("seven_day", out _) ||
            root.TryGetProperty("seven_day_opus", out _) ||
            root.TryGetProperty("seven_day_sonnet", out _) ||
            root.TryGetProperty("limits", out _) ||
            root.TryGetProperty("extra_usage", out _) ||
            root.TryGetProperty("spend", out _);
    }

    private static bool IsWithinMaxDepth(JsonElement root)
    {
        var pending = new Stack<(JsonElement Element, int Depth)>();
        pending.Push((root, 0));
        while (pending.Count > 0)
        {
            var (element, depth) = pending.Pop();
            if (depth > MaxJsonDepth)
                return false;
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                    pending.Push((property.Value, depth + 1));
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                    pending.Push((item, depth + 1));
            }
        }
        return true;
    }

    private static bool HasDuplicateRootSchemaProperties(JsonElement root)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in root.EnumerateObject())
        {
            if (!IsKnownSchemaProperty(property.Name))
                continue;
            if (!seen.Add(property.Name))
                return true;
        }
        return false;
    }

    private static bool IsKnownSchemaProperty(string name)
    {
        return name is "five_hour" or "seven_day" or "seven_day_opus" or "seven_day_sonnet" or
            "limits" or "extra_usage" or "spend";
    }

    private static bool ValidateRootShapes(JsonElement root)
    {
        foreach (var name in new[] { "five_hour", "seven_day", "seven_day_opus", "seven_day_sonnet" })
        {
            if (!root.TryGetProperty(name, out var value))
                continue;
            if (value.ValueKind is not (JsonValueKind.Object or JsonValueKind.Null))
                return false;
            if (value.ValueKind == JsonValueKind.Object &&
                HasDuplicateProperties(value, "utilization", "resets_at"))
                return false;
        }

        if (root.TryGetProperty("limits", out var limits))
        {
            if (limits.ValueKind is not (JsonValueKind.Array or JsonValueKind.Null))
                return false;
            if (limits.ValueKind == JsonValueKind.Array && !ValidateLimitEntries(limits))
                return false;
        }

        if (root.TryGetProperty("extra_usage", out var legacyExtra))
        {
            if (legacyExtra.ValueKind is not (JsonValueKind.Object or JsonValueKind.Null))
                return false;
            if (legacyExtra.ValueKind == JsonValueKind.Object && HasDuplicateProperties(legacyExtra,
                    "is_enabled", "used_credits", "monthly_limit", "decimal_places", "currency"))
                return false;
        }

        if (root.TryGetProperty("spend", out var spend))
        {
            if (spend.ValueKind is not (JsonValueKind.Object or JsonValueKind.Null))
                return false;
            if (spend.ValueKind == JsonValueKind.Object &&
                (HasDuplicateProperties(spend, "enabled", "used", "limit") ||
                 !ValidateMoneyShape(spend, "used") ||
                 !ValidateMoneyShape(spend, "limit")))
                return false;
        }

        return true;
    }

    private static bool ValidateLimitEntries(JsonElement limits)
    {
        foreach (var item in limits.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                return false;
            if (HasDuplicateProperties(item, "kind", "percent", "resets_at", "scope", "is_active"))
                return false;

            var scope = Property(item, "scope");
            if (scope.ValueKind == JsonValueKind.Object)
            {
                if (HasDuplicateProperties(scope, "model"))
                    return false;
                var model = Property(scope, "model");
                if (model.ValueKind == JsonValueKind.Object && HasDuplicateProperties(model, "display_name"))
                    return false;
            }
        }
        return true;
    }

    private static bool ValidateMoneyShape(JsonElement parent, string name)
    {
        var value = Property(parent, name);
        return value.ValueKind is (JsonValueKind.Object or JsonValueKind.Null or JsonValueKind.Undefined) &&
            !HasDuplicateProperties(value, "amount_minor", "currency", "exponent");
    }

    private static bool HasDuplicateProperties(JsonElement element, params string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return false;

        var known = new HashSet<string>(names, StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            if (known.Contains(property.Name) && !seen.Add(property.Name))
                return true;
        }
        return false;
    }

    private static bool TryReadLegacyBucket(JsonElement root, string name, out Bucket? bucket)
    {
        bucket = null;
        if (!root.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
            return true;
        if (value.ValueKind != JsonValueKind.Object)
            return false;
        bucket = ParseBucket(value, "utilization");
        return true;
    }

    private static List<ModernEntry> ReadModernEntries(JsonElement root, out bool validShape)
    {
        validShape = true;
        var entries = new List<ModernEntry>();
        if (!root.TryGetProperty("limits", out var limits) || limits.ValueKind == JsonValueKind.Null)
            return entries;

        if (limits.ValueKind != JsonValueKind.Array)
        {
            validShape = false;
            return entries;
        }

        var ordinal = 0;
        foreach (var item in limits.EnumerateArray())
        {
            ordinal++;
            if (item.ValueKind != JsonValueKind.Object)
            {
                validShape = false;
                return entries;
            }

            if (!item.TryGetProperty("kind", out var kindValue) || kindValue.ValueKind != JsonValueKind.String)
            {
                validShape = false;
                return entries;
            }

            var kind = kindValue.GetString();
            if (string.IsNullOrEmpty(kind))
            {
                validShape = false;
                return entries;
            }

            var displayName = ReadDisplayName(item);
            var isActive = ReadBoolean(item, "is_active");
            entries.Add(new ModernEntry(
                kind,
                ParseBucket(item, "percent"),
                displayName,
                isActive,
                ordinal));
        }

        return entries;
    }

    private static ModernEntry? FirstModernEntry(IEnumerable<ModernEntry> entries, string kind)
    {
        foreach (var entry in entries)
        {
            if (!string.Equals(entry.Kind, kind, StringComparison.Ordinal))
                continue;
            if (entry.Bucket.HasData)
                return entry;
        }

        foreach (var entry in entries)
        {
            if (string.Equals(entry.Kind, kind, StringComparison.Ordinal))
                return entry;
        }

        return null;
    }

    private static void AddSharedGroup(
        List<QuotaGroup> groups,
        HashSet<string> ids,
        Bucket? legacy,
        ModernEntry? modern,
        string id,
        string name,
        string windowId,
        TimeSpan duration)
    {
        if (legacy is not null && legacy.HasData)
        {
            var allowed = modern?.IsActive;
            groups.Add(CreateGroup(id, name, null, legacy, windowId, duration, allowed));
            ids.Add(id);
            return;
        }

        if (modern is not null)
        {
            groups.Add(CreateGroup(id, name, null, modern.Bucket, windowId, duration, modern.IsActive));
            ids.Add(id);
            return;
        }

        if (legacy is not null)
        {
            groups.Add(CreateGroup(id, name, null, legacy, windowId, duration, null));
            ids.Add(id);
        }
    }

    private static void AddLegacyFamilyGroup(
        List<QuotaGroup> groups,
        HashSet<string> ids,
        Bucket? bucket,
        string id,
        string name)
    {
        if (bucket is null)
            return;
        groups.Add(CreateGroup(id, name, null, bucket, "7d", TimeSpan.FromDays(7), null));
        ids.Add(id);
    }

    private static QuotaGroup CreateGroup(
        string id,
        string? name,
        string? meteredFeature,
        Bucket bucket,
        string windowId,
        TimeSpan? duration,
        bool? allowed)
    {
        var windows = new List<QuotaWindow>(1);
        // A JSON object is an explicitly returned bucket even when its percentage
        // is null or malformed. Preserve that row with unknown values; only a JSON
        // null bucket means the provider did not return the window.
        var used = bucket.UsedPercent;
        windows.Add(new QuotaWindow(
            windowId,
            used,
            used is null ? null : 100 - used,
            duration,
            bucket.ResetsAt));

        return new QuotaGroup(
            id,
            name,
            meteredFeature,
            NormalModelSlug: null,
            // Claude's is_active ranks severity, rather than access entitlement.
            Allowed: null,
            LimitReached: null,
            windows.AsReadOnly());
    }

    private static ClaudeExtraUsage? ParseExtraUsage(JsonElement root)
    {
        if (root.TryGetProperty("spend", out var spend) && spend.ValueKind == JsonValueKind.Object)
        {
            var enabled = ReadBoolean(spend, "enabled");
            var currentUsed = ReadMoney(spend, "used");
            var explicitNullLimit = spend.TryGetProperty("limit", out var limitValue) &&
                limitValue.ValueKind == JsonValueKind.Null;
            var currentLimit = limitValue.ValueKind == JsonValueKind.Object
                ? ReadMoneyValue(limitValue)
                : null;
            return new ClaudeExtraUsage(
                enabled,
                ClaudeExtraUsageSource.Current,
                currentUsed,
                currentLimit,
                explicitNullLimit);
        }

        if (!root.TryGetProperty("extra_usage", out var legacy) || legacy.ValueKind != JsonValueKind.Object)
            return null;

        var legacyEnabled = ReadBoolean(legacy, "is_enabled");
        var exponent = ReadNonNegativeInt(legacy, "decimal_places");
        var currency = ReadCurrency(legacy, "currency");
        var legacyUsed = ReadLegacyMoney(legacy, "used_credits", exponent, currency);

        var explicitNullMonthlyLimit = legacy.TryGetProperty("monthly_limit", out var monthlyLimitValue) &&
            monthlyLimitValue.ValueKind == JsonValueKind.Null;
        var legacyLimit = monthlyLimitValue.ValueKind == JsonValueKind.Number
            ? ReadLegacyMoneyValue(monthlyLimitValue, exponent, currency)
            : null;

        return new ClaudeExtraUsage(
            legacyEnabled,
            ClaudeExtraUsageSource.Legacy,
            legacyUsed,
            legacyLimit,
            explicitNullMonthlyLimit);
    }

    private static ClaudeMoneyAmount? ReadMoney(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
            return null;
        return value.ValueKind == JsonValueKind.Object ? ReadMoneyValue(value) : null;
    }

    private static ClaudeMoneyAmount ReadMoneyValue(JsonElement value)
    {
        return new ClaudeMoneyAmount(
            ReadNullableLong(value, "amount_minor"),
            ReadNonNegativeInt(value, "exponent"),
            ReadCurrency(value, "currency"));
    }

    private static ClaudeMoneyAmount? ReadLegacyMoney(
        JsonElement parent,
        string name,
        int? exponent,
        string? currency)
    {
        if (!parent.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
            return null;
        return value.ValueKind == JsonValueKind.Number
            ? ReadLegacyMoneyValue(value, exponent, currency)
            : null;
    }

    private static ClaudeMoneyAmount ReadLegacyMoneyValue(
        JsonElement value,
        int? exponent,
        string? currency)
    {
        return new ClaudeMoneyAmount(ReadNullableLong(value), exponent, currency);
    }

    private static long? ReadNullableLong(JsonElement parent, string? name = null)
    {
        var value = name is null ? parent : Property(parent, name);
        return value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var result) && result >= 0 ? result : null;
    }

    private static int? ReadNonNegativeInt(JsonElement parent, string name)
    {
        var value = Property(parent, name);
        return value.ValueKind == JsonValueKind.Number &&
            value.TryGetInt32(out var result) &&
            result >= 0
            ? result
            : null;
    }

    private static string? ReadCurrency(JsonElement parent, string name)
    {
        var value = Property(parent, name);
        if (value.ValueKind != JsonValueKind.String)
            return null;
        var currency = value.GetString();
        return string.IsNullOrWhiteSpace(currency) ? null : currency;
    }

    private static bool? ReadBoolean(JsonElement parent, string name)
    {
        var value = Property(parent, name);
        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    private static string? ReadDisplayName(JsonElement entry)
    {
        if (!entry.TryGetProperty("scope", out var scope) || scope.ValueKind != JsonValueKind.Object ||
            !scope.TryGetProperty("model", out var model) || model.ValueKind != JsonValueKind.Object ||
            !model.TryGetProperty("display_name", out var displayName) ||
            displayName.ValueKind != JsonValueKind.String)
            return null;

        var value = displayName.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static Bucket ParseBucket(JsonElement value, string percentName)
    {
        var used = ReadPercent(value, percentName);
        var reset = ReadReset(value, "resets_at");
        return new Bucket(used, reset, used.HasValue || reset.HasValue);
    }

    private static double? ReadPercent(JsonElement parent, string name)
    {
        var value = Property(parent, name);
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out var result) ||
            !double.IsFinite(result) || result < 0 || result > 100)
            return null;
        return result;
    }

    private static DateTimeOffset? ReadReset(JsonElement parent, string name)
    {
        var value = Property(parent, name);
        if (value.ValueKind != JsonValueKind.String)
            return null;

        var text = value.GetString();
        if (string.IsNullOrWhiteSpace(text) || !HasTimeZone(text) ||
            !DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
            return null;
        return parsed;
    }

    private static bool HasTimeZone(string value)
    {
        var text = value.Trim();
        if (text.EndsWith('Z') || text.EndsWith('z'))
            return true;

        if (text.Length >= 6)
        {
            var offsetStart = text.Length - 6;
            if ((text[offsetStart] is '+' or '-') && text[offsetStart + 3] == ':' &&
                IsAsciiDigit(text[offsetStart + 1]) && IsAsciiDigit(text[offsetStart + 2]) &&
                IsAsciiDigit(text[offsetStart + 4]) && IsAsciiDigit(text[offsetStart + 5]))
                return true;
        }

        if (text.Length >= 5)
        {
            var offsetStart = text.Length - 5;
            if ((text[offsetStart] is '+' or '-') &&
                IsAsciiDigit(text[offsetStart + 1]) && IsAsciiDigit(text[offsetStart + 2]) &&
                IsAsciiDigit(text[offsetStart + 3]) && IsAsciiDigit(text[offsetStart + 4]))
                return true;
        }

        return false;
    }

    private static bool IsAsciiDigit(char value) => value is >= '0' and <= '9';

    private static JsonElement Property(JsonElement parent, string name) =>
        parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(name, out var value) ? value : default;

    private static string Slug(string? value, string fallback)
    {
        var source = string.IsNullOrWhiteSpace(value) ? fallback : value!.Trim();
        var chars = new List<char>(Math.Min(source.Length, 64));
        var separator = false;
        foreach (var character in source)
        {
            var lower = char.ToLowerInvariant(character);
            if (lower is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                if (separator && chars.Count > 0)
                    chars.Add('-');
                if (chars.Count < 64)
                    chars.Add(lower);
                separator = false;
            }
            else
            {
                separator = chars.Count > 0;
            }
        }

        return chars.Count == 0 ? fallback : new string(chars.ToArray()).Trim('-');
    }

    private static string UniqueId(HashSet<string> ids, string idBase)
    {
        var id = idBase;
        for (var suffix = 2; !ids.Add(id); suffix++)
            id = idBase + ":" + suffix.ToString(CultureInfo.InvariantCulture);
        return id;
    }

    private sealed record Bucket(double? UsedPercent, DateTimeOffset? ResetsAt, bool HasData);

    private sealed record ModernEntry(
        string Kind,
        Bucket Bucket,
        string? DisplayName,
        bool? IsActive,
        int Ordinal);
}
