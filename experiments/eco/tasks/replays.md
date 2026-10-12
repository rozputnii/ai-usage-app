# Replays (S3)

Three past tasks, rerun from their base commits in arms A, B, C and D (one run each, plus a
repeat of the two cheapest arms on RP3). Never merged. The reference solution on `main` and its
review give the ground truth; judge each pair against arm A with `Start-Judge.ps1` and the
defect key below (pass the section to both: `-Brief replays.md#rpN -DefectKey replays.md#rpN`; the
judge then sees only that replay, not the arms or the other replays).

After S3 the operator freezes arm E: the better of C and D by the plan's decision rules, with at
most one round of tuning (record every change to `arms/E-*.md` and its reason in
`results/S3.md`).

## RP1 - peak-hours hint (T2, UI) {#rp1}

Base `0f25e49`. Held-out tests: `PeakHoursTests.cs` from `8eaf1f7` (all ten cases; the
operator adapts only namespaces or member names the brief left open, and records any change).

Brief: the approved specification as the owner and the primary wrote it before
implementation (as of `8eaf1f7`, status implementing), appended with
`-Appendix tasks/rp1-spec.md`. The appendix holds only the fenced specification: Start-Run
appends it verbatim, so an operator note there would reach the run. The run adds it as a spec (the path below is the reference's, not named in the brief; a different folder name is not a finding):
`docs/specs/T-NEW-peak-hours-hint/spec.md` (status implementing), implements it, and keeps
records per CONTRIBUTING.

> Implement the approved specification below. Owner decision 2026-10-10, recorded as R-NEW:
> a schedule-only peak-hours hint for every account, naming no provider, in the window title
> row and the tray header, with a monthly review of the schedule by an agent inside other
> work (add that review rule to AGENTS.md "Working rules" and the decision to
> docs/decisions/accepted.md). Research the announced schedule from public sources and
> record it with its sources and a `checked_at` date in `docs/providers/peak-hours.md`.
> The specification follows below.

Defect key (the real review of `8eaf1f7`, fixed in `9e0b115`; same as
[micro-review RV1](micro-review.md#rv1---peak-hours-hint-8eaf1f7), copied here because the judge
receives only this section):
- D1 (Important) the spec's verification record is missing.
- D2 (Important) the title-bar passthrough regions are not refreshed when the hint appears or
  changes (`LedgerWindow.xaml.cs` property-changed handling has no `Peak` case).
- D3 (Minor) the new AGENTS.md review rule is broader than the decision.
- D4 (Minor) the tooltip writes the schedule as a separate literal instead of using the
  schedule constants (`PeakHours.cs`).
- D5 (Minor) evidence details in peak-hours.md.
- D6 (Minor) layout nits in the tray header (vertical alignment).

## RP2 - repeated-failure diagnostics (T3, logging) {#rp2}

Base `3564996` (the spec is marked implementing). Reference: `49c7381` and `bee1252`; its
tests are the held-out tests (the operator runs the test classes `49c7381` added).

Brief:

> Implement T-062.2 as specified in `docs/specs/T-062-native-lifetime-lost-diagnostics/spec.md`
> (read the spec; its acceptance items are the requirements). This is a T3 change in the
> logging area: follow the logging policy and the security-lifecycle skill where it applies.

Defect key (the review minors fixed in `bee1252`):
- F1 `RepeatedFailure` is not null-safe for a missing signature part.
- F2 `firstAt` is not documented in the logging policy.
- F3 the fatal path's binding flush is not documented.
- F4 R-04 does not name the update checkpoint where pending counts are written.

## RP3 - test-erosion sentinel (T0) {#rp3}

Base `804096a^`. Ground truth: `804096a` (one line in `tools/windows/Test-Erosion.ps1`).

Brief:

> Bug: `tools/windows/Test-Erosion.ps1` does not flag a test that skips itself at runtime with
> `Assert.Skip(...)`; it only flags skip attributes. Make it flag runtime `Assert.Skip` calls
> too, with the minimal change, and show that it now reports such a test.

Defect key: none. Over-engineering (more than the minimal change, new files, new options) is a
Minor finding.
