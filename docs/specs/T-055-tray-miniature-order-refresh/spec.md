---
id: T-055
type: feature
status: implemented
goal: G-003
scope_version: 1
approval_basis: Owner direction, 2026-10-08, in conversation. The owner listed seven changes. In the tray flyout there are no tab stops or focus frames, and only today's budget shows, without a five-hour split; a subscription with a five-hour window gets a clock-like five-hour indicator. The tray bars are taller, styled like the window's today strip and follow density, and a provider icon replaces the name, with the name in a tooltip. In the main window the owner renames a subscription by clicking its name and orders subscriptions by hand. A refresh interval setting is added, default 5 min, minimum 1 min, in whole minutes. Of the presented variants, the owner chose a thin ring arc for the five-hour indicator, dragging by a grip for the order, a stepper with a 1–60 min range whose 15-minute continuity rules scale with the interval, and only the main limit per subscription in the tray. The owner directed that this batch run in parallel as autonomous subagents, each of which merges and pushes its own reviewed work, with no questions. Recorded as R-204, R-205 and R-206.
---

# Tray miniature, subscription order, click rename and refresh interval

## Problem

Checked in the source on 2026-10-08 at `b81ef9e`:

- The tray flyout (`LedgerTrayWindow`) makes every account row and every strip a tab
  stop with a focus frame. Opening the flyout focuses the first row, so a white frame
  shows around it (owner screenshot).
- Each row shows the account name in a 150 px column and one 8 px today strip per limit.
  A 5h + period limit is split into five-hour cells, and model and spending limits get
  their own strips. The flyout is 360 px wide and ignores density.
- An account is renamed only with F2. Accounts are ordered by sign-in order, and
  Alt+↑/↓ moves a limit only within its account (`MoveCardAsync`).
- The refresh interval is a fixed 5 min (`LiveLedgerSource.RefreshInterval`). After a
  successful refresh the next one waits a fixed 5 min (`NextRetry`). The one-minute tick
  and the `>=` test add up to one more minute between refreshes. Readings are stale
  after a fixed 15 min, and the Core decides whether readings are continuous with a
  fixed 15 min.

## Requirements

### Tray flyout (R-204)

- **R-01 No keyboard focus.** No element of the tray flyout is a tab stop. No focus
  frame ever shows, and opening the flyout focuses nothing. Arrow-key and Enter
  navigation in the flyout is removed. A click on a row still opens the window at that
  account, tooltips still show on hover, and the flyout still hides when it loses
  activation. Esc still closes the flyout if the window receives the key while nothing
  is focused; otherwise a click elsewhere closes it.
- **R-02 Main limit only.** There is one row per shown account; signed-out accounts
  follow "Show signed-out" as now. A row shows the account's primary limit only
  (`AccountCard.Primary`). Model limits, spending and credit pools that are not the
  primary limit, note-only limits and every weekly or monthly period bar stay out of the
  tray. A row whose primary limit is note-only has no bar.
- **R-03 One today bar.** The bar is exactly one cell for every layout, including
  5h + period limits: the today cell the window draws for a period limit. That cell
  shows used today, today's allowance left, the grey cut share, the over part and the
  dashed neutral day-off look. A used-up limit stays one solid red strip and a used-only
  or period-unknown limit an empty dashed track (R-187). The Used or Left mode is
  followed.
- **R-04 Five-hour ring.** A primary limit with a five-hour window (layout
  `FiveHourAndPeriod` with five-hour data) shows a 16 px ring after the bar:
  - The ring is a 2 px rail circle (token `Rail`) and a 2 px arc with round caps that
    starts at 12 o'clock and runs clockwise.
  - The arc spans the share of the current five-hour window used in Used mode, or left
    in Left mode, clamped to 0–100 %. At 100 % it is a full circle.
  - The arc takes the limit's tone colour; a fully used window is critical red, and a
    day off is neutral. Before the window starts only the rail shows.
  - The tooltip reads `Current 5h window · 25 % used · until 18:40` (Left mode
    `75 % left`), or `Next 5h window · starts on first use`.

  The ring column is reserved in every row, so the bars line up.
