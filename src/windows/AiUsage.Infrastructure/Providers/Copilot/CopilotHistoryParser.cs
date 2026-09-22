using System.Text.Json;
using AiUsage.Core.History;

namespace AiUsage.Infrastructure.Providers.Copilot;

internal static class CopilotHistoryParser
{
    internal static IReadOnlyList<HistoryValue> Parse(JsonElement root, HistoryRange period)
    {
        var time = HistoryJson.Property(root, "timePeriod");
        if (HistoryJson.Number(HistoryJson.Property(time, "year")) != period.From.Year ||
            HistoryJson.Number(HistoryJson.Property(time, "month")) != period.From.Month) throw HistoryJson.Invalid();
        var day = HistoryJson.Property(time, "day");
        if (period.From == period.To && (day.ValueKind == JsonValueKind.Undefined || HistoryJson.Number(day) != period.From.Day)) throw HistoryJson.Invalid();
        var rows = new List<HistoryValue>();
        foreach (var item in HistoryJson.Array(root, "usageItems").EnumerateArray())
        {
            var dimensions = new Dictionary<string, string>();
            foreach (var key in new[] { "product", "sku", "model" })
                if (HistoryJson.Text(item, key) is { } value) dimensions[key] = value;
            foreach (var metric in new[] { "grossQuantity", "discountQuantity", "netQuantity", "pricePerUnit", "grossAmount", "discountAmount", "netAmount" })
            {
                var value = HistoryJson.Property(item, metric);
                if (value.ValueKind == JsonValueKind.Undefined) continue;
                var unit = metric.EndsWith("Quantity", StringComparison.Ordinal) ? HistoryJson.Text(item, "unitType") : metric == "pricePerUnit" ? "USD/unit" : "USD";
                rows.Add(new(period.From, period.To, metric, HistoryJson.Number(value), unit, dimensions));
            }
        }
        return rows.AsReadOnly();
    }
}
