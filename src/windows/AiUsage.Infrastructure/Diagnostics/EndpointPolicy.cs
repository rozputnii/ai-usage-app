namespace AiUsage.Infrastructure.Diagnostics;

/// <summary>Routes are source constants/templates; URLs and query values are never persisted.</summary>
internal sealed record EndpointPolicy(string Provider, string Route, string Kind)
{
    public string Id => $"{Provider}/{Kind}/v1";
    internal static EndpointPolicy Classify(Uri? uri)
    {
        if (uri is null || !uri.IsAbsoluteUri || uri.Scheme != "https") return new("unknown", "unclassified", "unknown");
        var host = uri.Host;
        var path = uri.AbsolutePath;
        if (host == "auth.openai.com" && path is "/oauth/token" or "/api/accounts/deviceauth/usercode" or "/api/accounts/deviceauth/token")
            return new("codex", path, "auth");
        if (host == "chatgpt.com" && path == "/backend-api/wham/usage") return new("codex", path, "quota");
        if (host == "api.anthropic.com" && path == "/v1/oauth/token") return new("claude", path, "auth");
        if (host == "api.anthropic.com" && path == "/api/oauth/usage") return new("claude", path, "quota");
        if (host == "api.anthropic.com" && path == "/api/claude_cli/bootstrap") return new("claude", path, "identity");
        if (host == "github.com" && path is "/login/device/code" or "/login/oauth/access_token") return new("copilot", path, "auth");
        if (host == "api.github.com" && path == "/user") return new("copilot", path, "identity");
        if (host == "api.github.com" && path == "/copilot_internal/user") return new("copilot", path, "quota");
        if (host == "oauth2.googleapis.com" && path == "/token") return new("antigravity", path, "auth");
        if (host == "www.googleapis.com" && path == "/oauth2/v1/userinfo") return new("antigravity", path, "identity");
        if (host == "daily-cloudcode-pa.googleapis.com")
        {
            if (path == "/v1internal:retrieveUserQuotaSummary") return new("antigravity", path, "quota");
            if (path is "/v1internal:loadCodeAssist" or "/v1internal:onboardUser") return new("antigravity", path, "provisioning");
            if (path.StartsWith("/v1internal/operations/", StringComparison.Ordinal)) return new("antigravity", "/v1internal/operations/{operation}", "provisioning");
        }
        return new("unknown", "unclassified", "unknown");
    }

    internal bool Numeric(string key) => Kind switch
    {
        "auth" => key is "expires_in" or "interval" or "expires_at",
        "quota" when Provider == "codex" => key is "used_percent" or "limit_window_seconds" or "reset_at" or "reset_after_seconds" or "balance" or "available_count",
        "quota" when Provider == "claude" => key is "utilization" or "percent" or "amount_minor" or "exponent" or "used" or "limit" or "remaining" or "used_credits" or "monthly_limit" or "used_usd" or "limit_usd" or "used_usd_cents" or "limit_usd_cents",
        "quota" when Provider == "copilot" => key is "entitlement" or "remaining" or "percent_remaining" or "overage_count" or "overage_permitted" or "quota_remaining",
        "quota" when Provider == "antigravity" => key is "remainingAmount" or "remainingFraction" or "limit" or "quota" or "used",
        _ => false
    };

    internal bool Boolean(string key) => Kind is "quota" or "provisioning" && key is "allowed" or "limit_reached" or "has_credits" or "unlimited" or "reached" or "is_enabled" or "enabled" or "is_active" or "overage_permitted" or "done" or "isDefault";

    internal static bool KnownKey(string key) => Names.Contains(key);
    internal static bool CanDescend(string key) => key is not ("access_token" or "refresh_token" or "id_token" or "authorization_code" or
        "device_code" or "user_code" or "client_secret" or "code" or "verifier" or "account" or "organization" or "email" or "login" or
        "id" or "name" or "user" or "project" or "cloudAICompanionProject" or "verification_uri" or "verification_url" or "scope" or "scopes" or "error_description");
    private static readonly HashSet<string> Names = new((
        "access_token refresh_token id_token authorization_code device_code user_code client_secret code verifier token_type expires_in expires_at interval error error_description " +
        "account organization email login id name user project cloudAICompanionProject verification_uri verification_url scope scopes " +
        "rate_limit primary_window secondary_window code_review_rate_limit additional_rate_limits limit_name metered_feature model plan_type credits has_credits unlimited balance " +
        "used_percent limit_window_seconds reset_at reset_after_seconds allowed limit_reached rate_limit_reset_credits available_count spend_control reached rate_limit_reached_type type " +
        "five_hour seven_day seven_day_opus seven_day_sonnet seven_day_oauth_apps seven_day_cowork limits extra_usage spend utilization percent amount_minor exponent enabled is_active kind resets_at used limit remaining used_credits monthly_limit is_enabled used_usd limit_usd used_usd_cents limit_usd_cents " +
        "quota_snapshots premium_interactions chat completions entitlement percent_remaining overage_count overage_permitted quota_remaining copilot_plan access_type quota_reset_date quota_reset_date_utc " +
        "groups buckets quotaInfos remainingAmount remainingFraction resetTime quotaId modelId currentTier allowedTiers paidTier tierId done response result data " +
        "usageItems usageItemsSummary usage usage_summary daily_usage daily_tokens events items date day timestamp unit unitType quantity grossAmount discountAmount netAmount count total input_tokens output_tokens cached_input_tokens tokens amount currency product sku " +
        "").Split(' ', StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);

    internal bool StringValue(string key, string value)
    {
        if (Numeric(key) && value.Length <= 100 && decimal.TryParse(value, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out _)) return true;
        if (key == "token_type" && Kind == "auth") return value is "Bearer" or "bearer";
        if (key == "error") return value is "authorization_pending" or "slow_down" or "expired_token" or "access_denied" or "invalid_grant" or "invalid_request" or "invalid_client";
        if (Kind != "quota") return false;
        if (key is "resetTime" or "resets_at" or "quota_reset_date" or "quota_reset_date_utc" or "date" or "day" or "timestamp")
            return value.Length <= 35 && DateTimeOffset.TryParse(value, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal, out _);
        if (key is "unit" or "unitType") return value is "requests" or "tokens" or "credits" or "USD" or "usd" or "count";
        if (key == "currency") return value is "USD" or "EUR" or "GBP";
        if (key == "kind" && Provider == "claude") return value is "session" or "weekly_all" or "weekly_scoped";
        if (key is "plan_type" or "copilot_plan") return value is "free" or "plus" or "pro" or "team" or "business" or "enterprise" or "individual";
        return false;
    }
}
