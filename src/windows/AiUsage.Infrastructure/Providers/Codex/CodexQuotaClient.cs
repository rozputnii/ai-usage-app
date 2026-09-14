using System.Net;
using System.Net.Http.Headers;
using AiUsage.Core.Usage;

namespace AiUsage.Infrastructure.Providers.Codex;

public sealed class CodexQuotaClient(HttpClient client, TimeProvider? timeProvider = null)
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;

    public async Task<QuotaSnapshot> GetQuotaAsync(CodexCredentials credentials, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        await credentials.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            credentials.EnsureUsable();
            if (credentials.ExpiresAt <= clock.GetUtcNow())
                throw new CodexException(CodexFailureKind.AuthenticationRequired);
            using var request = new HttpRequestMessage(HttpMethod.Get, CodexHttp.UsageUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credentials.AccessToken);
            request.Headers.Add("ChatGPT-Account-Id", credentials.AccountId);
            // The inspected OMP usage path sends only these headers; residency stays unsent until a
            // real region rejection proves it is required.
            using var response = await CodexHttp.SendAsync(client, request, clock, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccess)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    credentials.RequireReauthentication();
                throw CodexHttp.Failure(response);
            }
            var root = response.Body!.RootElement;
            var accountValue = CodexQuotaParser.Property(root, "account_id");
            if (accountValue.ValueKind is not (System.Text.Json.JsonValueKind.String or System.Text.Json.JsonValueKind.Null or System.Text.Json.JsonValueKind.Undefined))
                throw new CodexException(CodexFailureKind.InvalidResponse);
            var returnedAccount = CodexQuotaParser.Text(accountValue);
            if (returnedAccount is not null && !StringComparer.Ordinal.Equals(returnedAccount, credentials.AccountId))
                throw new CodexException(CodexFailureKind.AccountMismatch);
            return CodexQuotaParser.Parse(root, clock.GetUtcNow());
        }
        finally { credentials.Gate.Release(); }
    }
}