- **R-05 Bar look.** The bars are 14 px tall with a 6 px corner radius and use the
  window today strip's paints, hatches and over edge (`TodayStrip`, cell height 14). The
  empty dashed and solid red variants use the same size and radius.
- **R-06 Density.** In Compact, rows and the title row use the card's compact padding
  (12, 8); in Comfortable, its comfortable padding (15, 13). A density change applies to
  an open flyout and to the next one opened.
- **R-07 Provider icon.** A 16 px monochrome mark of the account's provider (Claude,
  Codex, Copilot, Antigravity) replaces the name.
  - The marks are simplified, recognisable silhouettes drawn as vector paths in this
    repository; no downloaded asset or new dependency is used.
  - The mark is drawn in `Ink`. On the R-187 error states it is drawn in `CritText`
    instead of the red name and warning triangle.
  - The hover tooltip's first line is the account's display name (owner-editable, for
    example `Copilot business 2`), followed on errors by the existing status lines.
- **R-08 Width.** The flyout is 260 px wide. The title row is unchanged, and the rush
  and on-extra-usage marks stay after the ring. Row automation names stay as they are,
  because the desktop smokes find rows by them.

### Main window (R-205)

- **R-09 Click to rename.** A single click on the account name in a card header starts
  the existing inline rename (as F2) with the text selected. Enter, a click outside the
  box or focus leaving the box saves the name; Esc cancels. An empty or unchanged name
  changes nothing, and the
  limit stays 100 characters. On hover the name shows the dotted underline of the
  "N hidden" mark and the tooltip `Rename`. Sections have no name of their own and are
  unchanged.
- **R-10 Subscription order.**
  - On card hover a quiet grip (a dot-grip glyph from Segoe Fluent Icons) appears at
    the left of the account card's header.
    Implementation clarification, 2026-10-08, within R-205: the installed Segoe Fluent
    Icons font has no dot gripper, so the grip is six dots drawn as a vector path in
    this repository, placed in the card's left padding beside the header so the name
    stays aligned.
  - Dragging the grip lifts the card, which follows the pointer vertically, and shows a
    2 px insertion line between cards. Releasing drops the card there; Esc or lost
    pointer capture cancels the drag. With one account the grip is hidden.
  - Alt+↑/↓ from a control in an account card's header moves the account one place. In
    a section it still moves the section within its account.
  - The order is saved in the Ledger preferences, applies to the window and the tray,
    and survives a restart. New accounts appear last, and hidden signed-out accounts
    keep their place.
  - The preferences gain `AccountOrder`, a list of account IDs: at most 256 distinct
    `N`-format GUIDs. An older file without it loads with no order, which means the
    registry order.

### Refresh interval (R-206)

- **R-11 Setting.** Settings › View gets a `Refresh` row with a stepper:
  `[−] N min [+]`.
  - The number can also be typed: whole minutes from 1 to 60, default 5.
  - − is disabled at 1 and + at 60. A typed value outside the range, or one that is not
    digits, is refused and the field returns to the saved value.
  - Each change saves at once to `LedgerPreferences.RefreshMinutes`, and an older file
    without the field loads 5.
  - The footer caption `every 5 min` is removed; the footer keeps the system status and
    ⋯.
- **R-12 Schedule.**
  - A connected account is due once its last successful reading is at least the
    interval minus 30 s old, which absorbs the one-minute tick.
  - After a successful refresh the next one is due one interval later. The failure
    backoff (10, 20, then 30 min after consecutive failures) is unchanged, and so is a
    manual refresh (F5 or the title-bar button).
  - A changed interval applies at the next tick, without a restart.
