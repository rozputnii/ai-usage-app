---
id: T-NEW
type: bug
status: implemented
goal: G-003
scope_version: 1
approval_basis: owner request, 2026-11-01, in conversation: Work today looks the same on and off; drop the caption beside it and colour the button while on; review the installed app's logs, find the errors and fix them, as one Claude account (its subscription ended, now a free plan) does not sync.
---
# Work today state and a refused usage read

## Problem

- Current (Work today): on a day off the title row shows a caption ("Day off" or "Extra work
  day · until midnight") beside the Work today button. Off, the button is outlined; on, it
  takes the segment control's grey surface (`LedgerSegmentOnButton`, #3A3936 on #1F1E1D), which
  reads like the outline. The owner could not tell on from off.
- Current (refused read): the installed app's log of 2026-11-01 shows the second Claude
  account renewing its token (200) and its usage read answering 403 at every refresh; after
  three manual refreshes within 17 seconds the fourth read got 429 with Retry-After 3600. The
  owner confirmed that the account's subscription ended and it is on the free plan. The
  projection treats `AccessDenied` like any failure, so the card keeps the figures from before
  the subscription ended, marked "past reset", "5h past reset" and "sync failed", with
  "not ready".
- Expected: Work today has no caption; it stays outlined while off and is filled with the extra
  day's green with a tick while on. A refused usage read shows one note card that says so.
- Unchanged: stored readings, caps, names and history (the next successful read shows the
  figures again); the retry schedule (10, 20, then 30 minutes) and manual refresh; every other
  failure keeps its figures with the "sync failed" mark; logging; what Work today does, its
  toast and Undo.

## Requirements

- R-01: The title row shows no day caption. The Work today button is outlined while off, and
  filled with the OkP green (#5E8A6B) with a tick while on, at the same size; its tooltip and
  accessible name carry the state.
- R-02: A connected account whose last refresh failed with `AccessDenied`, and is neither
  sign-in expired nor in recovery, shows one note card in the state `NoAccess` instead of its
  limit cards, in the attention tone. The note shows nothing old, so it is not stale: it is not
  dimmed and its spoken name gives no reading time.
- R-03: The `NoAccess` card's pill is "no access"; its tip is "No usage access" and the last
  reading time with the retry; its note is "Claude doesn't share usage for this account · a free
  plan has none" for Claude and "<Provider> doesn't share usage for this account" otherwise. The
  tray row's tip says "No usage access" with the same note, and the refresh button's tip names
  the account with "no access" and no reading time; the button stays red.
- R-04: Every other refresh failure keeps the figures and the "sync failed" mark.

## Acceptance criteria

- AC-01: `AccessDeniedShowsOneNoAccessNoteInsteadOfTheOldFigures` (Claude and Copilot) shows
  R-02 to R-04 for the card, `NoAccessRowSaysSoInsteadOfOfferingAReading` R-03 for the tray and
  `NoAccessAccountSaysSoInTheRefreshTip` R-03 for the refresh tip; all fail at the base.
- AC-02: The state gallery covers `NoAccess` (H12), and the gallery geometry and spoken-name
  checks pass.
- AC-03: The window-size smoke turns Work today on in the day-off demo without overlap, and its
  screenshots show the outline while off and the green fill with the tick while on.
- AC-04: C2, C4, C5, C6, C7 and C8 pass.

## Out of scope

- Why a provider refuses: the sanitizer withholds the 403 body's values, and the provider
  record in docs/providers is unchanged.
- Fewer manual reads of a refused account: the 429 followed three quick manual refreshes; the
  automatic backoff already waits 10, 20, then 30 minutes.
- Log retention: only the current session's application log was present to review.

Variant: A (green fill with a tick) and 1 (one note line, old bars hidden), owner, 2026-11-01.
