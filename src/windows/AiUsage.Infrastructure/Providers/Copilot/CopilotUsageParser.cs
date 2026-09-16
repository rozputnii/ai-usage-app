using AiUsage.Core.Providers.Copilot;
using System.Text.Json;

namespace AiUsage.Infrastructure.Providers.Copilot;

/// <summary>Parses GitHub's documented user billing usage report. Payload values are never echoed.</summary>
public static class CopilotUsageParser
{
    private const int MaximumItems = 1000;

    public static bool TryParse(ReadOnlyMemory<byte> payload, CopilotUsageReportKind kind, DateTimeOffset observedAt,
        out CopilotUsageReport? report, out string? user)
    {
        report = null;
        user = null;
        try
        {
            using var document = JsonDocument.Parse(payload, new JsonDocumentOptions { MaxDepth = 32 });
            return TryParse(document.RootElement, kind, observedAt, out report, out user);
        }
        catch (JsonException) { return false; }
    }

    internal static bool TryParse(JsonElement root, CopilotUsageReportKind kind, DateTimeOffset observedAt,
        out CopilotUsageReport? report, out string? user)
    {
        report = null;
        user = null;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("timePeriod", out var period) || period.ValueKind != JsonValueKind.Object ||
            !TryInt(period, "year", required: true, out var year) || year is < 2000 or > 9999 ||
            !TryInt(period, "month", required: false, out var month) || month is < 1 or > 12 ||
            !TryInt(period, "day", required: false, out var day) || day is < 1 or > 31 ||
            !TryString(root, "user", required: true, out user) ||
            !root.TryGetProperty("usageItems", out var itemsElement) || itemsElement.ValueKind != JsonValueKind.Array ||
            itemsElement.GetArrayLength() > MaximumItems)
            return false;
        var items = new List<CopilotUsageItem>(itemsElement.GetArrayLength());
        foreach (var item in itemsElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object ||
                !TryString(item, "product", required: true, out var product) ||
                !TryString(item, "sku", required: true, out var sku) ||
                !TryString(item, "model", required: false, out var model) ||
                !TryString(item, "unitType", required: true, out var unitType) ||
                !TryDecimal(item, "pricePerUnit", out var price) ||
                !TryDecimal(item, "grossQuantity", out var grossQuantity) ||
                !TryDecimal(item, "grossAmount", out var grossAmount) ||
                !TryDecimal(item, "discountQuantity", out var discountQuantity) ||
                !TryDecimal(item, "discountAmount", out var discountAmount) ||
                !TryDecimal(item, "netQuantity", out var netQuantity) ||
                !TryDecimal(item, "netAmount", out var netAmount))
            {
                user = null;
                return false;
            }
            items.Add(new(product!, sku!, model, unitType!, price, grossQuantity, grossAmount, discountQuantity, discountAmount, netQuantity, netAmount));
        }
        report = new(kind, year!.Value, month, day, items, observedAt);
        return true;
    }

    private static bool TryInt(JsonElement parent, string name, bool required, out int? value)
    {
        value = null;
        if (!parent.TryGetProperty(name, out var element) || element.ValueKind == JsonValueKind.Null) return !required;
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out var number)) return false;
        value = number;
        return true;
    }

    private static bool TryString(JsonElement parent, string name, bool required, out string? value)
    {
        value = null;
        if (!parent.TryGetProperty(name, out var element) || element.ValueKind == JsonValueKind.Null) return !required;
        if (element.ValueKind != JsonValueKind.String) return false;
        var text = element.GetString()!;
        if (text.Length > 256) return false;
        if (text.Length == 0) return !required;
        value = text;
        return true;
    }

    /// <summary>Quantities may be fractional credits; negative or non-numeric values are a schema failure, not zero.</summary>
    private static bool TryDecimal(JsonElement parent, string name, out decimal value)
    {
        value = 0;
        return parent.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.Number &&
            element.TryGetDecimal(out value) && value >= 0;
    }
}
