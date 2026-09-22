using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using AiUsage.Core.History;
using AiUsage.Core.Usage;

namespace AiUsage.Infrastructure.Providers.Copilot;

internal sealed class CopilotHistoryClient(HttpClient client, TimeProvider? timeProvider = null, ProviderTransportOptions? options = null)
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    private DateTimeOffset retryAt;
    internal async Task<ProviderHistoryResult> FetchAsync(CopilotCredentials credentials, HistoryRange range, CancellationToken token)
    {
        range.Validate();
        if (retryAt > clock.GetUtcNow()) return new(range, clock.GetUtcNow(), [new("history", HistoryStatus.RateLimited, [], retryAt)]);
        var reports = new List<HistoryReport>();
        try
        {
            // Never trust a stored display label as the account's billing route.
            using var identity = await GetAsync(CopilotHttp.IdentityUrl, credentials, token).ConfigureAwait(false);
            var root = identity.Body!.RootElement;
            var id = HistoryJson.Property(root, "id");
            if (id.ValueKind != JsonValueKind.Number || !id.TryGetInt64(out var number) || number.ToString(CultureInfo.InvariantCulture) != credentials.AccountId)
                throw new ProviderException(ProviderFailureKind.AccountMismatch);
            var login = HistoryJson.Text(root, "login");
            if (string.IsNullOrEmpty(login) || login.Length > 100 || !login.All(c => char.IsAsciiLetterOrDigit(c) || c == '-')) throw HistoryJson.Invalid();
            foreach (var report in new[] { "ai_credit", "premium_request" })
                foreach (var period in Periods(range))
                {
                    token.ThrowIfCancellationRequested();
                    var key = report + ":" + period.From.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    try
                    {
                        var query = $"?year={period.From.Year}&month={period.From.Month}" + (period.From == period.To ? $"&day={period.From.Day}" : "");
                        using var response = await GetAsync($"https://api.github.com/users/{Uri.EscapeDataString(login)}/settings/billing/{report}/usage{query}", credentials, token).ConfigureAwait(false);
                        if (HistoryJson.Text(response.Body!.RootElement, "user") is { } user && !string.Equals(user, login, StringComparison.OrdinalIgnoreCase))
                            throw new ProviderException(ProviderFailureKind.AccountMismatch);
                        reports.Add(HistoryJson.Report(key, CopilotHistoryParser.Parse(response.Body.RootElement, period)));
                    }
                    catch (ProviderException error)
                    {
                        if (error.Kind == ProviderFailureKind.AccountMismatch) return ProviderHistoryResult.Unavailable(range, HistoryStatus.AccountChanged);
                        reports.Add(Failure(key, error));
                        if (error.Kind is ProviderFailureKind.AuthenticationRequired or ProviderFailureKind.RateLimited)
                            return new(range, clock.GetUtcNow(), reports.AsReadOnly());
                        // Stop this report's period fan-out; the other billing report may remain accessible.
                        break;
                    }
                }
        }
        catch (ProviderException error) { reports.Add(Failure("history", error)); }
        return new(range, clock.GetUtcNow(), reports.AsReadOnly());
    }

    private HistoryReport Failure(string id, ProviderException error)
    {
        if (error.Kind == ProviderFailureKind.RateLimited) retryAt = clock.GetUtcNow() + (error.RetryAfter ?? TimeSpan.FromMinutes(1));
        return new(id, HistoryJson.Status(error.Kind), [], error.Kind == ProviderFailureKind.RateLimited ? retryAt : null);
    }
    private async Task<ProviderHttpResponse> GetAsync(string url, CopilotCredentials credentials, CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credentials.AccessToken);
        request.Headers.Add("X-GitHub-Api-Version", "2026-03-10");
        var response = await ProviderTransport.SendAsync(client, request, clock, token, options).ConfigureAwait(false);
        if (response.IsSuccess) return response;
        using (response) throw ProviderTransport.Failure(response);
    }
    internal static IEnumerable<HistoryRange> Periods(HistoryRange range)
    {
        for (var date = range.From; date <= range.To;)
        {
            var end = new DateOnly(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
            if (date.Day == 1 && end <= range.To) { yield return new(date, end); date = end.AddDays(1); }
            else { yield return new(date, date); date = date.AddDays(1); }
        }
    }
}
