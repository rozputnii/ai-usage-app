---
id: T-050
type: feature
status: implemented
goal: G-003
scope_version: 1
approval_basis: Owner direction, 2026-10-07, in conversation. The owner asked that one account never show several limit cards, chose design A (every other limit is a section of the account card, as the Claude model limit already was) and design H2 (each section can be hidden fully, and the account card shows "N hidden" with the most urgent colour to show them again), with one rule for every additional limit. Recorded as R-191.
---

# One card per account, with hideable limit sections

## Problem

R-186 makes each subscription one card, but the window grouped only model limits
and spending into the account card. Every other limit of the same account got its
own tile. A Copilot Business account showed three cards (Premium requests, Chat and
Completions); Codex individual limits and Antigravity model groups split the same way.
The owner also wants to hide a limit section they do not need, Fable included.

## Requirements

- **R-01 One card.** Each account is one card. Its primary limit heads it: the first
  shown limit in the account order, preferring a subscription window or pool over a
  model limit or spending, and either over a note-only limit (no cap, not included).
  Every other limit is a section below a divider, in account order with bar limits
  first, then spending, then note-only limits. A section keeps its own label, history,
  cap and status pill; sign-out, rename and account marks stay on the primary limit.
- **R-02 Hide.** Each section has a quiet hide button. A hidden section leaves the
  window. The primary limit cannot be hidden. The choice is per limit and is kept with
  the window's preferences (labels, order), so it survives restart. Provider data,
  readings, caps and the tray are unchanged; the tray still shows every limit.
- **R-03 Show again.** An account card with hidden sections shows "N hidden" after the
  name, with a dot in the most urgent hidden colour (red, orange, else neutral) and a
  tooltip naming the hidden limits. Activating it shows every hidden section of that
  account again.
- **R-04 Fallback.** If only hidden limits remain on an account, the first of them heads
  the card so the account never disappears.
- **R-05 Keyboard.** Hiding keeps focus on the account card, and showing focuses the
  shown section. Alt+Up/Down still reorders limits within the account.

## Acceptance criteria

- AC-01: In the demo Brief scenario every account is one card: Copilot Free shows
  completions with chat and premium sections, and Antigravity shows group 1 with a
  group 2 section.
- AC-02: Hiding a section removes it from the card, the primary limit shows
  "1 hidden" with the matching dot, and showing it restores the section.
- AC-03: The live source rejects hiding the primary limit or an unknown card, persists
  a hidden section across a source restart and shows it again on request.
- AC-04: A preference file without the hidden list loads as before; a null or
  duplicated hidden list is rejected without being overwritten.
- AC-05: Presentation and Infrastructure suites, document validation and the diff
  check pass; the demo app shows AC-01 and AC-02.
