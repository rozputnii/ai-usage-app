using System.Text.Json;
using AiUsage.Core.History;

namespace AiUsage.Infrastructure.Providers.Codex;

internal static class CodexHistoryParser
{
    private static readonly string[] Metrics = ["credits", "on_demand_credits", "credit_amount", "cost_usd", "users", "threads", "turns",
        "uncached_text_input_tokens", "cached_text_input_tokens", "text_output_tokens", "text_total_tokens", "total_tokens", "invocation_counts"];
    private static readonly string[] Dimensions = ["model", "speed", "client_id", "product_surface", "usage_id", "plugin_id", "plugin_name", "display_name", "marketplace", "skill_name"];

    internal static IReadOnlyList<HistoryValue> Parse(string report, JsonElement root, HistoryRange range)
    {
        var rows = new List<HistoryValue>();
        var unit = HistoryJson.Text(root, "units") ?? HistoryJson.Text(root, "unit") ?? HistoryJson.Text(root, "balance_unit");
        foreach (var bucket in HistoryJson.Array(root, "data").EnumerateArray())
        {
            var date = HistoryJson.Date(bucket);
            if (date < range.From || date > range.To) continue;
            var required = report switch
            {
                "usage" or "workspace" or "workspace-models" => "product_surface_usage_values",
                "activity" => "totals", "plugins" => "plugin_usage_overviews", "skills" => "skill_usage_overviews",
                "credits" => "credit_amount", _ => "values"
            };
            if (HistoryJson.Property(bucket, required).ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) throw HistoryJson.Invalid();
            var before = rows.Count;
            ReadValues(bucket, date, new Dictionary<string, string>(), rows);
            Map(bucket, "product_surface_usage_values", "usage", unit, "surface", date, rows);
            Map(bucket, "values", "credits", unit, "series", date, rows);
            var premium = HistoryJson.Property(bucket, "premium_usage_values");
            if (premium.ValueKind == JsonValueKind.Object)
                foreach (var metric in premium.EnumerateObject())
                    Map(premium, metric.Name, metric.Name, metric.Name.Contains("tokens", StringComparison.Ordinal) ? "tokens" : "credits", "surface", date, rows);
            var totals = HistoryJson.Property(bucket, "totals");
            if (totals.ValueKind == JsonValueKind.Object) ReadValues(totals, date, new() { ["breakdown"] = "total" }, rows);
            foreach (var group in new[] { "models", "clients", "groups", "plugin_usage_overviews", "skill_usage_overviews" })
            {
                var array = HistoryJson.Property(bucket, group);
                if (array.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) continue;
                if (array.ValueKind != JsonValueKind.Array) throw HistoryJson.Invalid();
                foreach (var value in array.EnumerateArray()) ReadValues(value, date, new() { ["breakdown"] = group }, rows);
            }
            var attribution = HistoryJson.Property(bucket, "attribution");
            if (attribution.ValueKind == JsonValueKind.Array)
                foreach (var value in attribution.EnumerateArray())
                {
                    var dimensions = new Dictionary<string, string>();
                    foreach (var name in new[] { "thread_source", "turn_trigger", "model", "surface" })
                        if (HistoryJson.Text(value, name) is { } text) dimensions[name] = text;
                    rows.Add(new(date, date, "usage", HistoryJson.Number(HistoryJson.Property(value, "value")), unit, dimensions));
                }
            // A nonempty unknown schema must not masquerade as zero usage.
            if (rows.Count == before && report == "credits") throw HistoryJson.Invalid();
        }
        return rows.AsReadOnly();
    }

    private static void ReadValues(JsonElement value, DateOnly date, Dictionary<string, string> dimensions, List<HistoryValue> rows)
    {
        if (value.ValueKind != JsonValueKind.Object) throw HistoryJson.Invalid();
        foreach (var name in Dimensions)
            if (HistoryJson.Text(value, name) is { } text) dimensions[name] = text;
        var extra = HistoryJson.Property(value, "dimensions");
        if (extra.ValueKind == JsonValueKind.Object)
            foreach (var property in extra.EnumerateObject())
                if (property.Value.ValueKind == JsonValueKind.String) dimensions[property.Name] = property.Value.GetString()!;
        if (HistoryJson.Property(value, "is_other").ValueKind == JsonValueKind.True) dimensions["is_other"] = "true";
        var skillIds = HistoryJson.Property(value, "skill_ids");
        if (skillIds.ValueKind == JsonValueKind.Array)
            for (var i = 0; i < skillIds.GetArrayLength(); i++)
                if (skillIds[i].ValueKind == JsonValueKind.String) dimensions["skill_id[" + i + "]"] = skillIds[i].GetString()!;
        foreach (var metric in Metrics)
        {
            var number = HistoryJson.Property(value, metric);
            if (number.ValueKind == JsonValueKind.Undefined) continue;
            var unit = metric.Contains("tokens", StringComparison.Ordinal) ? "tokens" : metric == "cost_usd" ? "USD" :
                metric.Contains("credit", StringComparison.Ordinal) ? "credits" : metric;
            rows.Add(new(date, date, metric, HistoryJson.Number(number), unit, dimensions));
        }
    }

    private static void Map(JsonElement value, string key, string metric, string? unit, string dimension, DateOnly date, List<HistoryValue> rows)
    {
        var map = HistoryJson.Property(value, key);
        if (map.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) return;
        if (map.ValueKind != JsonValueKind.Object) throw HistoryJson.Invalid();
        foreach (var entry in map.EnumerateObject())
            rows.Add(new(date, date, metric, HistoryJson.Number(entry.Value), unit, new Dictionary<string, string> { [dimension] = entry.Name }));
    }
}
