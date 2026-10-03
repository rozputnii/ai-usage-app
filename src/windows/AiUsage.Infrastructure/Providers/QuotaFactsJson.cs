using System.Text.Json;
using System.Text.Json.Serialization;
using AiUsage.Core.Usage;

namespace AiUsage.Infrastructure.Providers;

internal sealed class QuotaQuantityConverter : JsonConverter<Quantity>
{
    public override Quantity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object) throw new JsonException();
        var kind = root.GetProperty("Kind").GetString();
        var allowed = kind == "count" ? new[] { "Kind", "Value", "Unit" } : new[] { "Kind", "MinorUnits", "Exponent", "Currency" };
        if (root.EnumerateObject().Any(p => !allowed.Contains(p.Name, StringComparer.Ordinal))) throw new JsonException();
        if (kind == "count" && root.TryGetProperty("Value", out var count) && count.TryGetDecimal(out var value) &&
            root.TryGetProperty("Unit", out var unit) && unit.ValueKind == JsonValueKind.String)
            return new CountQuantity(value, unit.GetString()!);
        if (kind == "money" && root.TryGetProperty("MinorUnits", out var minor) && minor.TryGetInt64(out var amount))
        {
            var exponent = root.GetProperty("Exponent");
            var currency = root.GetProperty("Currency");
            return new MoneyQuantity(amount, exponent.ValueKind == JsonValueKind.Null ? null : exponent.GetInt32(),
                currency.ValueKind == JsonValueKind.Null ? null : currency.GetString());
        }
        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, Quantity value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        switch (value)
        {
            case CountQuantity count:
                writer.WriteString("Kind", "count"); writer.WriteNumber("Value", count.Value); writer.WriteString("Unit", count.Unit);
                break;
            case MoneyQuantity money:
                writer.WriteString("Kind", "money"); writer.WriteNumber("MinorUnits", money.MinorUnits);
                if (money.Exponent is { } exponent) writer.WriteNumber("Exponent", exponent); else writer.WriteNull("Exponent");
                writer.WriteString("Currency", money.Currency);
                break;
            default: throw new JsonException();
        }
        writer.WriteEndObject();
    }
}

internal sealed class QuotaLimitValueConverter : JsonConverter<LimitValue>
{
    public override LimitValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("State", out var state) ||
            !state.TryGetInt32(out var code) || root.EnumerateObject().Any(p => p.Name is not ("State" or "Value"))) throw new JsonException();
        var hasValue = root.TryGetProperty("Value", out var value) && value.ValueKind != JsonValueKind.Null;
        if (code != (int)LimitValueState.Finite && hasValue) throw new JsonException();
        return (LimitValueState)code switch
        {
            LimitValueState.Unknown => LimitValue.Unknown,
            LimitValueState.ExplicitNull => LimitValue.ExplicitNull,
            LimitValueState.Unlimited => LimitValue.Unlimited,
            LimitValueState.NotApplicable => LimitValue.NotApplicable,
            LimitValueState.Finite when hasValue => LimitValue.Finite(value.Deserialize(QuotaFactsJson.Default.Quantity) ?? throw new JsonException()),
            _ => throw new JsonException()
        };
    }
    public override void Write(Utf8JsonWriter writer, LimitValue value, JsonSerializerOptions options)
    {
        writer.WriteStartObject(); writer.WriteNumber("State", (int)value.State);
        if (value.Value is { } quantity)
        {
            writer.WritePropertyName("Value");
            JsonSerializer.Serialize(writer, quantity, QuotaFactsJson.Default.Quantity);
        }
        writer.WriteEndObject();
    }
}

[JsonSourceGenerationOptions(Converters = [typeof(QuotaQuantityConverter), typeof(QuotaLimitValueConverter)],
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, MaxDepth = 32)]
[JsonSerializable(typeof(Quantity))]
[JsonSerializable(typeof(LimitSnapshot))]
internal partial class QuotaFactsJson : JsonSerializerContext;