- **R-13 Continuity tolerance.** The tolerance T = max(15 min, 3 × interval) replaces
  every fixed 15 min that decides whether readings are current or continuous:
  - a stale reading (the card, the tray and the red refresh status);
  - a day start carried from a reading before midnight;
  - incomplete tracking after a gap;
  - extra-usage evidence.

  With the default 5 min, T is 15 min and nothing changes. The budget store's merge of
  identical consecutive readings keeps its 15 min, because that rule only affects
  storage.
- **R-14 Demo.** The demo source honours `RefreshMinutes` in its settings summary.

## Diagnostics

The interval is a local preference visible in Settings. The worker that wires the
schedule reviews the existing refresh events under AGENTS.md "Useful logging". It adds
the effective interval to an existing refresh or startup record only if one already
describes refresh cadence; it adds no new event or logging configuration otherwise.

## Out of scope

- keyboard navigation in the tray;
- ordering from a settings list;
- per-provider intervals;
- power-, network- or lock-aware refresh (T-012);
- the update-check cadence;
- screen-reader, contrast and display matrices (owner direction 2026-10-03);
- changes to provider requests or the reading-store format.

## Acceptance criteria

- AC-01: In the demo app, no descendant of the tray flyout is keyboard-focusable. A
  screenshot of the opened flyout shows no focus frame, and clicking a row opens the
  window at that account (R-01).
- AC-02: The tray projection gives one row per shown account, with one bar from the
  primary limit. The bar has exactly one cell, whose parts equal the window's today cell
  for that limit drawn as a period limit. No row has a bar for a model, spending or note
  limit. A used-up primary limit gives a solid red strip, and a used-only or
  period-unknown one an empty dashed track (R-02, R-03).
- AC-03: The ring geometry draws no arc at 0 % and, at 25 %, an arc from (0, −r) to
  (r, 0) clockwise. 50 % and 75 % are correct, 100 % is a full circle, and values over
  100 % are clamped. Left mode uses the left share. The tooltip text and colour rules
  follow R-04, and a row without a five-hour window has no ring (R-04).
- AC-04: Demo-app screenshots in both densities show the bars 14 px tall with radius 6,
  the density-dependent row padding and a width of 260 px (R-05, R-06, R-08).
- AC-05: Every `ProviderKind` has a mark. Tray rows show the mark, not the name. The
  tooltip's first line is the display name, and error rows draw the mark in `CritText`
  (R-07).
- AC-06: Clicking the name starts the rename. Enter, a click outside the box and focus
  loss save, and Esc cancels. A demo smoke renames an account by clicking its name
  (R-09).
- AC-07: The subscription order behaves as follows (R-10):
  - `MoveAccountAsync` places an account before another one, or last. The order
    persists and survives a reload; unknown IDs are rejected, and an invalid order is
    rejected by the file validation.
  - The order applies to the window and the tray.
  - Alt+↑/↓ on a header moves the account; on a section it moves the section.
  - In the demo app, dragging the grip moves a card.
- AC-08: The stepper stops at 1 and 60. Typed `0`, `61` and `a` are refused, saving 1
  updates the preferences, and an older file loads 5 (R-11).
- AC-09: With a fake clock and a 1 min interval, an account fetched 2 s after a tick is
  refreshed at the next tick. A successful refresh is followed by the next one interval
  later, the failure backoff is unchanged, and a changed interval applies at the next
  tick (R-12).
- AC-10: T is 15 min for an interval of 5 min or less and 3 × the interval above that.
  Staleness, day start, incomplete tracking and extra-usage evidence use T; the
  existing 5 min tests are unchanged. At 30 min, a reading 40 min before midnight
  carries the day start and one 100 min before does not. The store merge is unchanged
  (R-13).
- AC-11: The Infrastructure and Presentation suites pass, and the Release app build has
  no warnings. The document validation passes with `--final`, and the demo startup
  smoke passes (Preview gate). The owner's live check in the updated installed app is
  NOT_RUN until after deployment (R-190).
