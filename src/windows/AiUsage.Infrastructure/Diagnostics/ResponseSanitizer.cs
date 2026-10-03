using System.Text.Json;

namespace AiUsage.Infrastructure.Diagnostics;

internal sealed record Redaction(string Path, string OriginalType, string Reason);
internal sealed record SanitizedResponse(JsonElement Body, Redaction[] Redactions, bool DuplicateProperties);

internal static class ResponseSanitizer
{
    internal static SanitizedResponse Sanitize(JsonElement root, EndpointPolicy policy)
    {
        using var bytes = new MemoryStream();
        using var writer = new Utf8JsonWriter(bytes);
        var redactions = new List<Redaction>();
        var nodes = 0;
        var duplicates = false;
        Write(root, "", "", policy.Kind != "unknown", 0);
        writer.Flush();
        using var document = JsonDocument.Parse(bytes.ToArray());
        return new(document.RootElement.Clone(), redactions.ToArray(), duplicates);

        void Write(JsonElement value, string path, string key, bool approved, int depth)
        {
            if (++nodes > 10000 || depth > 32) throw new InvalidDataException("Sanitizer limit reached.");
            switch (value.ValueKind)
            {
                case JsonValueKind.Object:
                    writer.WriteStartObject();
                    var seen = new HashSet<string>(StringComparer.Ordinal);
                    var index = 0;
                    foreach (var property in value.EnumerateObject())
                    {
                        var known = EndpointPolicy.KnownKey(property.Name);
                        var name = known ? property.Name : $"withheld_{index}";
                        var child = path + "/" + name;
                        if (!seen.Add(property.Name)) duplicates = true;
                        if (!known) redactions.Add(new(child, property.Value.ValueKind.ToString(), "unclassified-name"));
                        writer.WritePropertyName(name);
                        Write(property.Value, child, name, approved && known && EndpointPolicy.CanDescend(name), depth + 1);
                        index++;
                    }
                    writer.WriteEndObject();
                    break;
                case JsonValueKind.Array:
                    writer.WriteStartArray();
                    var elementIndex = 0;
                    foreach (var item in value.EnumerateArray()) Write(item, path + "/" + elementIndex++, key, approved, depth + 1);
                    writer.WriteEndArray();
                    break;
                case JsonValueKind.Null: writer.WriteNullValue(); break;
                default:
                    var safe = approved && (value.ValueKind == JsonValueKind.Number && policy.Numeric(key) ||
                        value.ValueKind is JsonValueKind.True or JsonValueKind.False && policy.Boolean(key) ||
                        value.ValueKind == JsonValueKind.String && policy.StringValue(key, value.GetString()!));
                    if (safe) value.WriteTo(writer); // preserves numeric tokens, including precision beyond decimal
                    else { writer.WriteNullValue(); redactions.Add(new(path, value.ValueKind.ToString(), "unclassified-or-sensitive-value")); }
                    break;
            }
        }
    }
}
