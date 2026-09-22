using AiUsage.Core.Usage;
using System.Globalization;
using System.Text.Json;

namespace AiUsage.Infrastructure.Providers.Antigravity;

/// <summary>
/// Normalizes retrieveUserQuotaSummary. The provider's own grouping is preserved: buckets are not
/// re-attributed to model families, a shared group is never duplicated into per-family quotas, and
/// a missing field stays unknown rather than becoming zero, unlimited or exhausted.
/// </summary>
public static class AntigravityQuotaParser
{
    private const int MaximumGroups = 64;
    private const int MaximumWindows = 256;

    public static QuotaSnapshot Parse(ReadOnlyMemory<byte> json, DateTimeOffset fetchedAt, string? planType = null)
    {
        if (json.Length > 1024 * 1024) throw new ProviderException(ProviderFailureKind.InvalidResponse);
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 32 });
            return Parse(document.RootElement, fetchedAt, planType);
        }
        catch (JsonException) { throw new ProviderException(ProviderFailureKind.InvalidResponse); }
    }

    internal static QuotaSnapshot Parse(JsonElement root, DateTimeOffset fetchedAt, string? planType)
    {
        if (root.ValueKind != JsonValueKind.Object)
            throw new ProviderException(ProviderFailureKind.InvalidResponse);
        var groupsField = AntigravityAuthClient.Property(root, "groups");
        var bucketsField = AntigravityAuthClient.Property(root, "buckets");
        // Neither shape present means this is not a quota summary at all; an empty one is reported as empty.
        if (groupsField.ValueKind == JsonValueKind.Undefined && bucketsField.ValueKind == JsonValueKind.Undefined)
            throw new ProviderException(ProviderFailureKind.InvalidResponse);

        List<QuotaGroup> groups = [];
        var windowCount = 0;
        var identifiers = new HashSet<string>(StringComparer.Ordinal);
        if (Array(groupsField) is { } grouped)
        {
            foreach (var group in grouped.EnumerateArray())
            {
                if (group.ValueKind != JsonValueKind.Object)
                    throw new ProviderException(ProviderFailureKind.InvalidResponse);
                var name = Label(group, "displayName") ?? Label(group, "description");
                groups.Add(Group(Identifier(identifiers, name, groups.Count), name,
                    AntigravityAuthClient.Property(group, "buckets"), ref windowCount));
                if (groups.Count > MaximumGroups) throw new ProviderException(ProviderFailureKind.InvalidResponse);
            }
        }
        // A grouped response is authoritative; top-level buckets carry the limits only without groups.
        if (groups.Count == 0 && bucketsField.ValueKind != JsonValueKind.Undefined)
        {
            var single = Group("default", Label(root, "description"), bucketsField, ref windowCount);
            // An empty top-level array reports nothing; it does not become an empty provider group.
            if (single.Windows.Count > 0 || single.Allowed == false)
                groups.Add(single);
        }
        return new(fetchedAt, planType, groups, null, null, null, null);
    }

    private static QuotaGroup Group(string id, string? name, JsonElement buckets, ref int windowCount)
    {
        List<QuotaWindow> windows = [];
        var identifiers = new HashSet<string>(StringComparer.Ordinal);
        var present = 0;
        var disabled = 0;
        if (Array(buckets) is { } array)
        {
            foreach (var bucket in array.EnumerateArray())
            {
                if (bucket.ValueKind != JsonValueKind.Object)
                    throw new ProviderException(ProviderFailureKind.InvalidResponse);
                present++;
                if (++windowCount > MaximumWindows) throw new ProviderException(ProviderFailureKind.InvalidResponse);
                // A disabled bucket does not apply to this account; it is dropped rather than shown as zero.
                if (Boolean(bucket, "disabled") == true) { disabled++; continue; }
                windows.Add(Window(bucket, identifiers, windows.Count));
            }
        }
        else if (buckets.ValueKind is not (JsonValueKind.Undefined or JsonValueKind.Null))
            throw new ProviderException(ProviderFailureKind.InvalidResponse);
        // Only a group the provider disabled outright is reported as not allowed; nothing else is inferred.
        var allowed = present > 0 && disabled == present ? false : (bool?)null;
        return new(id, name ?? id, null, null, allowed, null, windows);
    }

    private static QuotaWindow Window(JsonElement bucket, HashSet<string> identifiers, int index)
    {
        var token = Label(bucket, "window");
        var id = Identifier(identifiers, Label(bucket, "bucketId") ?? token, index);
        var remainingPercent = RemainingPercent(bucket);
        var remaining = Number(bucket, "remainingAmount");
        return new(id,
            remainingPercent is null ? null : (double)(100 - remainingPercent.Value),
            remainingPercent is null ? null : (double)remainingPercent.Value,
            // Duration comes from the provider's own window token; an unrecognized token stays opaque.
            token?.ToLowerInvariant() switch
            {
                "weekly" or "week" or "7d" => TimeSpan.FromDays(7),
                "daily" or "day" or "24h" => TimeSpan.FromDays(1),
                "5h" => TimeSpan.FromHours(5),
                _ => null
            },
            Reset(bucket))
        {
            // The endpoint states neither a limit nor an unlimited flag; the raw remaining amount is
            // kept with an unknown unit so it is never read as a percentage or a request count.
            Amount = remaining is null ? null : new(remaining, null, null, "unknown"),
            SourceDetails = new(Label(bucket, "bucketId"), remaining, null, null)
        };
    }

    private static string Identifier(HashSet<string> taken, string? candidate, int index)
    {
        var original = candidate is { Length: > 0 and <= 256 } ? candidate : index.ToString(CultureInfo.InvariantCulture);
        // Opaque provider labels can repeat; a positional suffix keeps them distinct without renaming.
        var id = original;
        for (var suffix = index; !taken.Add(id); suffix++)
            id = original + "#" + suffix.ToString(CultureInfo.InvariantCulture);
        return id;
    }

    private static JsonElement? Array(JsonElement value) => value.ValueKind == JsonValueKind.Array ? value : null;

    private static string? Label(JsonElement root, string key)
    {
        var text = AntigravityAuthClient.Text(AntigravityAuthClient.Property(root, key));
        return text is { Length: > 0 and <= 256 } ? text : null;
    }

    private static DateTimeOffset? Reset(JsonElement root)
    {
        var text = AntigravityAuthClient.Text(AntigravityAuthClient.Property(root, "resetTime"));
        return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed) ? parsed : null;
    }

    /// <summary>A fraction outside 0..1 has no meaning here and stays unknown instead of being clamped.</summary>
    private static decimal? RemainingPercent(JsonElement bucket)
    {
        var value = Number(bucket, "remainingFraction");
        return value is >= 0 and <= 1 ? value * 100 : null;
    }

    private static decimal? Number(JsonElement root, string key)
    {
        var value = AntigravityAuthClient.Property(root, key);
        if (value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return null;
        // The summary reports some amounts as numeric strings; both forms carry the same value.
        if (value.ValueKind == JsonValueKind.String)
            return decimal.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var text) ? text : null;
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out var number))
            throw new ProviderException(ProviderFailureKind.InvalidResponse);
        return number;
    }

    private static bool? Boolean(JsonElement root, string key)
    {
        var value = AntigravityAuthClient.Property(root, key);
        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Undefined or JsonValueKind.Null => null,
            _ => throw new ProviderException(ProviderFailureKind.InvalidResponse)
        };
    }
}
