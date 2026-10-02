---
id: AIU-038
schema_version: 1
---

# Redesigned presentation plan

Sequential primary work on branch `users/aiu-038-presentation-redesign-1c2839` in its own
worktree. No push to or merge into `main`; integration is a rebase after AIU-042 closes.
Do not edit Core, Infrastructure, `tools/` or `Adapters/Live`. Implementation starts only
after the owner approves the specification.

### T-01 - Contract, demo source and platform spikes
- status: pending
- depends_on: []
- acceptance: AC-01, AC-03, AC-12
- evidence: not-run

- [ ] Add the section 4 contract records and `ILedgerSource`, the boundary test, and
  `DemoLedgerSource` with the six scenarios; fixture tests for the S1 figures and positions.
- [ ] Spike in the Windows project: repeat-mode hatch brush, squircle surface (PD-038-02),
  `--demo --ledger` hook (PD-038-01) and font loading from `Assets/Fonts` unpackaged.

### T-02 - Tokens, styles and fonts
- status: pending
- depends_on: [T-01]
- acceptance: AC-07, AC-11
- evidence: not-run

- [ ] `Themes/Ledger/Tokens.xaml` and `Styles.xaml` per section 7 for both densities;
  contrast test over the token file.
- [ ] Fonts after PD-038-03: licence pages checked and recorded, .ttf added as content,
  tabular figures confirmed.

### T-03 - Card view models and controls
- status: pending
- depends_on: [T-01, T-02]
- acceptance: AC-02, AC-03
- evidence: not-run

- [ ] Card, today-strip, period-bar, footer and accessible-name view models with tests for
  every section 4.4 state and section 6.3 rule in Used and Left modes.
- [ ] `Controls/Ledger` today strip, period bar and card template bound to them.

### T-04 - Main window, tray and interactions
- status: pending
- depends_on: [T-03]
- acceptance: AC-04, AC-05, AC-06, AC-10
- evidence: not-run

- [ ] Main window S1 to S3, S6 to S9, S12 and S13; inline history S4; settings panel S5;
  undo bar; keyboard map; tray miniature S10 to S10d; command and tray tests.

### T-05 - Verification and record
- status: pending
- depends_on: [T-04]
- acceptance: AC-01, AC-02, AC-03, AC-04, AC-05, AC-06, AC-07, AC-08, AC-09, AC-10, AC-11, AC-12
- evidence: not-run

- [ ] Run the section 11 checks, record results in verification.md as PASS, FAIL, NOT_RUN
  or BLOCKED, review the integrated diff against the acceptance, commit on the branch.

## Handoff

Base: 019836a on `main`. The specification is a draft awaiting owner approval; no code has
been written. Next action: after approval, start T-01 with the contract records and the
boundary test.
