---
id: T-060
type: feature
status: implemented
goal: G-003
scope_version: 1
approval_basis: owner decision, 2026-10-10 (fix confirmed defects of the four classes found in the T-056/T-057/T-058 run)
---
# Fragile smoke-harness input

## Problem
- Current: the T-058 fixes made `LedgerSmoke.Focus` and the activation clicks robust, but the rest of the
  ordinary (non-Explicit) smoke classes still use the old input patterns. A sweep on `17c79c7` found, by code:
  `CardEditingSmoke.Focus` and `RefreshIntervalSmoke.Focus` click a fixed title-row point (now the T-052 Refresh
  button) when Windows refuses the foreground request; about twenty mouse clicks and hovers are not guarded by
  `DesktopTestEnvironment.RequireOwnedPoint`; six fixed sleeps wait for app state (a context menu, a resize, a
  relayout); several tests hold a window or card element across a UI change; `LedgerActivationSmoke`'s `Named`
  helper falls back to a desktop-wide name search, so `Named("Save")` could invoke another app's button; the wait
  "History did not open" at `LedgerSmoke.cs:302` is already true before the click, so it can never fail; and many
  element lookups use a name although the element has an AutomationId.
- Expected: every ordinary smoke focuses with the Alt-tap helper, checks that the app owns each point it clicks or
  hovers, waits on conditions instead of fixed sleeps, re-finds elements after UI changes, scopes searches to the
  app's own windows, has waits that can fail, and finds elements by AutomationId where one exists (verification.md,
  Desktop smokes).
- Unchanged: the app, the tests' assertions and intent, the case count, deliberate input pacing (short sleeps
  between injected events, settle delays before a negative check), and the frozen Sandbox audit suite (`Audit*.cs`)
  and the Explicit Sandbox-guest smokes (`ShellSmoke`, `AutomaticRefreshSmoke`, `RecoverySmoke`, `UpdateSmoke`).

## Requirements
- R-01: One shared focus helper (`SmokeKit.Focus`, moved from `LedgerSmoke.Focus`) replaces the fixed-point click
  fallbacks in `CardEditingSmoke` and `RefreshIntervalSmoke`.
- R-02: One shared owned-click helper (moved from `LedgerSmoke.ClickOwned`, with right-click and hover variants)
  guards every mouse click, right-click and hover in the ordinary smoke classes; a click on Explorer's tray icon
  checks Explorer's ownership of the point. Where a test does not exercise mouse input, it invokes by
  AutomationId instead.
- R-03: The fixed sleeps that wait for app state (`CardEditingSmoke.cs:32`, `:75`; `RefreshIntervalSmoke.cs:80`,
  `:82`; `LedgerWindowSizeSmoke.cs:56`, `:100`) become condition waits.
- R-04: Elements held across a UI change are re-found per query (`LedgerSmoke.cs:180-186`, `:200-209`;
  `LedgerActivationSmoke.cs:66-103`, `:297-312`), and `LedgerActivationSmoke`'s name fallback searches only the
  app's own windows.
- R-05: The History wait at `LedgerSmoke.cs:302` matches the history panel's own name, and the hidden-window check
  at `LedgerActivationSmoke.cs:310` uses a fresh window.
- R-06: Element lookups listed in the sweep use the AutomationId (`RefreshButton`, `WorkTodayButton`, `UsedButton`,
  `LeftButton`, `DemoButton`, `AddButton`, `MoreButton`, `ConfirmButton`, `CancelButton`, `RefreshBox`,
  `RenameBox`, `HistoryButton`, `Grip`). The "Settings" lookups keep the name, because `SettingsButton` is not unique.

## Acceptance criteria
- AC-01: No ordinary smoke class has a fixed-offset focus click, an unguarded mouse click or hover, a state-waiting
  fixed sleep, a desktop-wide name fallback, or an element lookup by name where the sweep listed an AutomationId
  (checked by a grep listed in the verification record).
- AC-02: A wait that cannot fail is corrected: with the history panel kept closed, the `LedgerSmoke.cs:302` wait
  fails (shown once by a temporary local change, not committed), and with the fix the test passes.
- AC-03: Every ordinary smoke class passes twice in a row under the desktop lock against the C7 build; the case
  count is unchanged; `tools/windows/Test-Erosion.ps1` lists nothing unexplained.
- AC-04: C8 passes under the desktop lock; C6 and C2 pass.

## Out of scope
- The frozen Sandbox audit suite and the Explicit Sandbox-guest smokes: they can run only in a Sandbox guest, which
  needs the owner's authorization ("Questions for the owner").
- The drag target points in `CardEditingSmoke.cs:133-148` (pointer capture probably protects them; not confirmed)
  and the element held at `LedgerSmoke.cs:96-98` and `:123-124` (not confirmed).
- Stale assertions against changed UI (the sibling stale-smoke item).
