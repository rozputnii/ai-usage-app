# T-038 verification

Dates: 2026-10-02 through 2026-10-03 (Europe/Lisbon). Primary continuation of Claude's `dbe1db1`, branch
`users/aiu-038-presentation-redesign-1c2839`, base `019836a`.
Evidence applies to the continuation commit containing this record.
Feature acceptance is complete under owner-amended scope version 2 (2026-10-03).
Earlier WIP statements and excluded-setting observations below are historical evidence,
not future requirements. No failed historical check has been relabelled as passing.

## Environment and automated checks

Windows interactive desktop, local unpackaged x64 WinUI app, .NET SDK 10.0.401.
Commands ran in the existing Claude worktree. No provider credentials were imported
and no live sign-in or host trust changes occurred. The initial continuation left host
settings unchanged; the owner-authorized Windows matrix below temporarily changed
display/theme/accessibility preferences and restored them.
The SDK executable was `C:/Users/danii/.dotnet/ai-usage-sdk/dotnet.exe`.
Initial checks ran during the 2026-10-02 continuation; that MSIX completed at 20:32:16 UTC.
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
| AC-04 | PASS | UI-01 through UI-10, font gallery and W-06 establish surface/state reachability, including signed-out H9 and S10c/S10d. Native window/tray contours are retained under the owner amendment resolving PD-038-02. |
| AC-05 | PASS | V-01 command/editor/undo regressions plus UI-02 through UI-06. |
| AC-06 | PASS | V-01 tray rules and UI-07 actual miniature/open-account interaction. |
| AC-07 | PASS | V-01 reads Tokens.xaml, computes composited pill contrast and truncates ratios to two decimals; source review confirms token-derived paints. |
| AC-08 | PASS (amended scope) | UI-01/UI-10 and font smoke show S1/S2 fit the owner's ordinary desktop configuration. W-01 is historical evidence, not a required future matrix. |
| AC-09 | PASS (amended scope) | UI-01/UI-10 and ordinary desktop captures show dark Ledger content. Owner amendment excludes contrast themes and retains native chrome. W-02's former contrast-theme FAIL remains historical evidence below. |
| AC-10 | PASS (amended scope) | UI-02 through UI-07 and W-05 verify ordinary keyboard/editor/undo/tray interactions, focus/tooltips and reorder. Narrator and host accessibility-settings checks are no longer required. |
| AC-11 | PASS | Owner-approved static fonts and OFL licences packaged; internal names, weights, tabular advances and actual rendering checked in F-01 through F-04 below. |
| AC-12 | PASS | V-01 through V-07, UI-09 and F-01 through F-04. Scoped Ledger code, guarded hook, approved font Content declaration, task documents and presentation regressions. |

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
  W-03 verifies disabled-animation behaviour; exact timings are not instrumented.
- Clipped stripe geometry replaces the repeat gradient that dimmed adjacent text.
  Attention text, critical text and neutral-track tokens were minimally adjusted to
  satisfy composited contrast without weakening AC-07.
- Work today hides the clock to retain the full day label and all title actions.
- PD-038-04 uses summary rows: existing detailed sections depend on the old/live
  presentation, so expansion belongs to T-039.
- PD-038-02: cards, history, undo, first-run rows, provider menu, tooltips and appearance
  switches use squircles. Small-radius controls use the explicitly allowed CornerRadius
  treatment; the inline rectangular panel follows the reference sidebar shape. Native
  window/tray contours remain unresolved. No blanket fallback is claimed.

## Approved fonts and continuation after e27dafd

The owner explicitly approved downloading the four named files and continuing on
2026-10-02, resolving PD-038-03. Official licence text was checked before packaging;
all three licences state SIL OFL 1.1. Unmodified files live in `Assets/Fonts`.

