using AiUsage.Core.Usage;
using System.Globalization;
using System.Text.Json;

namespace AiUsage.Infrastructure.Providers.Copilot;

internal static class CopilotQuotaParser
{
    public static QuotaSnapshot Parse(ReadOnlyMemory<byte> json, DateTimeOffset fetchedAt)
    {
        if (json.Length > 1024 * 1024) throw new ProviderException(ProviderFailureKind.InvalidResponse);
        try
        {
            using var doc = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 32 });
            return Parse(doc.RootElement, fetchedAt);
        }
        catch (JsonException) { throw new ProviderException(ProviderFailureKind.InvalidResponse); }
    }
    internal static QuotaSnapshot Parse(JsonElement root, DateTimeOffset fetchedAt)
    {
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("quota_snapshots", out var snapshots) || snapshots.ValueKind != JsonValueKind.Object)
            throw new ProviderException(ProviderFailureKind.InvalidResponse);
        var resetText = CopilotAuthClient.Text(root, "quota_reset_date");
        DateTimeOffset? reset = DateTimeOffset.TryParse(resetText, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed) ? parsed : null;
        List<QuotaGroup> groups = [];
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in snapshots.EnumerateObject().OrderBy(p => p.Name switch
        { "premium_interactions" => 0, "chat" => 1, "completions" => 2, _ => 3 }))
        {
            if (!keys.Add(property.Name) || keys.Count > 128) throw new ProviderException(ProviderFailureKind.InvalidResponse);
            var value = property.Value;
            if (value.ValueKind == JsonValueKind.Null) continue;
            if (value.ValueKind != JsonValueKind.Object) throw new ProviderException(ProviderFailureKind.InvalidResponse);
            var unlimited = Boolean(value, "unlimited");
            var entitlement = Number(value, "entitlement");
            var remaining = Number(value, "remaining");
            var percent = Number(value, "percent_remaining");
            if (percent is < 0 or > 100) percent = null;
            if (entitlement < 0) entitlement = null;
            if (remaining < 0) remaining = null;
            // Preserve OMP's source-observed request groups only; unknown future keys have unknown units.
            var unit = property.Name is "premium_interactions" or "chat" or "completions" ? "requests" : "unknown";
            var used = entitlement is not null && remaining is not null && remaining <= entitlement ? entitlement - remaining : null;
            var window = new QuotaWindow("monthly", unlimited == true || percent is null ? null : 100 - (double)percent.Value,
                unlimited == true ? null : (double?)percent, null, reset)
            {
                Unlimited = unlimited,
                Amount = new(remaining, used, entitlement, unit),
                SourceDetails = new(CopilotAuthClient.Text(value, "quota_id"), Number(value, "quota_remaining"),
                    Number(value, "overage_count"), Boolean(value, "overage_permitted"))
            };
            groups.Add(new(property.Name, property.Name switch
            {
                "premium_interactions" => "Premium requests", "chat" => "Chat", "completions" => "Completions", _ => property.Name
            }, null, null, null, null, [window]));
        }
        return new(fetchedAt, CopilotAuthClient.Text(root, "copilot_plan"), groups, null, null, null, null);
    }
    private static decimal? Number(JsonElement root, string key)
    {
        if (!root.TryGetProperty(key, out var value) || value.ValueKind == JsonValueKind.Null) return null;
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out var number)) throw new ProviderException(ProviderFailureKind.InvalidResponse);
        return number;
    }
    private static bool? Boolean(JsonElement root, string key)
    {
        if (!root.TryGetProperty(key, out var value) || value.ValueKind == JsonValueKind.Null) return null;
        return value.ValueKind switch { JsonValueKind.True => true, JsonValueKind.False => false, _ => throw new ProviderException(ProviderFailureKind.InvalidResponse) };
    }
}
