# Ideas inbox

Raw, unreviewed ideas from the owner. Nothing here is backlog, a spec or an
authorization to implement. Each idea must be reviewed and, if accepted, moved
into docs/backlog.md as a `T-NEW` item before any work starts. Agents never
pick work from this file.

Ideas may be written in Ukrainian, as the owner states them.

Statuses: new / discussing / moved (to T-nnn) / dropped.

<!-- New ideas go at the top, below this line. Format:

## <short title>
- status: new
- added: YYYY-MM-DD
- idea: what the owner said, close to their words
- notes: questions and thoughts from discussion (optional)

-->

## Legal, ban-safe auth and usage-limit sources per provider
- status: new
- added: 2026-10-11
- idea: Перевірити, які ще можливі способи авторизації та отримання usage limits провайдерів,
  які були б легальними і точно не забанили акаунт. Ідеї можна взяти з більш зрілого
  монітора https://github.com/onllm-dev/onwatch.
- notes: Desk research only (2026-10-10, Haiku subagents, claims not independently verified,
  nothing run live). Direction: read limits through each provider's own official local tool
  instead of reusing another app's OAuth client against internal endpoints.
  Claude: Claude Code statusline JSON `rate_limits` (5h/7d %, resets_at), written by an
  opt-in user script to a local file; data only while a session runs; changes the owner's
  Claude Code settings. Current own-login with the Claude Code client is restricted by the
  Anthropic legal page (high risk).
  Codex: local `codex app-server` child, `account/rateLimits/read` + `account/rateLimits/updated`
  (primary/secondary usedPercent, resetsAt, plan, credits); protocol experimental; OpenAI
  allows app-server auth for local/open-source, not commercial/hosted. Fallback: `token_count`
  rate_limits in `~/.codex/sessions` rollouts (stale, files hold prompts). Subscription off
  since 2026-10-05, so no live check now.
  Copilot: own GitHub App (device flow, Plan read) + documented billing REST
  (`/users/{u}/settings/billing/premium_request/usage`, `/ai_credit/usage`); consumed only,
  remaining derived from plan allowance, reset 1st 00:00 UTC. Alternative: Copilot SDK
  `account.getQuota` (remaining %, resetDate; new dependency). `copilot_internal/user` with the
  OpenCode client is undocumented (medium risk, may already break).
  Antigravity: FAQ and Terms s.6 name third-party credential use as suspension grounds; the
  current v1internal method is high risk. Only clean route found: manual entry from Settings or
  `agy /usage`. Local language-server GetUserStatus (used by onwatch, quota extensions) is
  undocumented and likely covered by s.6.
  onwatch (v2.14.11, ea66113): reads and writes back CLI credential files, spoofs a claude-cli
  User-Agent, advertises "rate limit bypass via token rotation", reads Antigravity CSRF token
  from process args. Not a model to copy; only the "local sources" idea is useful.
  Not useful: Anthropic/OpenAI Admin usage APIs (API orgs only), OTel and JSONL logs (tokens,
  no limits), rate-limit headers (need inference), scraping usage pages.
  Any switch is its own task with provider-evidence and owner approval.

## Kanban board instead of in-repo task tracking
- status: discussing
- added: 2026-10-11
- idea: Хочу працювати через канбан: зараз не бачу візуально задач і проблем, не можу згадати, на чому закінчили. Треба щось просте. AI-агент має доступ до борди: заповнює, редагує, створює підзадачі, сам робить декомпозицію, сам підхоплює задачі й починає роботу. Я — PM, Claude бере в розробку і лише питає уточнення, бажано з дизайн-прикладами.
- notes: Recommended option (2026-10-07 discussion): GitHub Projects + Issues — repo is
  already on GitHub, `gh` is authenticated, native sub-issues, PRs link to and close cards,
  mobile app. Alternatives: Linear (best UX, connector exists but unauthorized, extra
  service/limits), Notion/Jira/Asana (too heavy), markdown kanban in repo (same problem).
  Proposed columns: Inbox → Needs input → Ready → In progress → Review → Done (+ Blocked).
  Owner moving a card to Ready = authorization (replaces "backlog status does not select
  work"; needs a decision record and AGENTS.md/CONTRIBUTING.md update). Claude triages Inbox
  (description, acceptance criteria, sub-issues, questions as comments, 2-3 UI variants),
  keeps a "state + next step" comment per card. Auto-pickup via a local scheduled task in
  Claude desktop (cloud runners cannot build/check WinUI). Specs/decisions stay in repo;
  open backlog items (20 at the time) move to issues, done ones stay as archive.
  Prerequisite: `gh auth refresh -s project`.
