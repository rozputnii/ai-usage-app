---
id: AIU-032
type: spec
status: implementing
goal: G-003
scope_version: 1
approval_basis: Owner request, 2026-09-24, in the session conversation - remove the light and the contrast theme together with the theme settings, and clean out the leftovers of the interface removed by AIU-030 and AIU-031. Recorded as D-182.
---
# AIU-032 - Dark-only appearance and removed-UI cleanup

## Outcome

The app has one appearance: the designed dark palette. There is no theme choice anywhere, and
code, styles, strings and demo controls that only served removed screens or the removed themes
are gone, so what remains is what the single-window compact view actually uses.

## Scope

- The app always renders the dark palette, including the main window, dialogs, the tray popup
  and the title bar buttons, whatever the Windows app mode is.
- Remove the Light theme and the high-contrast token set, the demo's simulated contrast
  dictionary and its "Windows mode" and "High contrast" simulation controls.
- Remove the App theme section from Settings, the theme preference, its command key and its
  strings. A stored preference file that still carries a theme value loads and keeps it as
  unknown data; no stored format is rewritten.
- The token generator emits one dark dictionary; framework lightweight-styling aliases follow it.
- Remove removed-UI leftovers that nothing reaches any more: the old local History view model
  and its chart control, the Overview variants of the window line, unused bind helpers, styles
  and tokens, the arrow-navigation helper, and strings no code or markup uses.
- Keep behavior that is still reachable: account detail (with its sparkline and window lines),
  provider history, settings sections other than the theme, density, always-on-top, order and
  visibility, reduced motion, keyboard access and automation names.

## Excludes

Changing the dark palette values, the compact usage view, pace colors, provider data,
notifications, stored formats other than no longer writing the theme value, and removing the
account list beside account detail.

## Acceptance criteria

- AC-01: The main window, a dialog and the tray popup render the dark palette while Windows is in
  light app mode; the title bar buttons use the dark colors.
- AC-02: Settings shows no theme section; the demo panel offers no Windows-mode or contrast
  simulation; no theme preference, theme strings, light or contrast dictionaries remain.
- AC-03: A preference file written with a theme value loads, keeps the other preferences and
  round-trips the unknown value.
- AC-04: The listed leftovers are deleted and a scan finds no unreferenced resource string,
  style or bind helper introduced by removed screens.
- AC-05: Deterministic tests, document validation, the unsigned package build and the demo and
  product Windows smoke pass.
