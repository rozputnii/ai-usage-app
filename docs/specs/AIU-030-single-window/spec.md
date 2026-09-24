---
id: AIU-030
type: spec
status: implementing
goal: G-003
scope_version: 1
approval_basis: Owner request, 2026-09-24, in the session conversation - simplify the interface because it has too many steps and transitions; one window without tabs and a separate settings button on the right; no Add account dialog, with a provider list that opens on hover and starts browser sign-in on one click, adding usage without any confirmation; an icon-only exit button on the right of each account usage panel that signs out without confirmation and does not delete history. Remaining layout choices below are derived within that request and recorded as D-180.
---
# AIU-030 - Single-window shell, one-click sign-in and immediate sign-out

## Outcome

The main window is one usage view. There are no navigation tabs: the header holds Back (only
when another view is open), Refresh all, Add account and a Settings icon on the right. Adding an
account takes one hover and one click, sign-in progress stays inside the window, and signing
out is a single click on the account panel.

## Scope

- Remove the top navigation tabs (Overview, Accounts, History, Settings, System Status).
- The Settings icon opens one scrolling settings view in place of the usage view. Its former tabs
  (Appearance, Monitoring and notifications, Data and privacy, Updates) and the former System
  Status page are consecutive sections. A request for a section scrolls to it. The icon, Back
  and Esc return to usage.
- Account detail and provider history stay reachable from the account panel (label and
  History link) without tabs; Back returns along the path taken.
- Remove the Add account dialog. Add account opens a provider menu on pointer hover without
  taking focus, and on click, tap, Enter or Space with focus inside. A hover-opened menu closes
  shortly after the pointer leaves the button and the menu; a click keeps it open.
- One provider click starts that provider's browser sign-in immediately, with no method step.
  An inline strip under the header shows progress, the device code with Copy where the provider
  needs one, "Enter a code instead" where the provider supports a manual code, and Cancel.
  Success shows nothing extra: the account and its usage appear. Denied, expired, failed,
  duplicate and one-account-per-provider outcomes show a note in the same strip with Try again
  or Dismiss.
- First run lists the providers directly; each is the same one-click sign-in.
- Every account panel has an icon-only sign-out button at its right edge. It disconnects at once
  without confirmation. A signed-out panel shown through Show disconnected accounts offers
  Sign in, which reuses the inline sign-in. Account detail sign-out is also unconfirmed.
- CLI import, where its capability exists (the demo), is an item of the Add account menu that
  opens inline; the live product does not list it.

## Preserve

- D-093: sign-out deletes only the application credential and stops monitoring. Label, order,
  history and identity stay for the next sign-in; Delete stored data remains a separate,
  confirmed action. Nothing is revoked at the provider.
- Provider connection flows, credential storage and the one-account-per-provider live slot are
  unchanged. The manual code stays transient and is cleared on submit.
- Exit, factory reset, delete stored data and hide keep their confirmations. Close-to-tray,
  tray menu and recovery takeover are unchanged.

## Excludes

Multi-account per provider, new providers, CLI discovery (AIU-005), history storage (AIU-029),
and any change to provider transport or stored-data formats.

## Acceptance criteria

- AC-01: The main window shows no navigation tabs; the header offers Refresh all, Add account
  and a Settings icon on the right, and Back only while settings, history or account detail is
  shown.
- AC-02: The Settings icon shows every settings section and System status in one view; a
  section request scrolls to it; the icon, Back and Esc return to the usage view.
- AC-03: Hovering Add account opens the provider menu; moving into it keeps it open; leaving
  closes it; clicking the button opens it with keyboard focus inside and it stays open.
- AC-04: One provider click starts browser sign-in with no dialog, method step or confirmation.
  Success adds the account and its usage and closes the strip; cancel is neutral and changes
  nothing; failures leave a scoped note with Try again.
- AC-05: A connected provider in the live single-slot product is listed as Connected and cannot
  start a second sign-in; a signed-out or re-auth account is offered Sign in again.
- AC-06: The panel sign-out icon disconnects immediately without a dialog, announces that
  history is kept, keeps label, order and history, and hides the account by default.
- AC-07: Deterministic presentation tests and the interactive product and demo Windows smoke
  pass against the new shell.
