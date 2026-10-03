using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using AiUsage.Core.Usage;
using AiUsage.Core.Providers.Claude;

namespace AiUsage.Infrastructure.Providers;

internal static class ProviderStateMigration
{
    internal static QuotaSnapshot? UpgradeQuota(string provider, QuotaSnapshot? quota, ClaudeExtraUsage? extra = null)
    {
        if (quota is null) return null;
        var next = quota with { Limits = QuotaLimitMapping.FromLegacy(provider, quota, extra) };
        return Valid(next) ? next : null;
    }

    internal static ClaudeQuotaReading? UpgradeClaude(ClaudeQuotaReading? reading) =>
        reading is not null && UpgradeQuota("claude", reading.Quota, reading.ExtraUsage) is { } quota
            ? reading with { Quota = quota } : null;

    // Only an old cache can be discarded. Re-read all grant fields with the same strict
    // serializer and validator so corrupt identity/version/unknown state members still fail.
    internal static T Decode<T>(ReadOnlyMemory<byte> bytes, JsonTypeInfo<T> type, Action<T> validate) where T : class
    {
        using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 32 });
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("Version", out var version) ||
            !version.TryGetInt32(out var number) || number is not (1 or 2) ||
            root.EnumerateObject().Select(p => p.Name).Distinct(StringComparer.Ordinal).Count() != root.EnumerateObject().Count())
            throw new ProviderException(ProviderFailureKind.RecoveryRequired);
        try { return Read(root); }
        catch (Exception e) when (number == 1 && e is JsonException or ProviderException or NotSupportedException or InvalidOperationException or KeyNotFoundException or FormatException)
        {
            using var buffer = new MemoryStream();
            using (var writer = new Utf8JsonWriter(buffer))
            {
                writer.WriteStartObject();
                foreach (var property in root.EnumerateObject())
                {
                    if (property.Name == "CachedQuota") writer.WriteNull("CachedQuota");
                    else property.WriteTo(writer);
                }
                writer.WriteEndObject();
            }
            try
            {
                using var clean = JsonDocument.Parse(buffer.GetBuffer().AsMemory(0, (int)buffer.Length));
                return Read(clean.RootElement);
            }
            finally { CryptographicOperations.ZeroMemory(buffer.GetBuffer()); }
        }

        T Read(JsonElement element)
        {
            var state = element.Deserialize(type) ?? throw new JsonException();
            validate(state);
            return state;
        }
    }

    internal static bool Valid(QuotaSnapshot quota)
    {
        if (quota.Groups is null || quota.Groups.Count > 1024 || quota.Groups.Any(g => g is null || g.Id is null ||
            g.Windows is null || g.Windows.Any(w => w is null || w.Id is null ||
                !Percent(w.UsedPercent) || !Percent(w.RemainingPercent) || w.Amount is { Unit: null }))) return false;
        if (quota.Limits is not { } snapshot) return true;
        return snapshot.Limits is not null && snapshot.Limits.Count <= 2048 && Enum.IsDefined(snapshot.Source) &&
            snapshot.Limits.All(f => f is not null && ValidKey(f.Key) && f.Unit is not null &&
                f.Limit is not null && Enum.IsDefined(f.Kind) && Enum.IsDefined(f.Limit.State) &&
                (f.Limit.State == LimitValueState.Finite) == (f.Limit.Value is not null) &&
                ValidQuantity(f.Limit.Value) && ValidQuantity(f.Used) && ValidQuantity(f.Remaining) &&
                ValidQuantity(f.ReportedLimit) && ValidQuantity(f.SecondaryAmount) &&
                f.UsedPercent is not (< 0 or > 100) && f.RemainingPercent is not (< 0 or > 100) &&
                (f.LegacyKey is null || ValidKey(f.LegacyKey)) &&
                (f.Reset is null || Enum.IsDefined(f.Reset.Origin) && Enum.IsDefined(f.Reset.Meaning) &&
                    Enum.IsDefined(f.Reset.Precision) && Enum.IsDefined(f.Reset.Zone))) &&
            snapshot.Limits.Select(f => f.Key).Distinct().Count() == snapshot.Limits.Count;
    }

    private static bool ValidKey(LimitKey? key) => key is not null && key.Provider is not null && key.Family is not null && key.NativeDiscriminator is not null;
    private static bool ValidQuantity(Quantity? value) => value is null or CountQuantity { Unit: not null } or MoneyQuantity;
    private static bool Percent(double? value) => value is null || double.IsFinite(value.Value) && value is >= 0 and <= 100;
}
