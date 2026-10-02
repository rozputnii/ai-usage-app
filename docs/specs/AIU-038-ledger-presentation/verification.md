# AIU-038 verification

Date: 2026-10-02. Primary continuation of Claude's `dbe1db1`, branch
`users/aiu-038-presentation-redesign-1c2839`, base `019836a`.
Evidence applies to the continuation commit containing this record.
Feature acceptance remains open; this is a verified WIP save point.

## Environment and automated checks

Windows interactive desktop, local unpackaged x64 WinUI app, .NET SDK 10.0.401.
Commands ran in the existing Claude worktree. No provider credentials were imported,
no live sign-in occurred, and no host display, accessibility or trust settings changed.
The SDK executable was `C:/Users/danii/.dotnet/ai-usage-sdk/dotnet.exe`.
Checks ran during the 2026-10-02 continuation; final MSIX completed at 20:32:16 UTC.
Earlier regression results remain applicable: subsequent changes were confined to
WinUI controls and documentation.

| Check | Command / observation | Result |
| --- | --- | --- |
| V-01 | `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS, 261/261 |
| V-02 | `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo` | PASS, 421/421 on this branch |
| V-03 | `dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo` | PASS, 80/80 |
| V-04 | `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json` | PASS, no diagnostics |
| V-05 | `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Debug -p:Platform=x64 -p:WindowsPackageType=None --no-restore` | PASS, zero warnings/errors |
| V-06 | `./tools/windows/Build-Package.ps1 -MsixVersion 2026.10.202.0 -NoRestore -OutputDirectory .ai-usage-local/AIU-038/packages` | PASS, Release unsigned validation MSIX |
| V-07 | `git diff --check` and integrated diff/scope review | PASS |

V-06 SHA-256:
`1DC36DDAB1248FCA52EB9766B793307202CE066A56CD1D09AB4133D18771E3F9`.
The packaging toolchain reports missing `mspdbcmf.exe`, so it does not create a symbols
package. This is not an owned-code warning. No package was signed or installed.
Validator assets initially failed with NETSDK1004; an offline restore from an empty
local feed and the existing package cache resolved it. No network restore was enabled.

## Actual interactive evidence

The app was driven through its real Windows UI. Captures live in the ignored
`.ai-usage-local/AIU-038/ui/` directory. They are local evidence, not repository assets.
Some captures precede the final cap-hint wrapping and tray initial-focus adjustments;
they are not represented as a complete final visual audit.

| Check | Observed result |
| --- | --- |
| UI-01 | PASS: S1 nine-card grid and S2 Left mode fit the default Compact window without scrolling at the current desktop setting. Used/Left changes fills and figures. |
| UI-02 | PASS: Ctrl+, opens inline settings; cards reflow to one column with scrolling; Escape closes the panel. |
| UI-03 | PASS: visible keyboard focus; Enter opens history, arrows change its selected day and Escape closes it. Focus restoration also has V-01 coverage. |
| UI-04 | PASS: S12 day-off strips are neutral; Work today restores work-day colours/full day text; Ctrl+Z restores day off. No empty green pills remain. Capture: `s12-undo.png`. |
| UI-05 | PASS: first run lists four providers. Synthetic Codex sign-in displays waiting progress then adds cards. F2 rename updates both account cards. Capture: `s6-first-run.png`. |
| UI-06 | PASS: invalid cap text remains in the editor with an error; Escape cancels. Hint truncation found here was fixed with wrapping in V-05/V-06. Pre-wrapping capture: `s3-invalid-cap.png`. |
| UI-07 | PASS: last-work-day scenario shows rush, extra usage and expired sign-in. Tray shows all four accounts after the first-show DPI sizing fix, with rush/$/error marks. Tab, Down and Enter open/focus the selected main-window account. Captures: `s10b-window.png`, `s10b-tray.jpg`. |
| UI-08 | PASS: final Debug build opens the state gallery with scrolling and meaningful accessibility-tree names. Demo menu reaches failed sign-in with Try again. Captures: `states-final.jpg`, `s7-failed-final.jpg`. |
| UI-09 | PASS: final product startup with a new empty `AIU_DEVELOPMENT_STATE_DIRECTORY` opens the existing no-accounts view. Plain `--demo` opens the existing sample-data view. Captures: `product-empty-final.jpg`, `plain-demo-final.jpg`. |
| UI-10 | PASS: final Debug tray receives visible first-row focus and a tooltip immediately on first show; Escape hides it. Final S1 grid remains fully visible. Captures: `tray-focus-final.jpg`, `s1-final.jpg`. |

Current-display observations do not establish 100%, 150% or 200% acceptance.
The desktop's AppliedDPI registry value was 120; no exact monitor-scale matrix was run.
Accessibility-tree inspection does not establish an actual Narrator reading.

## Acceptance verdicts

| Criterion | Verdict | Evidence and limit |
| --- | --- | --- |
| AC-01 | PASS | V-01 contract/boundary tests and primary source review; no Core/Infrastructure dependency in Ledger features. |
| AC-02 | PASS | V-01 drawing/state tests plus gallery coverage in Used/Left and Compact/Comfortable; no-cap is reached by removing a cap. |
| AC-03 | PASS | V-01 explicit reference positions/figures and UI-01. |
| AC-04 | NOT_RUN | Scenarios implemented and major flows exercised; exhaustive side-by-side S1-S13/reference-state audit remains open. |
| AC-05 | PASS | V-01 command/editor/undo regressions plus UI-02 through UI-06. |
| AC-06 | PASS | V-01 tray rules and UI-07 actual miniature/open-account interaction. |
| AC-07 | PASS | V-01 reads Tokens.xaml, computes composited pill contrast and truncates ratios to two decimals; source review confirms token-derived paints. |
| AC-08 | NOT_RUN | Owner-controlled 100/150/200% display matrix required by spec section 11. |
| AC-09 | NOT_RUN | Owner-controlled Windows light app mode and contrast theme checks. |
| AC-10 | NOT_RUN | Keyboard/focus/tooltip subset passed; full keyboard map, Narrator and Windows animations-off matrix remain unverified. |
| AC-11 | BLOCKED | PD-038-03 per-file download confirmation pending. No reference fonts packaged; fallback rendering is not font acceptance. |
| AC-12 | PASS | V-01 through V-07 and UI-09. Permitted Ledger code, guarded App.xaml.cs hook, task documents and presentation regressions only. |

## Fixes and implementation rulings

- Preserved Claude's final squircle-padding correction and footer truncation.
- Registered Ledger resources inside guarded composition. Removed the temporary
  global App.xaml merge so ordinary product/demo resources are unchanged.
- Regression-first fixes cover Always on top notification, settings-cap Escape,
  Work today preserving sign-outs/order/caps, stale undo after synthetic-data deletion,
  history focus restoration and contrast failures. Added failed-sign-in/retry coverage.
- Replaced nullable x:Bind visibility inputs with booleans after actual Work today UI
  showed empty pills. Wrapped editor hints and queued cap-input focus.
- Tray sizing waits for the real XamlRoot scale; names trim within their column.
  Row/strip keyboard navigation includes rows without strips.
- Width transitions and panel/history/undo/strip fades check Windows animation settings.
  Exact timings and disabled-animation behaviour still need interactive verification.
- Clipped stripe geometry replaces the repeat gradient that dimmed adjacent text.
  Attention text, critical text and neutral-track tokens were minimally adjusted to
  satisfy composited contrast without weakening AC-07.
- Work today hides the clock to retain the full day label and all title actions.
- PD-038-04 uses summary rows: existing detailed sections depend on the old/live
  presentation, so expansion belongs to AIU-039.
- PD-038-02 fidelity remains in the pending visual audit: cards use squircles; several
  native popup/panel/control surfaces retain native corners. Required 150%/performance
  evidence for a blanket fallback has not been obtained.

## Fonts awaiting PD-038-03

Official metadata and licence text were checked on 2026-10-02; all state SIL OFL 1.1.
No font binary has been downloaded or packaged.

| File | Official upstream | Size |
| --- | --- | --- |
| SourceSerif4-Semibold.ttf | [Adobe source-serif release/TTF](https://github.com/adobe-fonts/source-serif/tree/release/TTF) | 272,088 bytes |
| HankenGrotesk-Regular.ttf | [Hanken Grotesk fonts/ttf](https://github.com/marcologous/hanken-grotesk/tree/master/fonts/ttf) | 73,648 bytes |
| HankenGrotesk-SemiBold.ttf | Same Hanken upstream | 73,644 bytes |
| IBMPlexMono-Regular.ttf | [IBM Plex Mono complete/ttf](https://github.com/IBM/plex/tree/master/packages/plex-mono/fonts/complete/ttf) | 173,052 bytes |

Licence sources: Adobe `LICENSE.md` (4,491 bytes), Hanken `OFL.txt` (4,402 bytes),
IBM `LICENSE.txt` (4,456 bytes). Add licences and static TTF content after confirmation,
correct resource paths, verify actual families/weights/tabular figures, rebuild and
repeat font-sensitive checks.

## Review and remaining boundaries

Primary reviewed the integrated branch against the spec and fixed the defects above.
No subagents were used. Independent security review is not triggered: changed
delete/sign-in paths operate only on synthetic in-memory demo data.
No main merge or push occurred; the owner requested continuation/commit in this worktree.
This branch has not been rebased onto AIU-042's later main commits.

Live provider operation, packaged installation/update, product switch and release
approval are NOT_RUN and outside this change. Unfinished fonts and interactive
acceptance prevent AIU-038 closure.