| File | Official upstream | Size |
| --- | --- | --- |
| SourceSerif4-Semibold.ttf | [Adobe source-serif release/TTF](https://github.com/adobe-fonts/source-serif/tree/release/TTF) | 272,088 bytes |
| HankenGrotesk-Regular.ttf | [Hanken Grotesk fonts/ttf](https://github.com/marcologous/hanken-grotesk/tree/master/fonts/ttf) | 73,648 bytes |
| HankenGrotesk-SemiBold.ttf | Same Hanken upstream | 73,644 bytes |
| IBMPlexMono-Regular.ttf | [IBM Plex Mono complete/ttf](https://github.com/IBM/plex/tree/master/packages/plex-mono/fonts/complete/ttf) | 173,052 bytes |

Downloads were pinned to these upstream commits, including their accompanying licence:

- Adobe: `80d3f8894c09c937bebfa9011247d2e1c79fd6f4`,
  [TTF source](https://github.com/adobe-fonts/source-serif/blob/80d3f8894c09c937bebfa9011247d2e1c79fd6f4/TTF/SourceSerif4-Semibold.ttf)
  and [LICENSE.md](https://github.com/adobe-fonts/source-serif/blob/80d3f8894c09c937bebfa9011247d2e1c79fd6f4/LICENSE.md).
- Hanken: `eff37d18946b018ad239cf5fd3992db5d19b82a0`,
  [TTF sources](https://github.com/marcologous/hanken-grotesk/tree/eff37d18946b018ad239cf5fd3992db5d19b82a0/fonts/ttf)
  and [OFL.txt](https://github.com/marcologous/hanken-grotesk/blob/eff37d18946b018ad239cf5fd3992db5d19b82a0/OFL.txt).
- IBM: `763c36ef9117782905ae010056dfbe8fd2653a25`,
  [TTF source](https://github.com/IBM/plex/blob/763c36ef9117782905ae010056dfbe8fd2653a25/packages/plex-mono/fonts/complete/ttf/IBMPlexMono-Regular.ttf)
  and [LICENSE.txt](https://github.com/IBM/plex/blob/763c36ef9117782905ae010056dfbe8fd2653a25/LICENSE.txt).

Local licences retain the original bytes under `SourceSerif4-LICENSE.md` (4,491),
`HankenGrotesk-OFL.txt` (4,402), and `IBMPlexMono-LICENSE.txt` (4,456).
No font was installed system-wide.
Folder-local Git attributes preserve upstream licence line endings/trailing spaces;
the whitespace exception applies only to these unmodified third-party licences.

| Font | SHA-256 | Weight / default digit advance |
| --- | --- | --- |
| SourceSerif4-Semibold.ttf | `36db62940cb5728b12b1802476dc7fcf4c6c519a7bdd476ba23a4e555fc4655f` | 600 / 520 |
| HankenGrotesk-Regular.ttf | `0a5c86e907f6d6cba528c3bfb0516b3f46c9524fa22f32f292109f25dc149b32` | 400 / 560 |
| HankenGrotesk-SemiBold.ttf | `19ef1bdde3563a72e3a8387461f9e3eff33e6ae1ab3576e904db82fd99cf4f4a` | 600 / 560 |
| IBMPlexMono-Regular.ttf | `7c6fbddca4b700be918f5f6183d9bd4464fa427fe435f0b480d77fe2bb8c5a43` | 400 / 600 |

| Check | Result |
| --- | --- |
| F-01, OpenType inspection | PASS: TrueType outlines, no variable-font table, internal family names match resources, expected weights, embedding field zero. Each font's ten digits have identical advance widths; Hanken/IBM figures are tabular by default. Local evidence: `font-inspection.json`. |
| F-02, build/regressions | PASS: V-01 repeated, 261/261. Final Debug V-05 repeated, zero warnings/errors. An earlier premature app launch briefly locked build output; app stopped and clean build repeated successfully. |
| F-03, package contents | PASS: Release MSIX 2026.10.203.0 and unpackaged output contain all four TTFs and all three licences byte-for-byte. Local evidence: `font-package-verification.json`. |
| F-04, actual Windows UI | PASS at current desktop setting: Compact S1/S2 fit, complete figures remain visible, Comfortable scrolls, settings cap editor focuses its input and Escape cancels, inline delete confirmation focuses Cancel and Escape cancels, tray fonts/focus/tooltips render. No system-scale/theme/Narrator verdict inferred. |

F-03 used V-06's command with version `2026.10.203.0`, completed at
2026-10-02 21:55:00 UTC. Package SHA-256:
`A713DF491F4C03DF04A31277E02FF9F47C2130F7225229328FDF62AAA43C7E43`.
The same missing-symbol-tool packaging warning applies; no installation/signing performed.

F-04 captures under the same ignored UI directory: `fonts-s1.jpg`, `fonts-s2.jpg`,
`fonts-settings-cap.jpg`, `fonts-comfortable-settings.jpg`,
`fonts-inline-confirm.jpg`, `fonts-tray.jpg`, `fonts-tray-tooltip.jpg`.
Static regular/semibold resources are explicit, including buttons, tooltips and menus.
The footer now measures natural text widths and wraps the reset/action when necessary,
matching the reference flex-wrap rule instead of truncating decision figures.
Long tooltip text wraps within its maximum width.

After font commit `aa416aa`, scrolled the complete visible Compact/Used state gallery
from A1 through R8 at the current desktop setting. No new visible layout defect was
found in these cards; checked Left mode again on the day-off/rush/used-up/pool tail.
Captures: `fonts-gallery-01.jpg` through `fonts-gallery-07.jpg` and
`fonts-gallery-left-rush.jpg`. This is additional native UI evidence, not the full
side-by-side reference or display-scale acceptance matrix.

## Owner-authorized Windows matrix after 1e89073

Date: 2026-10-02, actual Windows desktop, 1920 x 1200, unpackaged Debug Ledger.
The owner explicitly allowed temporary display/theme/Narrator/animation changes and
restoration. Settings were changed through Windows Settings, not content-scale simulation.
Original and restored snapshots are in the ignored `windows-settings-original.json`
and `windows-settings-restored.json`; final restoration check was 22:55:20 UTC.
Original values restored: 125% recommended scale, dark system/app mode, contrast None,
animation effects on, Narrator off. Resolution and security/privacy settings were unchanged.

| Check | Result and actual evidence |
| --- | --- |
| W-01, display scale | PASS: all nine S1/S2 cards fit at actual 100% and 150%; thin marks remain distinguishable at 150%. At 200%, the original 600-DIP outer height exceeded the monitor work area and clipped the last card. Initial sizing now clamps size and position to the work area; both modes scroll to the complete last footer. Captures: `scale-100-s1.jpg`, `scale-100-s2.jpg`, `scale-150-s1.jpg`, `scale-150-s2.jpg`, `scale-200-before.jpg`, `scale-200-top.jpg`, `scale-200-bottom.jpg`, `scale-200-s2.jpg`. |
| W-02, light/contrast | Light PASS: main, settings and history stay dark. Night sky contrast initially changed popup text backgrounds, checkbox and scrollbars; Ledger-owned template paints now use Ledger tokens, and the two-state checkbox has an explicit template with native CheckBox semantics. Rechecked menu selection and panel scrolling. Native outer frame becomes purple and native caption buttons yellow/black despite AppWindow colour settings: AC-09 FAIL, owner decision pending. Captures: `windows-light-main.jpg`, `windows-light-panel-history.jpg`, `contrast-before.jpg`, `contrast-main.jpg`, `contrast-menu-checked.jpg`, `contrast-panel-scroll.jpg`. |
| W-03, reduced motion | PASS: actual Animation effects off; settings opens without motion and synthetic sign-in waiting uses static waiting text. Restored on. Capture: `animations-off-waiting.jpg`. No millisecond timing claim. |
| W-04, Narrator | PASS: actual Narrator running with the existing Microsoft David voice. Speech recap displays the strings spoken for a card with expired sign-in, separate today cells, Show values: left, tray account rows and a used-up limit. Names carry state, figures and reset information; arrows expose the focused cell tooltip. Captures: `narrator-card.jpg`, `narrator-cells-controls-tray.jpg`. This is the real Narrator speech recap, not an inference from UI Automation names; no claim of separately recorded/listened-to audio. Narrator restored off. |
| W-05, keyboard regressions | PASS: actual Alt+Down originally moved the card but lost focus, so Alt+Up failed. Added a regression and observed RED (1 failed / 262), then GREEN (262/262) after emitting focus after the source update and queuing focus until the grid rebuild completes. Final Debug live consecutive Alt+Down/Alt+Up returns the card to its original position with visible focus (`reorder-focus-restored.jpg`). Used previously acquired the Left button's name in Left mode; its label is now fixed and final live UIA shows distinct Used/Left names. F5 smoke caused no error; the synthetic fixture intentionally keeps its fixed time. |
| W-06, reference reachability | PASS: opened the rendered imported Surfaces page and inspected S1-S13 against the corresponding native surfaces from this and earlier captures. Native day-off and Work today trays are reached (`s10c-day-off.jpg`, `s10d-work-today.jpg`); H9 is visible after Show signed-out accounts (`gallery-h9.jpg`). The full Provider States DOM and earlier native A1-R8 gallery were inspected, with the reference A1-A4 visually displayed. Not an exhaustive per-card pixel comparison. PD-038-02 remains open: native popup/panel/control/tray corners have not all been replaced with the approved squircle treatment or justified by the specified fallback evidence. |

Microsoft documents that Windows contrast settings can override title-bar colours in
[title bar customization](https://learn.microsoft.com/en-us/windows/apps/develop/title-bar?tabs=winui3).
The observed native-chrome exception is therefore recorded as PD-038-05, not silently
excluded from AC-09. Narrator's Speech recap is documented in the
[Narrator guide](https://support.microsoft.com/en-us/accessibility/windows/narrator/complete-guide-to-narrator).
Temporary reference browser tabs and the localhost reference server were closed afterward.

Final automated checks for this continuation: Presentation 262/262; Debug build zero
warnings/errors. Infrastructure 421/421 and validator regressions 80/80 remain applicable
because those areas did not change. Final document validation, diff review and unsigned
package evidence are recorded below.

Package for `dbec191`: `Build-Package.ps1 -MsixVersion 2026.10.205.0 -NoRestore
-OutputDirectory .ai-usage-local/AIU-038/packages`, PASS at 22:55:03 UTC.
Unsigned MSIX SHA-256:
`5B77E75F8A8930FA5385C56742178223C66B9499238C082F95AF204787399F4F`.
All four fonts and three licences match the source bytes in this MSIX and Debug output.
The previously recorded missing-symbol-tool warning is unchanged; no package was installed.
Final document validator: PASS, `valid: true`, no diagnostics. Final `git diff --check`:
PASS. Primary integrated review checked the source update/focus ordering, work-area
clamp, token-only paints and retained CheckBox automation semantics; acceptance gaps
remain explicitly recorded above.

### Corner continuation after dbec191

W-07, PASS at actual Windows 150%: provider menu, two-state appearance switches and
data tooltips now draw the shared squircle surface, preserving padding, focus and
CheckBox/Button semantics. Captures: `corners-menu-150.jpg`, `corners-switches-150.jpg`,
`corners-tooltip-150.jpg`. Native Windows menu dismissal and the switch's off/on state
were exercised. Title/card-action hints now use the same tooltip factory, avoiding
default system tooltip styling; the final Debug title tooltip was observed at 125%.
Debug build: PASS, zero warnings/errors. View-model tests remain 262/262; these later
changes affect WinUI rendering only. No performance failure or corner fallback is claimed.

Scale restored to 125% again; the restoration snapshot is dated 2026-10-02 23:01:01 UTC
(2026-10-03 locally). Dark theme, contrast None, animations on and Narrator off remain
restored. Settings was minimized after verification. Remaining PD-038-02 work concerns
native window/tray contours and must be considered with the pending PD-038-05 chrome
decision; it does not justify declaring the feature accepted.

Final unsigned Release package `2026.10.206.0`: PASS at 2026-10-02 23:01:41 UTC,
same packaging command with the new version. SHA-256:
`AEEB75DE24E4ED7FD8C630FC612370C49AA9232C0C4851B03B1E1DFF150A0B40`.
Font/licence byte comparison repeated against this package and Debug output: PASS.
The same missing-symbol-tool warning applies. No installation, signing or publication.
Primary review checked geometry ownership, transparent outer presenters, unchanged
layout/automation semantics and the remaining native-chrome boundary.
Document validator and `git diff --check` after this continuation: PASS.

## Review and remaining boundaries

Primary reviewed the integrated branch against the spec and fixed the defects above.
No subagents were used. Independent security review is not triggered: changed
delete/sign-in paths operate only on synthetic in-memory demo data.
No main merge or push occurred; the owner requested continuation/commit in this worktree.
This branch has not been rebased onto T-042's later main commits.

Live provider operation, packaged installation/update, product switch and release
approval are NOT_RUN and outside this change. Before the owner amendment below,
native contrast chrome (PD-038-05) and remaining corner fidelity (PD-038-02) prevented
closure; these former gates are now removed from the scope.

## Owner amendment and closure, 2026-10-03

The owner explicitly rejected further Narrator/screen-reader, contrast-theme,
extreme zoom/DPI and unusual-display scope for this personal app, and asked to avoid
additional complexity. Scope version 2 therefore retains native window/tray chrome
and contours, removes those verification gates and resolves PD-038-02/PD-038-05.
Earlier FAIL/NOT_RUN observations are preserved as history, not changed into claims
that the excluded behaviour was implemented or newly tested.

Existing ordinary-use evidence satisfies the amended acceptance. Code is unchanged
since ba6cd01; no repeat application build, runtime matrix or host-setting change was
performed for this documentation-only amendment. AGENTS.md and the verification
workflow carry the owner's future-work instruction. Primary reviewed the amendment
against the owner's request; document validation and whitespace checks are recorded
with the closure commit. Live wiring/product switch remains T-039 and is not started.

Closure checks: document validator PASS (`valid: true`, no diagnostics);
`git diff --check` PASS. No runtime checks were repeated.

## Main integration verification, 2026-10-03

Owner requested integration of users/aiu-038-presentation-redesign-1c2839 into main.
Merged branch tip 8a53324 with main 04c2900 without conflicts. Primary integration
inspection confirmed the guarded demo composition, font content declaration and
preservation of T-037/042; this is not a new independent review. No application
source was changed while merging. Earlier branch-only statements above are historical.

| Check on the combined tree | Result |
| --- | --- |
| Infrastructure Release suite, no restore | PASS, 530/530 |
| Presentation Release suite, no restore | PASS, 262/262 |
| ProjectValidation regression suite, no restore | PASS, 80/80 |
| Document validator | PASS, valid true, no diagnostics |
| Unsigned Release MSIX 2026.10.305.0, Build-Package.ps1 -NoRestore | PASS |
| Integrated diff and whitespace check | PASS |

An initial simultaneous Infrastructure/Presentation build collided on the shared
Core output (CS2012). Presentation completed; Infrastructure was rerun sequentially
and passed. This was a build-output collision, not a failing test.
The package toolchain retains its known missing mspdbcmf.exe warning; no symbols
package was generated. Package SHA-256:
`E3468BCD7BAB08E4AFA86AB57C90AE546C6675BB41C28BDCE78E6D702EAA0CA0`.
Output is ignored under .ai-usage-local/AIU-038-main-integration/packages.

Interactive smoke was not repeated during integration; existing ordinary-desktop
branch evidence above is retained for the unchanged Ledger UI. Live providers,
installation/update and product switching were not exercised. No host settings,
credentials or trust were changed. T-038 is done; T-039 is ready for owner
selection, not started. T-040 remains dependent on T-039.

## Execution ledger

Collapsed from tasks.md on 2026-10-09 (OD-19); the full plan is in Git history at 00007ca.

- T-038.1 Contract, demo source and platform spikes: done; commits not recorded per task (implementation through ba6cd01 and dbec191); review primary check; checks C5 (V-01, 261/261); grant owner spec approval, 2026-10-02.
- T-038.2 Tokens, styles and fonts: done; commits aa416aa (fonts); review primary check; checks C5, Debug app build, C9 (MSIX 2026.10.203.0), font checks F-01 to F-04; grant owner approval of the spec and the four named font files, 2026-10-02.
- T-038.3 Card view models and controls: done; commits not recorded per task (corner work after dbec191); review primary check; checks C5 (262/262), Debug app build, display-scale matrix W-01, corner check W-07; grant owner-authorized Windows matrix, 2026-10-02, and owner amendment (scope version 2), 2026-10-03.
- T-038.4 Main window, tray and interactions: done; commits not recorded per task; review primary check; checks C5 (262/262 after the W-05 red/green), interactive checks UI-01 to UI-10 and W-02 to W-06; grant owner-authorized Windows matrix, 2026-10-02, and owner amendment (scope version 2), 2026-10-03.
- T-038.5 Verification and record: done; commits ea4aa1c (closure), 58d94b9 (handoff), branch tip 8a53324 merged with main 04c2900; review primary integrated review (no independent review triggered); checks C4 (530/530 on the merged tree), C5 (262/262), C1 (80/80), C2, C6, Debug app build, C9 (2026.10.206.0; 2026.10.305.0 on the merged tree), F-03 byte checks; grant owner amendment (scope version 2) and owner request to merge into main, 2026-10-03.
