using System.Text.Json;
using System.Text.Json.Serialization;
using AiUsage.Core.Accounts;
using AiUsage.Core.Budget;
using AiUsage.Core.Usage;
using AiUsage.Features.Ledger.Contract;
using FactLimit = AiUsage.Core.Usage.LimitValue;

namespace AiUsage.Adapters.Live.Audit;

// Test replay data, exported by tests using the production parsers. No credential fields.
internal sealed record AuditInput(DateTimeOffset Now, string ZoneId, AccountSnapshot[] Accounts,
    Dictionary<string, string> Labels, ReadingObservation[] Observations, BudgetConfiguration Configuration)
{
    public string SyntheticMarker { get; init; } = "AI Usage synthetic audit v1";
    public Dictionary<string, CardState> ExpectedStates { get; init; } = [];
    // Explicit presentation-contract evidence, never inferred from provider subscription names or payloads.
    public Dictionary<string, MonetaryScope> ScopeAnnotations { get; init; } = [];
    public AccountSnapshot[]? NextAccounts { get; init; }
    public DateTimeOffset? NextNow { get; init; }
    public SignInFailure? SignInFailure { get; init; }
    public bool ManualCode { get; init; }
    public bool BlockRefresh { get; init; }
    public bool BlockInitialization { get; init; }
    public bool UseProductMaintenance { get; init; }
    public int RecoveryFailures { get; init; }
    public LedgerRecoveryModel? Recovery { get; init; }
}

internal sealed record AuditProcessReceipt(string SyntheticMarker, int ProcessId, DateTimeOffset ProcessStartedAt,
    string FixtureSha256, DateTimeOffset ControlledNow, string ZoneId);

internal sealed class AuditQuantityConverter : JsonConverter<Quantity>
{
    public override Quantity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        return root.GetProperty("kind").GetString() switch
        {
            "count" => new CountQuantity(root.GetProperty("value").GetDecimal(), root.GetProperty("unit").GetString()!),
            "money" => new MoneyQuantity(root.GetProperty("minor").GetInt64(),
                root.GetProperty("exponent").ValueKind == JsonValueKind.Null ? null : root.GetProperty("exponent").GetInt32(),
                root.GetProperty("currency").GetString()),
            _ => throw new JsonException("Unsupported synthetic quantity")
        };
    }
    public override void Write(Utf8JsonWriter writer, Quantity value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        if (value is CountQuantity count)
        {
            writer.WriteString("kind", "count"); writer.WriteNumber("value", count.Value); writer.WriteString("unit", count.Unit);
        }
        else if (value is MoneyQuantity money)
        {
            writer.WriteString("kind", "money"); writer.WriteNumber("minor", money.MinorUnits);
            if (money.Exponent is { } exponent) writer.WriteNumber("exponent", exponent); else writer.WriteNull("exponent");
            writer.WriteString("currency", money.Currency);
        }
        else throw new JsonException("Unsupported synthetic quantity");
        writer.WriteEndObject();
    }
}

internal sealed class AuditLimitConverter : JsonConverter<FactLimit>
{
    public override FactLimit Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        return (LimitValueState)root.GetProperty("state").GetInt32() switch
        {
            LimitValueState.Finite => FactLimit.Finite(JsonSerializer.Deserialize(root.GetProperty("value"), AuditJson.Default.Quantity)!),
            LimitValueState.ExplicitNull => FactLimit.ExplicitNull,
            LimitValueState.Unlimited => FactLimit.Unlimited,
            LimitValueState.NotApplicable => FactLimit.NotApplicable,
            LimitValueState.Unknown => FactLimit.Unknown,
            _ => throw new JsonException("Unsupported synthetic limit")
        };
    }
    public override void Write(Utf8JsonWriter writer, FactLimit value, JsonSerializerOptions options)
    {
        writer.WriteStartObject(); writer.WriteNumber("state", (int)value.State);
        writer.WritePropertyName("value"); JsonSerializer.Serialize(writer, value.Value, AuditJson.Default.Quantity);
        writer.WriteEndObject();
    }
}

[JsonSourceGenerationOptions(WriteIndented = true, Converters = new[] { typeof(AuditQuantityConverter), typeof(AuditLimitConverter) })]
[JsonSerializable(typeof(AuditInput))]
[JsonSerializable(typeof(AuditProcessReceipt))]
[JsonSerializable(typeof(Quantity))]
internal sealed partial class AuditJson : JsonSerializerContext;
