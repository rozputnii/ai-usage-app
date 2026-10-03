using System.Diagnostics;
using System.Net;
using System.Text.Json;
using AiUsage.Core.Diagnostics;

namespace AiUsage.Infrastructure.Diagnostics;

internal sealed class ProviderCapture
{
    private readonly FileDiagnostics log;
    private readonly EndpointPolicy policy;
    private readonly Guid id = Guid.NewGuid();
    private readonly DateTimeOffset started = DateTimeOffset.UtcNow;
    private readonly long tick = Stopwatch.GetTimestamp();
    private readonly DiagnosticContext? operation = FileDiagnostics.Operation.Value;
    private readonly string method;
    private int completed;

    internal ProviderCapture(FileDiagnostics log, HttpRequestMessage request)
    {
        this.log = log;
        policy = EndpointPolicy.Classify(request.RequestUri);
        method = request.Method == HttpMethod.Get ? "GET" : request.Method == HttpMethod.Post ? "POST" : "other";
    }

    internal void Complete(string state, HttpResponseMessage? response, JsonDocument? body, long? observedBytes, TimeSpan? retryAfter)
    {
        if (Interlocked.Exchange(ref completed, 1) != 0) return;
        try
        {
            SanitizedResponse? sanitized = null;
            if (body is not null)
            {
                if (!log.CaptureBodies) state = "disabled";
                else
                {
                    try
                    {
                        sanitized = ResponseSanitizer.Sanitize(body.RootElement, policy);
                        state = sanitized.Redactions.Length > 0 ? "withheld-values" : "complete-sanitized";
                    }
                    catch (InvalidDataException) { state = "sanitizer-limit"; }
                }
            }
            var contentType = response?.Content.Headers.ContentType?.MediaType;
            var envelope = new
            {
                schemaVersion = 1, captureVersion = 1, sanitizerVersion = 1, captureId = id,
                timestamp = started, endedAt = DateTimeOffset.UtcNow, durationMs = Stopwatch.GetElapsedTime(tick).TotalMilliseconds,
                operationId = operation?.Id, parentOperationId = operation?.Parent,
                provider = policy.Provider, route = policy.Route, method, attemptId = id,
                status = response is null ? (int?)null : (int)response.StatusCode,
                declaredBytes = response?.Content.Headers.ContentLength, observedBytes,
                contentType = contentType is "application/json" or "text/json" ? contentType : "other-or-absent",
                retryAfterSeconds = retryAfter?.TotalSeconds, policyId = policy.Id, completeness = state,
                body = sanitized?.Body, redactions = sanitized?.Redactions ?? [], duplicateProperties = sanitized?.DuplicateProperties ?? false
            };
            log.Event(DiagnosticEvent.HttpCompleted, response is not null && !response.IsSuccessStatusCode ? DiagnosticSeverity.Warning : DiagnosticSeverity.Information,
                outcome: state, duration: Stopwatch.GetElapsedTime(tick).TotalMilliseconds,
                context: new { captureId = id, provider = policy.Provider, route = policy.Route, method, status = envelope.status, persisted = false });
            if (!log.Capture(JsonSerializer.SerializeToUtf8Bytes(envelope, FileDiagnostics.JsonOptions), started, id))
                log.Event(DiagnosticEvent.CaptureLost, DiagnosticSeverity.Warning, context: new { captureId = id, reason = "queue-or-size-limit" });
        }
        catch (Exception) { log.Record(DiagnosticEvent.CaptureLost, DiagnosticCategory.Unexpected, DiagnosticSeverity.Warning); }
    }
}
