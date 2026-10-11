# Baseline from past sessions

Aggregated from the owner's local Claude Code transcripts on the current PC for this repository
(all worktrees), measured 2026-10-11 with `tools/Measure-Run.ps1 -Baseline`. Counters only; no
content was read. Costs are API-price equivalents (the eco skill's price table), a proxy for
the subscription limit. The transcripts exist only on that PC, so these numbers cannot be
recomputed on the new PC.

## Totals

| Item | Value |
| --- | --- |
| Primary sessions | 101 |
| Subagents | 180 |
| Primary cost | $1354 (70%) |
| Subagent cost | $580 (30%) |
| Median primary session | $4.2, 48 calls |
| Three most expensive sessions | 29% of primary cost: $171 (696 calls, 0.96M max context), $120 (535 calls, 0.92M), $101 (461 calls, 0.82M) |
| Usual primary | Opus 5.5 high in 48 sessions, medium in 25, xhigh in 16 |

## Cost shares

| Part | Primary | Subagents |
| --- | --- | --- |
| Cache write | 20% | 27% |
| Cache read | 67% | 60% |
| Output | 13% | 14% |
| Thinking share of output tokens | 33% | 45% |

Primaries use the 1-hour prompt cache (writes at twice the input price); subagents use the
5-minute cache.

## Subagents by type

| Type | Count | Median cold start (tokens) | Mean cost | Mean calls |
| --- | --- | --- | --- | --- |
| general-purpose | 111 | 55,433 | $3.39 | 38 |
| aiu-reviewer | 49 | 23,854 | $1.90 | 30 |
| aiu-implementer | 12 | 43,022 | $8.36 | 81 |
| Explore | 3 | 42,882 | $2.55 | 27 |
| aiu-simple-implementer | 3 | 53,275 | $0.74 | 23 |
| Plan | 1 | 36,965 | $0.81 | 5 |

`general-purpose` and `Explore` inherit about 42 tools, including every MCP tool of the account
(browser, documents and others); `aiu-reviewer` has 7. The S0 smoke measured the lean `scout`
role (4 tools, no CLAUDE.md) at a 4,042-token cold start.

## What it implies

- The largest lever is the primary's context length times its number of calls, above all in
  long sessions; model choice for subagents comes second.
- Unneeded tool schemas cost about 30k tokens per subagent start; tool allowlists and
  `omitClaudeMd` for read-only roles remove most of that.
- Output, including thinking, is about an eighth of spend, so terse replies help little on
  their own.
