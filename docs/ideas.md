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
