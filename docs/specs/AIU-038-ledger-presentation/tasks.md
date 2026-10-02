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
- status: in-progress
- depends_on: [T-01, T-02]
- acceptance: AC-02, AC-03
- evidence: docs/specs/AIU-038-ledger-presentation/verification.md

- [x] State/geometry/figure tests, cards, bars, today strips and spoken names.
- [x] Correct nullable binding visibility, squircle padding and footer truncation.
- [x] With real fonts, preserve complete footer figures using the reference wrapping flow.
- [ ] Final font-dependent visual comparison and display-scale matrix.

### T-04 - Main window, tray and interactions
- status: in-progress
- depends_on: [T-03]
- acceptance: AC-04, AC-05, AC-06, AC-10
- evidence: docs/specs/AIU-038-ledger-presentation/verification.md

- [x] Window, inline history/settings, editors, undo, sign-in states and tray miniature.
- [x] Fix Work today edit retention, settings Escape, preference notification,
  delete/undo lifetime, focus return, keyboard detail navigation and first-show tray DPI.
- [x] Actual local smoke of main surfaces and selected keyboard interactions.
- [ ] Complete reference comparison, Narrator and animations-off acceptance.

### T-05 - Verification and record
- status: in-progress
- depends_on: [T-04]
- acceptance: AC-01, AC-02, AC-03, AC-04, AC-05, AC-06, AC-07, AC-08, AC-09, AC-10, AC-11, AC-12
- evidence: docs/specs/AIU-038-ledger-presentation/verification.md

- [x] Regression suites, Debug build, unsigned Release MSIX, product/plain-demo smoke.
- [x] Integrated primary review and explicit per-AC evidence/limitations.
- [x] Font completion and byte-for-byte font/licence checks in unpackaged/MSIX output.
- [ ] Owner-controlled Windows matrix; feature closure.

## Handoff

Continued Claude's `dbe1db1` on 2026-10-02, preserving both uncommitted layout fixes.
Previous implementation commits are `ae2dc66` and `dbe1db1`; branch base is `019836a`.
The continuation commit is a WIP save point, not acceptance closure or a release.
No subagents were used. Primary review applies under CONTRIBUTING: this change touches
synthetic demo/presentation code, not material credential, durable destructive-data or
privilege boundaries.

PASS: Presentation 261/261, Infrastructure 421/421, validator regressions 80/80,
Debug build with zero warnings/errors, unsigned Release MSIX 2026.10.202.0,
actual product and plain-demo startup. Commands, UI evidence and limitations are in
verification.md. No pending worker artifacts. Generated output stays ignored.

Font continuation after `e27dafd`: PD-038-03 approved and implemented; Presentation
261/261 passed again, Debug has zero warnings/errors, unsigned MSIX 2026.10.203.0
contains all seven font/licence files byte-for-byte. Verified actual Compact Used/Left,
Comfortable, settings/editor, inline confirmation/cancel and tray typography.

NOT_RUN: actual 100/150/200% display settings, light/contrast
themes, Narrator, animations off and full side-by-side fidelity comparison.
Current display smoke does not substitute for these checks. No system settings changed.

Font/code commit: `aa416aa`. Additional current-display gallery observations are in
verification.md. A request to allow temporary agent-controlled display/theme/Narrator/
animation changes, followed by restoration, is pending; no such changes were made.

Exact next action: obtain the pending Windows-settings answer, then run S1/S2 at actual
100% display scaling with either owner or newly authorized agent setting it, followed
by 150%/200% and the remaining reference/accessibility checks. No font approval is outstanding.
Do not mark AIU-038 done from this save point.
