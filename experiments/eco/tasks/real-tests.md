# Real task S4e: test suite speed and coverage

Paired run (arms A and E) from `main` after S4c and S4d merged, so the tests stay a safety net
during those refactors. Owner answers 2026-10-11: Q8 yes (`dotnet-coverage` as a local tool,
plus UI/UX behavior coverage that line coverage cannot show), Q9 yes (no fixed time target;
before/after report, slowest first).

## Brief {#brief}

> Owner request, 2026-10-11: make the test suites faster without lowering the protection they
> give the product, remove tests that are unnecessary or wrong, and cover what is not covered,
> including the UI behavior.
>
> 1. Measure first: per-test durations for C1, C4, C5 and the UI suite, and the counts per
>    project. Work on the slowest first. No fixed time target; report before and after.
> 2. Speed: remove waste (needless real delays, repeated expensive setup, serial work that can
>    share fixtures safely). Never relax a timing bound that guards behavior; the
>    `DiagnosticCrashTests` precondition wait stays as it is (docs/workflow/host-environment.md).
> 3. Removal: a test may go only when it duplicates another test, asserts nothing meaningful, or
>    pins an implementation detail instead of behavior. Each removal is listed in the
>    verification record with the requirement and the test that still covers it. Run
>    `tools/windows/Test-Erosion.ps1` and justify every item it lists.
> 4. Line coverage: add `dotnet-coverage` as a local .NET tool (a tool manifest in the
>    repository, never a product dependency; a dependency change is T3, so the focused
>    independent review applies). Report coverage for Core, Infrastructure and Windows, and add
>    tests for uncovered logic that matters (provider parsing, persistence, refresh and failure
>    handling).
> 5. UI behavior coverage: build an inventory of user interactions from the acceptance criteria
>    in `docs/specs/*/spec.md` and map each to the FlaUI tests in
>    `tests/windows/AiUsage.Windows.Tests/`. Add smokes for critical uncovered interactions
>    (drag reorder and cancel, the settings sheet, the history panel, the tray flyout), under
>    the desktop lock. Record the inventory and its gaps in the verification record.
>
> Acceptance:
> - AC-01 A before/after table of suite durations, test counts and coverage per project.
> - AC-02 Every removed test is justified as in step 3; Test-Erosion items are explained.
> - AC-03 The UI behavior inventory exists, with each critical interaction covered or listed as
>   a gap with a reason.
> - AC-04 C1, C4, C5, C7, C8, C2 and C6 pass, and the new smokes pass under the desktop lock.
