using System.Globalization;
using System.Text.Json;
using AiUsage.Core.History;
using AiUsage.Core.Usage;

namespace AiUsage.Infrastructure.Providers;

internal static class HistoryJson
{
    internal static ProviderException Invalid() => new(ProviderFailureKind.InvalidResponse);
    internal static JsonElement Property(JsonElement root, string name) =>
        root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var value) ? value : default;
    internal static string? Text(JsonElement root, string name) => Property(root, name) is { ValueKind: JsonValueKind.String } value ? value.GetString() : null;
    internal static JsonElement Array(JsonElement root, string name)
    {
        var value = Property(root, name);
        return value.ValueKind == JsonValueKind.Array ? value : throw Invalid();
    }
    internal static decimal? Number(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Null) return null;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number)) return number;
        if (value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number)) return number;
        throw Invalid();
    }
    internal static DateOnly Date(JsonElement row)
    {
        var text = Text(row, "date");
        return DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : throw Invalid();
    }
    internal static HistoryStatus Status(ProviderFailureKind failure) => failure switch
    {
        ProviderFailureKind.AccessDenied => HistoryStatus.AccessDenied,
        ProviderFailureKind.AuthenticationRequired => HistoryStatus.AuthenticationRequired,
        ProviderFailureKind.RateLimited => HistoryStatus.RateLimited,
        ProviderFailureKind.AccountMismatch => HistoryStatus.AccountChanged,
        _ => HistoryStatus.Failed
    };
    internal static HistoryReport Report(string id, IReadOnlyList<HistoryValue> values) =>
        new(id, values.Count == 0 ? HistoryStatus.Empty : HistoryStatus.Available, values);
}
