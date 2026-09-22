using AiUsage.Core.Usage;
using AiUsage.Core.Providers.Claude;
using System.Net;

namespace AiUsage.Infrastructure.Providers.Claude;

internal static class ClaudeHttp
{
    internal const string ClientId = "9d1c250a-e61b-44d9-88ed-5944d1962f5e";
    internal const string TokenUrl = "https://api.anthropic.com/v1/oauth/token";
    internal const string UsageUrl = "https://api.anthropic.com/api/oauth/usage";

}
