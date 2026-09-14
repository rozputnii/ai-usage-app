using System.Globalization;
using System.Text.Json;
using AiUsage.Core.Usage;

namespace AiUsage.Infrastructure.Providers.Codex;

public static class CodexQuotaParser
{
    public static QuotaSnapshot Parse(ReadOnlyMemory<byte> utf8, DateTimeOffset fetchedAt)
    {
        if (utf8.Length > 1024 * 1024)
            throw new CodexException(CodexFailureKind.InvalidResponse);
        try
        {
            using var document = JsonDocument.Parse(utf8, new JsonDocumentOptions { MaxDepth = 32 });
            return Parse(document.RootElement, fetchedAt);
        }
        catch (JsonException) { throw new CodexException(CodexFailureKind.InvalidResponse); }
    }

    internal static QuotaSnapshot Parse(JsonElement root, DateTimeOffset fetchedAt)
    {
        if (root.ValueKind != JsonValueKind.Object ||
            !(root.TryGetProperty("plan_type", out _) || root.TryGetProperty("rate_limit", out _) ||
              root.TryGetProperty("credits", out _) || root.TryGetProperty("additional_rate_limits", out _) ||
              root.TryGetProperty("spend_control", out _) || root.TryGetProperty("rate_limit_reset_credits", out _)))
            throw new CodexException(CodexFailureKind.InvalidResponse);
        var groups = new List<QuotaGroup>();
        var ids = new HashSet<string>(StringComparer.Ordinal) { "codex" };
        var main = Property(root, "rate_limit");
        if (main.ValueKind is not (JsonValueKind.Object or JsonValueKind.Null or JsonValueKind.Undefined))
            throw new CodexException(CodexFailureKind.InvalidResponse);
        if (main.ValueKind == JsonValueKind.Object)
            groups.Add(ParseGroup(main, "codex", "Codex", null, null, fetchedAt));
        var additional = Property(root, "additional_rate_limits");
        if (additional.ValueKind is not (JsonValueKind.Array or JsonValueKind.Null or JsonValueKind.Undefined))
            throw new CodexException(CodexFailureKind.InvalidResponse);
        if (additional.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in additional.EnumerateArray())
            {
                index++;
                if (item.ValueKind != JsonValueKind.Object)
                    throw new CodexException(CodexFailureKind.InvalidResponse);
                var name = Text(Property(item, "limit_name"));
                var feature = Text(Property(item, "metered_feature"));
                var idBase = "codex:" + (feature ?? name ?? "additional-" + index.ToString(CultureInfo.InvariantCulture));
                var id = idBase;
                for (var suffix = 2; !ids.Add(id); suffix++)
                    id = idBase + ":" + suffix.ToString(CultureInfo.InvariantCulture);
                groups.Add(ParseGroup(Property(item, "rate_limit"), id, name, feature,
                    Text(Property(item, "normal_model_slug")), fetchedAt));
            }
        }
        var credits = Property(root, "credits");
        var balance = Decimal(Property(credits, "balance"));
        var creditBalance = credits.ValueKind == JsonValueKind.Object
            ? new CreditBalance(Boolean(Property(credits, "has_credits")), Boolean(Property(credits, "unlimited")), balance >= 0 ? balance : null)
            : null;
        var resets = Number(Property(Property(root, "rate_limit_reset_credits"), "available_count"));
        int? resetCount = resets is >= 0 and <= int.MaxValue && resets == Math.Truncate(resets.Value) ? (int)resets.Value : null;
        return new QuotaSnapshot(fetchedAt, Text(Property(root, "plan_type")), groups.AsReadOnly(), creditBalance,
            resetCount, Boolean(Property(Property(root, "spend_control"), "reached")),
            Text(Property(Property(root, "rate_limit_reached_type"), "type")));
    }

    private static QuotaGroup ParseGroup(JsonElement limit, string id, string? name, string? feature, string? normalModel, DateTimeOffset fetchedAt)
    {
        if (limit.ValueKind is not (JsonValueKind.Object or JsonValueKind.Null or JsonValueKind.Undefined))
            throw new CodexException(CodexFailureKind.InvalidResponse);
        var windows = new List<QuotaWindow>(2);
        AddWindow("primary", Property(limit, "primary_window"));
        AddWindow("secondary", Property(limit, "secondary_window"));
        return new QuotaGroup(id, name, feature, normalModel, Boolean(Property(limit, "allowed")),
            Boolean(Property(limit, "limit_reached")), windows.AsReadOnly());

        void AddWindow(string windowId, JsonElement value)
        {
            if (value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
                return;
            if (value.ValueKind != JsonValueKind.Object)
                throw new CodexException(CodexFailureKind.InvalidResponse);
            var used = Number(Property(value, "used_percent"));
            if (used is < 0 or > 100)
                used = null;
            var durationSeconds = Number(Property(value, "limit_window_seconds"));
            TimeSpan? duration = durationSeconds is > 0 && durationSeconds <= TimeSpan.MaxValue.TotalSeconds
                ? SafeDuration(durationSeconds.Value) : null;
            DateTimeOffset? resetsAt = null;
            var absolute = Number(Property(value, "reset_at"));
            if (absolute is >= -62135596800 and <= 253402300799 && absolute == Math.Truncate(absolute.Value))
                resetsAt = DateTimeOffset.FromUnixTimeSeconds((long)absolute.Value);
            var relative = Number(Property(value, "reset_after_seconds"));
            if (resetsAt is null && relative is >= 0 && relative <= (DateTimeOffset.MaxValue - fetchedAt).TotalSeconds)
            {
                try { resetsAt = fetchedAt.AddSeconds(relative.Value); }
                catch (ArgumentOutOfRangeException) { }
            }
            windows.Add(new QuotaWindow(windowId, used, used is null ? null : 100 - used, duration, resetsAt));
        }
    }

    private static TimeSpan? SafeDuration(double seconds)
    {
        try { return TimeSpan.FromSeconds(seconds); }
        catch (OverflowException) { return null; }
    }

    internal static JsonElement Property(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) ? value : default;
    internal static string? Text(JsonElement element) => element.ValueKind == JsonValueKind.String ? element.GetString() : null;
    internal static bool? Boolean(JsonElement element) => element.ValueKind switch { JsonValueKind.True => true, JsonValueKind.False => false, _ => null };
    internal static double? Number(JsonElement element) => element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var value) && double.IsFinite(value) ? value : null;
    private static decimal? Decimal(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var number))
            return number;
        return element.ValueKind == JsonValueKind.String && decimal.TryParse(element.GetString(), NumberStyles.Float,
            CultureInfo.InvariantCulture, out number) ? number : null;
    }
}
