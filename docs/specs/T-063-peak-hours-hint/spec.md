---
id: T-063
type: feature
status: implemented
goal: G-003
scope_version: 1
approval_basis: owner decision, 2026-10-10, in conversation. The owner asked to research whether the app can tell when provider peak hours apply, then chose a schedule-only informational hint for every account, without naming a provider, in the window title row and the tray header, and a short monthly review of the schedule by an agent inside other work. Recorded as R-221.
---

# Peak hours hint

## Problem

Anthropic announced that during weekday peak hours Claude's five-hour session limits drain
faster. No provider exposes whether peak applies at a given moment: the quota responses carry
no peak field and providers publish no load by hour. The owner wants a plain hint that work in
this window may be slower or use more of a limit.

## Requirements

- **R-01 Schedule.** Peak is weekdays 05:00 to 11:00 Pacific time (America/Los_Angeles,
  with its daylight-saving changes); the weekday is the Pacific one. The window and its
  evidence are recorded in [peak hours](../../providers/peak-hours.md).
- **R-02 Window chip.** Inside the window the title row shows an attention-coloured pill after
  the day group: a dot and "Peak · until HH:mm" in the owner's local time. Outside it nothing
  is shown. The pill names no provider.
- **R-03 Tray chip.** The tray header shows the same pill at the right of "AI Usage".
- **R-04 Tooltip.** Both pills share the tooltip "Peak hours", "Weekdays 05:00–11:00 Pacific
  (HH:mm–HH:mm here)", "Requests may be slower or use more of a limit" and "An announced
  schedule, not your account's data". The spoken name is "Peak hours until HH:mm".
- **R-05 Updates.** The hint follows the snapshot's local time, so it appears and disappears
  within a minute of the window edges. It is replaced only when its text changes.
- **R-06 Monthly review.** The schedule is reviewed at most monthly by an agent as a short
  research pass inside other work, never by a trigger (R-221).

Variant A (attention pill shown only during peak), owner, 2026-10-10; the owner removed the
provider name from the label.

## Acceptance criteria

- AC-01: Window tests cover both edges of the window, the caller's offset, weekends, the
  Pacific weekday seen from UTC+11 and UTC+3, and the weeks between the European and US
  daylight-saving changes.
- AC-02: The hint text, tooltip and spoken name match R-02 and R-04, and no hint exists
  outside the window.
- AC-03: The window and tray view models show the hint inside the window, raise no change
  while its text stays the same and clear it at the end.
- AC-04: The app builds without warnings; the presentation suite, the launch smoke, the
  document validation and the diff check pass.

## Out of scope

- Detecting peak from account data, response times or quota burn rates.
- Per-provider or per-plan windows, notifications and settings for the hint.
