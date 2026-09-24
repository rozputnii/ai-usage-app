# AIU-030 verification

Date: 2026-09-24. Windows 11 Pro 10.0.26200 x64, .NET SDK 10.0.401 (global.json), display
scale 125 %. Base `e06e802`; implementation save point `1ba8559`, completed in the commit that
adds this record. An owner's packaged debug instance was running during the checks and was not
touched; every agent-launched instance was a separate unpackaged process with an isolated state
directory under the session scratch folder.

## Results by acceptance criterion

| AC | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | Demo and empty-product smoke assert the header IDs `RefreshAllButton`, `AddAccountButton`, `SettingsButton` and the absence of every former tab ID; `BackButton` is absent on the usage view and present on the others. Captured demo window inspected at 1100 and 600 effective px: at 600 px Refresh all shrinks to its icon and actions stay on one row. |
| AC-02 | PASS | Smoke `navigation`/`theme`: the gear shows `ThemeNote`, `HistoryEnabledSwitch`, `UpdateStatus` and `StatusBuild` in one view; Back returns. Interactive demo: System status "Open updates" scrolled the view to the Updates section; with focus in the window Esc returned to usage. A duplicate "Updates" heading seen in the capture was removed. |
| AC-03 | PASS | Interactive demo with absolute mouse input: hover opened the menu, moving into it kept it open, leaving closed it within the 400 ms delay; hover followed by a click kept it open as a standard flyout after the pointer left; UIA invoke opened it with focus on the first provider. Two defects found and fixed during this check: Button marked pointer-entered handled (now subscribed with handled events), and a hover-opened transient flyout swallowed the click (now passes input to the button and reopens as standard). |
| AC-04 | PASS | Presentation `ConnectionTests` cover one-click start, waiting text, approve without confirmation, deny/expire notes with Try again, neutral cancel, quota-unavailable success, duplicate note, manual code validation and clearing, replacement by a second provider click and the security block. Interactive demo: Claude from the menu showed the inline strip with "Enter a code instead" and the demo outcomes; Approve added "Claude account 2" (5 to 6 accounts) and closed the strip with no dialog. |
| AC-05 | PASS | `LiveAdapterTests.OccupiedProviderExplainsLimitWithoutClaimingIdentityVerification`: with a connected live Codex slot the menu entry reads Connected, is disabled and starts no browser launch. Row and detail reconnect use the same inline flow (`AccountTests` F06). |
| AC-06 | PASS | `AccountTests.RowSignOutIsImmediateKeepsIdentityAndHistoryAndHidesByDefault`: no dialog request, Disconnecting then NotConnected, label kept, identical 7-day history points before and after, hidden by default and offering Sign in when shown. `DetailSignOutIsImmediateToo` covers detail. Interactive demo: the icon showed tooltip "Sign out of Claude account 2"; one click removed the panel, no dialog appeared and the live region read "Signed out of Claude account 2. History is kept." |
| AC-07 | PASS | Presentation 159/159, Infrastructure 330/330, ProjectValidation tests 80/80. Windows smoke `ShellLaunchesNavigatesAndExits` rows launch, navigation, theme, repeated-exit and capabilities: demo 5/5, empty isolated product state 5/5. |

## Commands

```powershell
dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo
dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo
dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo
dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json
dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Debug -p:Platform=x64 -p:WindowsPackageType=None --no-restore
./tools/windows/Build-Package.ps1 -MsixVersion 2026.9.2451.0 -NoRestore
dotnet run --project tests/windows/AiUsage.Windows.Tests -c Release --no-restore -- -noLogo -method AiUsage.Windows.Tests.ShellSmoke.ShellLaunchesNavigatesAndExits
git diff --check
```

The smoke ran twice with `AIU_SMOKE_EXE` set to the unpackaged Debug executable and a fresh
`AIU_SMOKE_EVIDENCE_DIRECTORY`: once with `AIU_SMOKE_MODE=demo`, once in product mode with an
empty `AIU_DEVELOPMENT_STATE_DIRECTORY`. The package build produced unsigned-validation-only
`AiUsage.Windows_2026.9.2451.0_x64.msix`, SHA-256
`04166CF187F7014E7FBB6A11EEBBB08211F38F3424ADE6FA32352DC08886AB7B`; it was not installed.

The first smoke run failed at opening account detail with a pointer click (FlaUI falls back to
the element centre because the link reports no clickable point); two manual real-mouse clicks on
the same label opened detail. The smoke now uses the Invoke pattern for its view entries, as it
already did for most buttons, and passed.

## Not run

- NOT_RUN: a live provider sign-in and sign-out through the new menu and panel button. The
  connection flows themselves are unchanged and remain covered by their provider records; this
  presentation change needs the owner's own account check.
- NOT_RUN: smoke rows `tray-exit` and `close-to-tray`. The tray icon is located by its
  "AI Usage" name, so with the owner's instance running these rows could have acted on it. Tray
  code is unchanged; only the window-lookup marker used by these rows changed.
- NOT_RUN: packaged install, update and recovery harnesses. `RecoverySmoke` and `UpdateSmoke`
  now find settings through the gear or, for older packages, the former Settings tab.
- NOT_RUN: screen-reader acceptance beyond automation names and the live-region announcement.
