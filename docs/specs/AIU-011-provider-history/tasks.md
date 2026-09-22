---
id: AIU-011
schema_version: 1
---
# AIU-011 implementation ledger

Base: c38467a. Owner explicitly selected implementation after the broader feasibility
assessment. Execute the [design and plan](design.md) sequentially on main. Existing
authorization only; local observation history stays deferred to AIU-029.

- T-01: Core contracts, hardened Codex/Copilot transports and parsers implemented.
  Initial new tests failed for absent contracts; targeted history tests now pass 12/12.
- T-02: Existing-session history operations, sanitized console probe and shared live
  scheduling implemented. Codex session/history checks passed 27/27 before additional
  parser tests; coalescing/disconnect/shutdown tests passed 2/2.
- T-03: Provider report presentation, automatic loading, native values, custom dates,
  memory cache and direct account entry implemented. Presentation suite passed 150/150;
  additional live scheduling tests passed separately. Interactive UI acceptance pending.
- T-04: In progress. Infrastructure suite passed 295/295 before five additional tests.
  Latest full-suite and build evidence will be recorded in verification.md.

Ruling: retain a presentation-specific report DTO and map it in Adapters/Live, following
existing frontend/backend separation. An initial direct Core dependency failed the
architecture test; the adapter fixes the cause without weakening that test.

Ruling: retain the old synthetic local-history contracts for demo sparklines and their
existing regression tests. Product provider reports have their own units and periods;
no percentage chart is generated from tokens or credits.

Live prerequisite: standard development state directory is absent; the installed
AiUsage.Dev package provider directory contained only a Claude lock file, no Codex or
Copilot grant. The owner was asked for an alternate app-owned directory while independent
work continues. No source CLI credential or new login is used.

Exact next action: run the integrated Windows history smoke against the unpackaged build,
then finish focused independent review and record any unavailable live acceptance honestly.
