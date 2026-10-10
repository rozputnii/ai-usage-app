---
topic: peak-hours
checked_at: 2026-10-10
classification: official announcements and plan pages; no machine-readable signal
backlog: T-063
---
# Provider peak hours

The app's peak hint (T-063) uses one shared schedule: **weekdays 05:00 to 11:00 Pacific time**.
It is a hint that requests may be slower or use more of a limit, not account data. Review it
under R-221: when `checked_at` is more than a month old, the session's primary agent spends a
short pass on the public sources below (no signed-in pages, provider APIs, inference requests
or credentials) and updates this file and its review log. A window change then goes into
`src/windows/AiUsage.Windows/Features/Ledger/PeakHours.cs` and its tests as its own verified
change; a change of shape is asked first.

## Signal

No provider exposes whether peak applies now. The Claude OAuth usage payload has only
per-limit utilization and reset times (see [Claude](claude.md)); the OMP Claude adapter at
`main` commit `d45ba77ee6a5efa389ab1fcde4711276e805c8dd`, checked 2026-10-10, has no peak
field. The `anthropic-ratelimit-unified-*` headers
come only with inference responses, which this monitor never makes. No provider publishes
load by hour.

## Evidence, 2026-10-10

| Provider | What is announced | Source |
| --- | --- | --- |
| Claude | 2026-03-26: on weekdays 05:00 to 11:00 PT ("1pm–7pm GMT" in the post; 12:00 to 18:00 UTC before UK summer time began on 29 March) Free, Pro and Max five-hour session limits drain faster; weekly limits unchanged. 2026-05-06: the reduction removed for Claude Code on Pro and Max. Chat status and Team or Enterprise are not stated. Max, Team and Enterprise list "priority access at high traffic times". | Anthropic staff post quoted by [InfoWorld](https://www.infoworld.com/article/4151196/anthropic-throttles-claude-subscriptions-to-meet-capacity.html); [Anthropic, 2026-05-06](https://www.anthropic.com/news/higher-limits-spacex); [pricing](https://claude.com/pricing) |
| Codex (OpenAI) | No peak window. 2026 changes concern five-hour limits and resets after bugs. | Third-party reports, for example [Digital Trends](https://www.digitaltrends.com/computing/openai-just-took-the-handcuffs-off-your-chatgpt-work-and-codex-usage-limits-at-least-for-now/) |
| Copilot | No window. Docs say response times may vary and requests may be rate limited during high usage. | [GitHub Docs](https://docs.github.com/en/copilot/reference/copilot-billing/request-based-billing-legacy/copilot-requests) |
| Antigravity (Google) | No window. Demand led to quota changes; an off-peak token discount is claimed by a partner for Gemini Enterprise only, without hours. | [Google blog](https://blog.google/feed/new-antigravity-rate-limits-pro-ultra-subsribers/); [Dito](https://www.ditoweb.com/2026/08/google-antigravity-now-available-and-deeply-integrated-with-gemini-enterprise/) |
| Industry | OpenRouter names the US morning as peak global traffic, when hosts queue and time to first token rises. | [OpenRouter](https://openrouter.ai/docs/guides/best-practices/latency-and-performance) |

The only published window is Anthropic's, and it matches the US-morning load OpenRouter
describes. The shared schedule therefore uses it for every account. Third-party trackers
(for example [UsageMeter](https://usagemeter.app/en/blog/claude-ai-peak-hours-surge-pricing-explained))
assume the chat reduction still applies; that is not confirmed by Anthropic.

## Review log

| Date | Result |
| --- | --- |
| 2026-10-10 | First research; window set to weekdays 05:00–11:00 Pacific. |
