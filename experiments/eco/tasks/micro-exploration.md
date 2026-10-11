# Micro-trial: exploration (S2)

Tests H6 and H9: which scout configuration answers code-location questions correctly for the
fewest tokens. Base: `main` at the S0 commit's parent on `main` (`1a8526e`), so the key below
stays true.

## Method

Each trial is one `Start-Run.ps1` run of the probe brief below, with a haiku low primary that
only delegates, and `-AgentsFile` set to [the scout variants](../arms/trials-scouts.json). The
measurement is the subagent's own entry in `metrics.json` (`subagents[]`): cost, calls, cold
start. Accuracy is scored against the key by the operator: 2 = correct and complete, 1 =
partly, 0 = wrong. Two repeats per variant and question; randomize the order.

Variants: `scout-grep` (Read, Grep, Glob), `scout-lsp` (+ LSP), `scout-graphify` (+ Bash for
`graphify query`, after `graphify .` has built the graph outside the measured run),
`scout-claudemd` (as scout-lsp but with CLAUDE.md), `scout-sonnet` (scout-lsp on sonnet), and
the built-in `Explore` on haiku (pass model haiku on the call).

Run `scout-graphify` only after `graphify .` in the trial clone; record the build time and
whether its semantic pass was skipped (code-only, no LLM use). The graph output stays out of
Git (clone only).

## Probe {#probe}

This is a measurement probe, not real work. Do exactly this:

1. Delegate the question below to the agent named `<VARIANT>` (for `Explore`, use the
   built-in Explore agent with model haiku), passing the question verbatim and nothing else.
2. Return the agent's answer verbatim in NOTES. Make no changes, read no files yourself.

Question: `<QUESTION>`

## Questions and key

| ID | Question | Key |
| --- | --- | --- |
| X1 | Why does a usage bar's fill grow from the middle instead of from the left when its value changes? Name the files and lines. | `Controls/Ledger/LimitBars.cs` `BuildCell` adds each fill to a star-sized grid column and calls `LedgerMotion.WidthOnLoad`; `Controls/Ledger/LedgerMotion.cs` `WidthOnLoad` animates `Width` while the fill keeps the default stretch alignment, so it is centered in its column and grows from the middle |
| X2 | Which code decides where a dragged account card will be inserted? | `Features/Ledger/ReorderMath.cs` `InsertionIndex` (counts other cards' midpoints above the pointer), called from `LedgerWindow.xaml.cs` `DropAt` |
| X3 | Where does the Antigravity provider get its OAuth client id, and what happens when it is missing? | `AiUsage.Infrastructure/Providers/Antigravity/AntigravityHttp.cs` reads `AIU_ANTIGRAVITY_CLIENT_ID` and `AIU_ANTIGRAVITY_CLIENT_SECRET` from the environment; unconfigured reports `RegistrationUnavailable` without contacting the provider |
| X4 | What is the default automatic refresh interval for accounts, and where is it defined? | 5 minutes, `Adapters/Live/LiveLedgerSource.cs` `DefaultRefreshInterval` (not `UpdateCoordinator.CheckInterval`, which is the update check) |
| X5 | Where is the peak-hours schedule defined, and what is it? | `Features/Ledger/PeakHours.cs`: weekdays 05:00-11:00 Pacific (`America/Los_Angeles` or `Pacific Standard Time`) |
| X6 | Which provider clients read a Retry-After value? | Antigravity (`AntigravityQuotaClient`), Claude (`ClaudeQuotaClient`), Codex (`CodexHttp`, `CodexException`), Copilot (`CopilotQuotaClient`) |

Paths are relative to `src/windows/` (Windows project files under `AiUsage.Windows/`). Before
the first trial the operator re-confirms each key line against the base with grep and fixes the
key if the code moved.
