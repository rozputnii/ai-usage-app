---
id: AIU-032
schema_version: 1
---
# AIU-032 implementation plan

Goal: [spec.md](spec.md). Evidence: [verification.md](verification.md). Sequential primary
work; commit and push after each task once its check passes. No dependency changes.

### T-01 - Dark-only tokens and theme plumbing
- status: done
- depends_on: []
- acceptance: AC-01, AC-03
- evidence: docs/specs/AIU-032-dark-only-cleanup/verification.md; Presentation 172/172 including LiveAdapterTests.PreferenceFileWithUnknownMembersRoundTripsUnchanged (stored Theme kept); app and smoke projects build; interactive checks in T-04

- [x] Generator emits a single dark dictionary; delete SimulatedHighContrast.xaml.
- [x] Application requests the dark theme; ThemeService shrinks to window chrome (dark root and
      title bar); drop IThemeService, EffectiveTheme, ThemePreference and PreferenceKey.Theme.
- [x] Preferences contract loses Theme; the live store keeps an old value as extension data.
- [x] Token lookup, provider tiles and code-drawn controls stop branching on theme or contrast.

### T-02 - Remove theme settings and demo display simulation
- status: done
- depends_on: [T-01]
- acceptance: AC-02
- evidence: docs/specs/AIU-032-dark-only-cleanup/verification.md; Presentation 172/172; smoke row theme replaced by appearance (asserts no theme or contrast controls), run in T-04

- [x] Remove the App theme section, ThemeOptionViewModel, theme strings and bind helpers.
- [x] Remove the demo Windows-mode and contrast simulation (DisplaySimulation keeps reduced
      motion only).

### T-03 - Removed-UI leftovers
- status: done
- depends_on: [T-02]
- acceptance: AC-04
- evidence: docs/specs/AIU-032-dark-only-cleanup/verification.md; Presentation 167/167 (five tests of the deleted local History page removed; chart gap and reset semantics kept on ChartModel); reference scan of strings, styles and members reports only the dynamically built RecoveryLive_Body_* keys

- [x] Delete HistoryViewModel, HistoryChart and the tests that only exercised them; keep
      ChartModel and the sparkline for account detail.
- [x] WindowLineView becomes detail-only; delete ArrowNavigation, unused Bind helpers, styles,
      NavigationView aliases and strings.
- [x] Re-run the reference scan until it reports only dynamically built keys.

### T-04 - Verification and records
- status: done
- depends_on: [T-03]
- acceptance: AC-05
- evidence: docs/specs/AIU-032-dark-only-cleanup/verification.md

- [x] Presentation, Infrastructure and validator tests, document validation, unsigned MSIX,
      demo and product smoke; the capture in Windows light mode is NOT_RUN.
- [x] verification.md, backlog status review, D-182.
