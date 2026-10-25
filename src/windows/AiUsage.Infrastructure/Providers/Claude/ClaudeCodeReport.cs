using System.Text.Json;
using AiUsage.Core.Providers.Claude;
using AiUsage.Core.Usage;

namespace AiUsage.Infrastructure.Providers.Claude;

/// <summary>
/// Reads the stream-json output of `claude -p /usage`. Only a local `/usage` command result with
/// no model turn and no cost is accepted; its structured `usage_report.rate_limits` object has
/// the `limits` and `extra_usage` members of the OAuth usage payload and goes to the same parser.
/// </summary>
internal static class ClaudeCodeReport
{
    private const int MaxJsonDepth = 32;

    /// <returns>The failure kind, or null when <paramref name="reading"/> is set.</returns>
    public static ProviderFailureKind? Read(ReadOnlySpan<byte> stdout, DateTimeOffset fetchedAt, out ClaudeQuotaReading? reading)
    {
        reading = null;
        var options = new JsonDocumentOptions { MaxDepth = MaxJsonDepth };
        var resultSeen = false;
        JsonElement? rateLimits = null;
        var reported = false;
        try
        {
            using var lines = new LineDocuments();
            foreach (var line in Lines(stdout.ToArray()))
            {
                var document = lines.Add(JsonDocument.Parse(line, options));
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String)
                    return ProviderFailureKind.InvalidResponse;
                switch (type.GetString())
                {
                    case "assistant" when LocalUsage(root):
                        if (root.TryGetProperty("usage_report", out var report) && report.ValueKind == JsonValueKind.Object)
                        {
                            reported = true;
                            rateLimits = report.TryGetProperty("rate_limits", out var limits) ? limits : null;
                        }
                        break;
                    case "assistant":
                        // Any other assistant message is a model reply: the prompt did not stay local.
                        return ProviderFailureKind.InvalidResponse;
                    case "result":
                        if (resultSeen || !LocalResult(root))
                            return ProviderFailureKind.InvalidResponse;
                        resultSeen = true;
                        break;
                }
            }
            if (!resultSeen)
                return ProviderFailureKind.InvalidResponse;
            if (!reported)
                return ProviderFailureKind.AuthenticationRequired;
            if (rateLimits is not { ValueKind: JsonValueKind.Object } payload)
                return ProviderFailureKind.RateLimited;
            return ClaudeQuotaParser.TryParse(payload, fetchedAt, out reading) ? null : ProviderFailureKind.InvalidResponse;
        }
        catch (JsonException)
        {
            // Process output is untrusted; parser text may echo it and is never surfaced.
            reading = null;
            return ProviderFailureKind.InvalidResponse;
        }
    }

    private static bool LocalUsage(JsonElement assistant) =>
        assistant.TryGetProperty("local_command_run", out var run) && run.ValueKind == JsonValueKind.Object &&
        run.TryGetProperty("command", out var command) && command.ValueKind == JsonValueKind.String && command.GetString() == "usage";

    private static bool LocalResult(JsonElement result) =>
        result.TryGetProperty("subtype", out var subtype) && subtype.ValueKind == JsonValueKind.String && subtype.GetString() == "success" &&
        result.TryGetProperty("local_command", out var command) && command.ValueKind == JsonValueKind.String && command.GetString() == "usage" &&
        result.TryGetProperty("num_turns", out var turns) && turns.ValueKind == JsonValueKind.Number && turns.TryGetInt32(out var count) && count == 0 &&
        result.TryGetProperty("total_cost_usd", out var cost) && cost.ValueKind == JsonValueKind.Number && cost.GetDouble() == 0 &&
        result.TryGetProperty("modelUsage", out var models) && models.ValueKind == JsonValueKind.Object && !models.EnumerateObject().Any();

    private static IEnumerable<ReadOnlyMemory<byte>> Lines(byte[] output)
    {
        var start = 0;
        for (var i = 0; i <= output.Length; i++)
        {
            if (i < output.Length && output[i] != (byte)'\n')
                continue;
            var length = i - start;
            if (length > 0 && output[start + length - 1] == (byte)'\r')
                length--;
            if (length > 0)
                yield return output.AsMemory(start, length);
            start = i + 1;
        }
    }

    /// <summary>Keeps every parsed line alive until the report has been read.</summary>
    private sealed class LineDocuments : IDisposable
    {
        private readonly List<JsonDocument> documents = [];
        public JsonDocument Add(JsonDocument document)
        {
            documents.Add(document);
            return document;
        }
        public void Dispose() => documents.ForEach(d => d.Dispose());
    }
}
