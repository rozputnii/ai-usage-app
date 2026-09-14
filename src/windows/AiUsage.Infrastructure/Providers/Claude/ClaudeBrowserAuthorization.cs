using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Web;

namespace AiUsage.Infrastructure.Providers.Claude;

public sealed class ClaudeBrowserAuthorization : IDisposable
{
    internal ClaudeBrowserAuthorization(LoopbackCallback callback, Uri url, string redirect, string state, string verifier, DateTimeOffset expires)
    {
        Callback = callback;
        AuthorizationUrl = url;
        RedirectUri = redirect;
        State = state;
        CodeVerifier = verifier;
        ExpiresAt = expires;
    }

    internal LoopbackCallback Callback { get; }
    internal string RedirectUri { get; }
    internal string State { get; }
    internal string CodeVerifier { get; }
    internal SemaphoreSlim Gate { get; } = new(1, 1);
    internal TaskCompletionSource<string> ManualCode { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal bool Completed { get; set; }
    [JsonIgnore] public Uri AuthorizationUrl { get; }
    [JsonIgnore] public DateTimeOffset ExpiresAt { get; }

    /// <summary>
    /// Deliberate fallback input. A supplied state must match; OMP's bare-code fallback has no
    /// returned state and relies on this user action and the attempt's PKCE verifier instead.
    /// The caller must clear its input immediately and never log or persist it.
    /// </summary>
    public bool TrySubmitCode(string input)
    {
        if (Completed || input.Length > 65536)
            return false;
        input = input.Trim();
        string? code;
        if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            if (!StringComparer.Ordinal.Equals(uri.GetLeftPart(UriPartial.Path), RedirectUri) || uri.Fragment.Length != 0)
                return false;
            var query = HttpUtility.ParseQueryString(uri.Query);
            if (!MatchesState(query["state"]))
                return false;
            code = query["code"];
        }
        else
        {
            var parts = input.Split('#');
            if (parts.Length > 2 || (parts.Length == 2 && !MatchesState(parts[1])))
                return false;
            code = parts[0];
        }
        return ClaudeAuthClient.SafeToken(code) && ManualCode.TrySetResult(code!);
    }

    internal bool MatchesState(string? value) => value is not null && CryptographicOperations.FixedTimeEquals(
        Encoding.UTF8.GetBytes(value), Encoding.UTF8.GetBytes(State));

    public override string ToString() => "ClaudeBrowserAuthorization (redacted)";

    /// <summary>Dispose after the completion task has drained.</summary>
    public void Dispose()
    {
        Completed = true;
        Callback.Dispose();
        Gate.Dispose();
    }
}
