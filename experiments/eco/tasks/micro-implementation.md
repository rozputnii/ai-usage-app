# Micro-trial: implementation (S2)

Tests H4: whether sonnet high matches opus medium on a fully specified brief and on a brief that
needs judgment.

## Method

Each trial is a `Start-Run.ps1` run of the probe below, haiku low primary, base as given,
`-AgentsFile ..\arms\D-agents.json` (the lean `implementer`). Variants: sonnet high, opus
medium, and for IM2 also haiku xhigh. Two repeats each.

After the run the operator scores it in the run's clone:

- IM1: copy the held-out test file from the reference commit
  (`git show 8eaf1f7:tests/windows/AiUsage.Presentation.Tests/PeakHoursTests.cs`), delete its
  last test (`TheWindowAndTheTrayShowTheHintAndKeepItWhileTheTextIsTheSame`, which needs the
  view models), and run C5 with `-class "AiUsage.Presentation.Tests.PeakHoursTests"`. Score =
  passing cases of 9.
- IM2: compare the diff with `42719a1` and build the UI test project (Release). Score = the
  three fixes present and equivalent (judged by the operator), the build passing, and no
  unrelated edits.
- Both: the subagent's cost, calls and cold start from `metrics.json`; any false PASS claim.

Each section below is a self-contained probe: run it with `-Brief micro-implementation.md#im1`
(or `#im2`) and `-Vars @{ MODEL = '...'; EFFORT = '...' }`.

## IM1 - peak-hours schedule (needs judgment) {#im1}

Base `0f25e49`. Run brief:

> This is a measurement probe, not real work. Delegate the brief below to the
> agent `implementer` with model `<MODEL>` and effort `<EFFORT>`, verbatim and nothing else,
> then return its report verbatim in NOTES. Make no changes yourself.
>
> > Work in this clone on the current branch. Create
> > `src/windows/AiUsage.Windows/Features/Ledger/PeakHours.cs` in namespace
> > `AiUsage.Features.Ledger` with `internal sealed record PeakHint(string Text,
> > IReadOnlyList<string> Tip, string AccessibleName)` and `internal static class PeakHours`
> > with two methods. `Window(DateTimeOffset now)` returns `(DateTimeOffset Start, DateTimeOffset
> > End)?`: the current peak window, weekdays 05:00 (inclusive) to 11:00 (exclusive) in Pacific
> > time (`America/Los_Angeles`, falling back to `Pacific Standard Time`; null when neither zone
> > exists), with the weekday taken in Pacific time, both bounds expressed in `now`'s offset, and
> > daylight-saving dates handled by the zone; null outside the window. `Hint(DateTimeOffset
> > now)` returns null outside the window, otherwise Text `"Peak · until " + end`, AccessibleName
> > `"Peak hours until " + end`, and Tip lines `"Peak hours"`, `"Weekdays 05:00–11:00 Pacific
> > (<start>–<end> here)"`, `"Requests may be slower or use more of a limit"`, `"An announced
> > schedule, not your account's data"`, where times use `LedgerFormat.Clock`. Write tests in
> > `tests/windows/AiUsage.Presentation.Tests/` first (red to green): window edges, the caller's
> > offset, weekends, the Pacific weekday seen from UTC+11, and the two daylight-saving changes
> > in late October and early November 2026. Run C5 and C6. Commit. Report.

## IM2 - smoke harness fixes (fully specified) {#im2}

Base `02eddb6`. Run brief:

> This is a measurement probe, not real work. Delegate the brief below to the
> agent `implementer` with model `<MODEL>` and effort `<EFFORT>`, verbatim and nothing else,
> then return its report verbatim in NOTES. Make no changes yourself.
>
> > Work in this clone on the current branch, in `tests/windows/AiUsage.Windows.Tests/`. Make
> > exactly three fixes. (1) In `CardEditingSmoke.cs`, the rename-hover wait must look for the
> > tooltip whose text is "Rename", so a leftover Refresh tooltip cannot satisfy it. (2) In
> > `LedgerWindowSizeSmoke.cs`, the squeeze must not wait for a height the second and third
> > squeezes already have: `SetWindowPos` without `SWP_ASYNCWINDOWPOS` returns once the window
> > has its new size, and `AssertClear` already waits for the title row's relayout. (3) In
> > `LedgerActivationSmoke.cs`, the first-run test must find the title-row Settings button by its
> > name, because the id `SettingsButton` is not unique. Build the project in Release and run
> > `tools/windows/Test-Erosion.ps1`, explaining any assert-count change. Desktop smokes are out
> > of scope. Commit. Report.
