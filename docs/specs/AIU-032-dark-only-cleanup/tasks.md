---
id: AIU-032
schema_version: 1
---
# AIU-032 implementation plan

Goal: [spec.md](spec.md). Evidence: [verification.md](verification.md). Sequential primary
work; commit and push after each task once its check passes. No dependency changes.

### T-01 - Dark-only tokens and theme plumbing
- status: in-progress
- depends_on: []
- acceptance: AC-01, AC-03
- evidence: not-run

- [ ] Generator emits a single dark dictionary; delete SimulatedHighContrast.xaml.
- [ ] Application requests the dark theme; ThemeService shrinks to window chrome (dark root and
      title bar); drop IThemeService, EffectiveTheme, ThemePreference and PreferenceKey.Theme.
- [ ] Preferences contract loses Theme; the live store keeps an old value as extension data.
- [ ] Token lookup, provider tiles and code-drawn controls stop branching on theme or contrast.

### T-02 - Remove theme settings and demo display simulation
- status: pending
- depends_on: [T-01]
- acceptance: AC-02
- evidence: not-run

- [ ] Remove the App theme section, ThemeOptionViewModel, theme strings and bind helpers.
- [ ] Remove the demo Windows-mode and contrast simulation (DisplaySimulation keeps reduced
      motion only).

### T-03 - Removed-UI leftovers
- status: pending
- depends_on: [T-02]
- acceptance: AC-04
- evidence: not-run

- [ ] Delete HistoryViewModel, HistoryChart and the tests that only exercised them; keep
      ChartModel and the sparkline for account detail.
- [ ] WindowLineView becomes detail-only; delete ArrowNavigation, unused Bind helpers, styles,
      NavigationView aliases and strings.
- [ ] Re-run the reference scan until it reports only dynamically built keys.

### T-04 - Verification and records
- status: pending
- depends_on: [T-03]
- acceptance: AC-05
- evidence: not-run

- [ ] Presentation, Infrastructure and validator tests, document validation, unsigned MSIX,
      demo and product smoke, interactive capture in Windows light mode.
- [ ] verification.md, backlog status review, D-182.
