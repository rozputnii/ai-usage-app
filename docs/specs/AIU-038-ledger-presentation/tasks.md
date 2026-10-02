---
id: AIU-038
schema_version: 1
---

# Redesigned presentation plan

Sequential primary work on branch `users/aiu-038-presentation-redesign-1c2839` in the
existing Claude worktree. Preserve the selected task's no-main integration instruction.
No Core, Infrastructure, tools or live-adapter edits. The owner approved the spec on
2026-10-02 and requested Codex continuation in this worktree with a commit.

### T-01 - Contract, demo source and platform spikes
- status: done
- depends_on: []
- acceptance: AC-01, AC-03, AC-12
- evidence: docs/specs/AIU-038-ledger-presentation/verification.md

- [x] Contract, boundary tests, synthetic scenarios and reference-figure tests.
- [x] Guarded startup, working squircle layout and geometry-based hatch rendering.
Font-loading verification is tracked in T-02.

### T-02 - Tokens, styles and fonts
- status: done
- depends_on: [T-01]
- acceptance: AC-07, AC-11
- evidence: docs/specs/AIU-038-ledger-presentation/verification.md

- [x] Tokens, both densities and composited-token contrast regression.
- [x] Owner approved the four named static TTF files on 2026-10-02. Fonts and licences
  are packaged; internal names/weights, tabular figures and actual rendering verified.

### T-03 - Card view models and controls
- status: done
- depends_on: [T-01, T-02]
- acceptance: AC-02, AC-03
- evidence: docs/specs/AIU-038-ledger-presentation/verification.md

- [x] State/geometry/figure tests, cards, bars, today strips and spoken names.
- [x] Correct nullable binding visibility, squircle padding and footer truncation.
- [x] With real fonts, preserve complete footer figures using the reference wrapping flow.
- [x] Actual S1/S2 display-scale matrix at 100%, 150% and 200%; fix high-DPI clipping.
- [x] Squircle provider menu, tooltips and appearance switches, checked at actual 150%.
- [x] Owner amendment 2026-10-03 retains native window/tray contours; no custom-chrome work.

### T-04 - Main window, tray and interactions
- status: done
- depends_on: [T-03]
- acceptance: AC-04, AC-05, AC-06, AC-10
- evidence: docs/specs/AIU-038-ledger-presentation/verification.md

- [x] Window, inline history/settings, editors, undo, sign-in states and tray miniature.
- [x] Fix Work today edit retention, settings Escape, preference notification,
  delete/undo lifetime, focus return, keyboard detail navigation and first-show tray DPI.
- [x] Actual local smoke of main surfaces and selected keyboard interactions.
- [x] Reference surface reachability, actual Narrator speech recap and animations-off checks.
- [x] Restore focus after Alt+Up/Down reorder; preserve the Used button's accessible name.
- [x] Owner amendment removes contrast-theme scope and resolves PD-038-05/PD-038-02.

### T-05 - Verification and record
- status: done
- depends_on: [T-04]
- acceptance: AC-01, AC-02, AC-03, AC-04, AC-05, AC-06, AC-07, AC-08, AC-09, AC-10, AC-11, AC-12
- evidence: docs/specs/AIU-038-ledger-presentation/verification.md

- [x] Regression suites, Debug build, unsigned Release MSIX, product/plain-demo smoke.
- [x] Integrated primary review and explicit per-AC evidence/limitations.
- [x] Font completion and byte-for-byte font/licence checks in unpackaged/MSIX output.
- [x] Owner-authorized Windows matrix and restoration of original settings.
- [x] Feature closure under amended ordinary-desktop scope version 2.

## Completion

Completed under owner-amended scope version 2 on 2026-10-03 in the same Claude
worktree, branch `users/aiu-038-presentation-redesign-1c2839`, base `019836a`.
Preserved Claude's work; implementation continues through `ba6cd01` and `dbec191`.
Fonts were completed in `aa416aa`. No pending workers or unintegrated worker artifacts.

Evidence: Presentation 262/262, Infrastructure 421/421, validator regressions 80/80,
Debug zero warnings/errors, unsigned Release MSIX 2026.10.206.0, packaged font/licence
byte checks and actual ordinary Windows interactions. See verification.md for observed
results and historical checks. Host settings were restored before the owner amendment.

The owner explicitly removed Narrator, contrast-theme and unusual zoom/display work
and rejected extra native-chrome complexity. Retain the implemented native window/tray
contours; PD-038-02 and PD-038-05 no longer block the amended scope. Do not repeat those
excluded checks or change host settings unless the owner specifically requests it again.
No new application tests or UI matrix are needed for this documentation-only amendment.

AIU-038 has no remaining action. AIU-039 live wiring/product switch is separate work
and is not started by this closure. No main merge, push, package install or release.
