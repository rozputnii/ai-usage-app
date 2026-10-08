---
id: AIU-055
schema_version: 1
---

# AIU-055 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to run this plan in the parallel waves described under "Execution model"; that section overrides the skill's one-task-at-a-time loop and its stop before merges and pushes. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The tray flyout becomes a focus-free miniature with provider icons, one today bar per subscription and a five-hour ring. Subscriptions are renamed by clicking their name and ordered by dragging. The refresh interval is a setting from 1 to 60 minutes.

**Architecture:**
- Presentation: `LedgerTrayViewModel` projects one row per account from `AccountCard.Primary`, with one today cell from `CardVisuals` and an optional ring. `LedgerTrayWindow` draws it with two new controls (`FiveHourRing`, `ProviderMark`), and the cards gain click-rename and a drag grip.
- Preferences: `LedgerPreferenceStore.State.AccountOrder` and `LedgerPreferences.RefreshMinutes` are the only new persisted values. `LiveLedgerSource` orders accounts and schedules refreshes from them.
- Core: `ReadingContinuity.Tolerance(interval)` replaces the fixed 15 minutes in the reading calculations through optional parameters, so existing callers keep their behaviour.

**Tech Stack:** C#/.NET 10, WinUI 3, CommunityToolkit.Mvvm, xUnit v3 (test projects run with `dotnet run`), FlaUI desktop smokes.

**Spec:** [spec.md](spec.md). Every worker reads it before its task; it is the binding authority.

## Execution model

This section binds the controller that runs this plan with superpowers:subagent-driven-development.
It overrides the skill where they differ.

1. **Owner direction (2026-10-08).** The owner wants maximum parallelism, no questions
   and automatic merge and push.
   - One autonomous worker runs each worker task (T-01 to T-11) end to end: it
     implements test-first, has its own work reviewed by nested reviewer subagents,
     merges fresh `main` and pushes to `main`.
   - Pushes and merges to `main` by workers and the controller are owner-authorized
     (CONTRIBUTING Git policy and this direction), so they are not stop points.
   - The controller does not run the skill's separate implementer, reviewer and fix loop
     per task. It dispatches, monitors, verifies integration, keeps the canonical records
     (this file, `verification.md`, backlog) and handles failures.
2. **Dispatch.** Call `Agent` with `subagent_type: "general-purpose"`, the model named in
   the task, `isolation: "worktree"` and `run_in_background: true`. The prompt is the
   worker brief below, filled in.
   - Dispatch every task whose `depends_on` tasks are all recorded as integrated, in one
     message.
   - Do not wait for a whole wave: start each task as soon as its own dependencies are
     integrated.
   - Waves (guidance): 1 = T-01 to T-06; 2 = T-07, T-08, T-09; 3 = T-10, T-11;
     4 = T-12 (controller).
3. **After each worker report:**
   - Run `git fetch origin` and confirm that
     `git merge-base --is-ancestor <reported sha> origin/main` succeeds.
   - Read the report and spot-check its diff (`git show --stat`).
   - Then, as the primary, set the task's `status: done` and `evidence: integrated into
     main at <sha>; <checks>`, and append the report's evidence rows to
     `verification.md`.
   - Commit as `docs(AIU-055): record T-0n` after merging `origin/main`, and push.
     Completions that arrive together go in one commit.
4. **Failures.** Rule, record the ruling in the SDD ledger and continue; do not ask the
   owner.
   - BLOCKED by a locked desktop: resume the same worker later with `SendMessage` to
     rerun only its smoke-and-push step.
   - FAIL, or concerns that leave an acceptance item unmet: dispatch a fresh worker with
     the previous report and the exact defect. The second retry uses `model: "opus"` and
     names the competing approaches.
   - After two failed retries, record the task as blocked with its evidence and finish
     the remaining tasks.
5. **Final.** The controller runs T-12 itself.

### Worker brief (fill `<…>`)

```text
You are the autonomous worker for task <T-0n> of AIU-055 in the AI Usage repository. You run in your own git worktree.
Read in order: AGENTS.md, CONTRIBUTING.md (Git policy), docs/specs/AIU-055-tray-miniature-order-refresh/spec.md, then in
docs/specs/AIU-055-tray-miniature-order-refresh/tasks.md the sections "Global Constraints", "Worker procedure" and "<T-0n>".
Follow the Worker procedure exactly, including your own independent review, merging fresh main and pushing to main.
Never ask the owner anything: decide, and record each decision in your report. You may dispatch subagents: read-only research
agents to compare solutions, and the reviewer the procedure requires. Return only the report format of the procedure.
```

## Global Constraints

- **Tray:**
  - width 260 px;
  - today bar 14 px tall with radius 6, drawn by `TodayStrip` with `CellHeight = 14` and `FocusableCells = false`;
  - ring 16 × 16 px, stroke 2, radius 7, centre (8, 8), rail `Rail`, arc from 12 o'clock clockwise with round caps;
  - padding Compact `(12, 8, 12, 8)`, Comfortable `(15, 13, 15, 12)` (the card's `CardPadding`).
- **Ring tooltip copy:** `Current 5h window · <n> % used · until <HH:mm>`, `Current 5h window · <n> % left · until <HH:mm>`,
  `Next 5h window · starts on first use`. Times use `LedgerFormat.Clock`, numbers `LedgerFormat.Round`.
- **Refresh:**
  - `LedgerPreferences.RefreshMinutes`, whole minutes `1..60`, default `5`;
  - an account is due at `now - FetchedAt >= interval - 30 s`;
  - continuity tolerance `max(15 min, 3 × interval)`;
  - failure backoff unchanged (`Math.Min(30, 5 << failures)` minutes: 10, 20, then 30).
- **Order:** `LedgerPreferenceStore.State.AccountOrder`, at most 256 distinct `N`-format non-empty GUIDs; unlisted accounts
  follow in registry order.
- **Copy:** the rename tooltip is `Rename`; the settings row reads `Refresh`, `[−] <n> min [+]`.
- **No:**
  - new dependency;
  - edits to `Themes/Ledger/Styles.xaml` or `Tokens.xaml` (use existing styles and tokens inline);
  - provider request, sign-in or live credentials (demo mode, isolated `AIU_DEVELOPMENT_STATE_DIRECTORY`);
  - new logging event (see the spec's Diagnostics);
  - screen-reader, contrast or DPI checks (owner direction 2026-10-03).
- Keep the row and card `AutomationProperties.Name` values that the smokes use.
- **Writes:**
  - a worker writes only the files in its task's `writes` list;
  - if it must touch another file, it keeps the change minimal and names the file in its report;
  - workers never edit `docs/` (the controller owns the records).
- **Commits:** `feat(AIU-055): … (T-0n)`, `test(AIU-055): … (T-0n)`, `fix(AIU-055): … (T-0n)`.
  - No attribution lines.
  - Never `--no-verify`, never force-push, never rewrite published history.
- **Host:**
  - Windows PowerShell 5.1 only (no `pwsh`).
  - Microsoft Defender can delay the first start of fresh binaries: rerun a first-run timeout once before calling it a
    failure.
  - The owner may be using the desktop.
- **Desktop smokes take the mouse and keyboard.** Run every FlaUI smoke under this lock (PowerShell 5.1):

```powershell
$lock = Join-Path $env:LOCALAPPDATA 'AiUsage-desktop-smoke.lock'
while ($true) {
  try { New-Item -ItemType Directory -Path $lock -ErrorAction Stop | Out-Null; break }
  catch { if ((Get-Item $lock).CreationTime -lt (Get-Date).AddMinutes(-30)) { Remove-Item $lock -Recurse -Force } else { Start-Sleep -Seconds 20 } }
}
try { <run the smoke exe with its -method filter> } finally { Remove-Item $lock -Recurse -Force }
```

## Worker procedure

1. **Sync.** Run `git fetch origin` and `git merge --no-edit origin/main`.
   - Confirm that the commits of every `depends_on` task are present
     (`git log --oneline --grep "(T-0x)"`). If one is missing, return BLOCKED.
2. **Restore and baseline.** Restore once from the local package cache:
   - `dotnet restore src/windows/AiUsage.Windows/AiUsage.Windows.csproj -p:Platform=x64`;
   - the same for `tests/windows/AiUsage.Presentation.Tests`, `tests/windows/AiUsage.Infrastructure.Tests`,
     `tests/AiUsage.ProjectValidation.Tests` and `tools/AiUsage.ProjectValidation`;
   - `tests/windows/AiUsage.Windows.Tests` with `-r win-x64` when the task has a smoke.

   Run the task's suites on the untouched tree and note the pass counts.
3. **Read** the spec, Global Constraints, your task and every file in its list.
4. **Decide, don't ask.**
   - When the task leaves a real choice, dispatch one or two read-only research subagents with one precise question each:
     `subagent_type: "Explore"` or `"general-purpose"`, `model: "sonnet"`, told not to edit.
   - Pick the simplest option that meets the spec, and record it under DECISIONS.
5. **Test-first.** Follow the task's steps. Each new test fails before the code (show the failure) and passes after.
6. **Checks.**
   - The task's suites:
     - `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo`;
     - `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo`.
   - `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Release -p:Platform=x64 -p:WindowsPackageType=None --no-restore`
     must report 0 warnings and 0 errors. Also run `git diff --check`.
   - Run smokes only with the steps below.
     - Build Debug with `-c Debug` and the same flags.
     - Publish the UI suite with `dotnet publish tests/windows/AiUsage.Windows.Tests -c Release -r win-x64 --self-contained true`.
     - Set `AIU_SMOKE_EXE` to the absolute path of
       `src/windows/AiUsage.Windows/bin/x64/Debug/net10.0-windows10.0.26100.0/win-x64/AiUsage.exe`.
     - Set `AIU_SMOKE_EVIDENCE_DIRECTORY` to `<worktree>/.ai-usage-local/AIU-055/<T-0n>` (git-ignored), and
       `AIU_SMOKE_MODE=demo` where the smoke reads it.
     - Run the published `AiUsage.Windows.Tests.exe -method "<filter>"` under the desktop lock.
   - Open every screenshot you capture (Read tool) and compare it with the spec. A locked desktop: retry every 5 min for
     up to 30 min, then return BLOCKED without pushing.
7. **Independent review.** Save `git diff origin/main...HEAD` to `.ai-usage-local/AIU-055/<T-0n>/review.diff`.
   - Dispatch a fresh reviewer: `subagent_type: "aiu-reviewer"` (if that type is not listed, `"general-purpose"` with
     "read-only: never edit, commit or push"), `model: "opus"`.
   - Give it the spec path, your task id, the diff file, your test output paths and the worktree path.
   - Ask for a met / not met / cannot-verify verdict per acceptance item, and for findings rated Critical, Important or
     Minor with file:line.
   - Fix Critical and Important findings test-first and rerun the checks. If a fix changed behaviour, get one scoped
     re-review. Stop after at most three review rounds.
   - Report residual disagreements with your reasons.
8. **Integrate.** Run `git fetch origin` and `git merge --no-edit origin/main`.
   - Resolve conflicts so that both sides' behaviour and tests survive.
   - Rerun the task's suites and the Release build when the merge changed any `src/` or `tests/` file.
   - Always run the Preview-gate demo startup smoke, `-method "*LedgerLaunchSettingsHistoryAndExit*"`, under the lock.
   - Run `git push origin HEAD:main`. If the push is rejected, repeat this step.
   - Finally, run `git fetch origin` and confirm that `git merge-base --is-ancestor HEAD origin/main` succeeds.
9. **Report.** Return only this format, at most 15 lines:

```text
STATUS: DONE | DONE_WITH_CONCERNS | BLOCKED
TASK: <T-0n>   MERGED: <sha on origin/main, verified>   COMMITS: <shas>
TESTS: <suite passed/total (baseline passed/total)>; Release build <warnings/errors>; diff check
SMOKE: <filter: PASS|FAIL|BLOCKED, evidence path>
REVIEW: <verdict; findings fixed; findings left with reason>
DECISIONS: <each ruling in one line>
OUT_OF_SCOPE_FILES: <paths or none>   NOT_RUN: <item: reason>
```

## Review Focus

1. **Two subscriptions of one provider in the tray** carry identical marks. The tooltip must still tell them apart by
   display name. Pinned by T-10 `SameProviderRowsAreToldApartByTheirTooltip`.
2. **A short interval while an account is backing off after a failure.** The backoff must win, so a 1 min interval
   never retries a failing account every minute. Pinned by T-08 `BackoffWinsOverAShortInterval`.
3. **Clicking a second card's name while the first card is being renamed** must save the first name and start the
   second rename. Pinned by T-04 smoke step 3.
4. **A tray with no accounts, and an account whose primary limit is note-only,** keep the empty text and show a row
   without a bar. Pinned by T-07 `EmptyTrayAndNoteOnlyPrimaryRow`.
5. **Stepper input `""`, `"05"` and a paste of `"7a"`.** Empty input reverts, `05` saves 5, and the paste is refused.
   Pinned by T-11 `StepperTextEdgeCases`.

---

### T-01 - Tray flyout without tab stops or focus frames
- status: done
- depends_on: []
- ownership: tray-focus
- writes: [src/windows/AiUsage.Windows/Controls/Ledger/LedgerTrayWindow.cs, tests/windows/AiUsage.Windows.Tests/LedgerSmoke.cs, tests/windows/AiUsage.Windows.Tests/AuditTrayControls.cs]
- shared: []
- parallel: true
- isolation: required
- agent: task-worker
- acceptance: [AC-01]
- evidence: docs/specs/AIU-055-tray-miniature-order-refresh/verification.md; integrated into main at dc9664f and 88828c8 (merged 0c2157b); Ledger launch smoke PASS with the no-focusable assertion, Presentation 316/316, Release build 0 warnings, opus review approve; AuditTrayControls compiled, run NOT_RUN (Sandbox opt-in)

**Model:** sonnet. **Requirement:** R-01.

- [x] In `LedgerSmoke.LedgerLaunchSettingsHistoryAndExit`, change the tray part:
  - Before choosing a row, capture the flyout window to `tray.png`.
  - Assert `Assert.DoesNotContain(trayWindow.FindAllDescendants(), e => e.Properties.IsKeyboardFocusable.ValueOrDefault)`.
  - Replace `row!.Focus(); … Keyboard.Press(VirtualKeyShort.RETURN)` with `row!.Click()`.
  - Keep the two existing waits: the window restores, and focus lands in a `claude-week-` card.
- [x] Publish the UI suite and run `-method "*LedgerLaunchSettingsHistoryAndExit*"` under the lock. Expect a FAIL on the
  focusable assertion.
- [x] In `LedgerTrayWindow`:
  - `LedgerClickRow` rows get `IsTabStop = false` and `UseSystemFocusVisuals = false`, without the focus-visual brushes or
    thicknesses.
  - Each strip body is added directly, carrying its tooltip and `AutomationProperties.Name`, with no `ContentControl`
    stop.
  - Remove `XYFocusKeyboardNavigation`, `rowStops`, `FocusFirstRow` and its two calls, the arrow-key branch and both
    Enter handlers.
  - Keep `root.KeyDown` for Esc only.
- [x] Rerun the smoke: PASS. Open `tray.png`: no frame. Click-away still hides the flyout (manual check with the demo
  build). Record whether Esc still closes the flyout.
- [x] Update `AuditTrayControls` (a Sandbox-only audit) to the click-based selection and the no-focusable assertion.
  Build it with `dotnet build tests/windows/AiUsage.Windows.Tests -c Release`; the run is NOT_RUN (Sandbox opt-in).
- [x] Commit `fix(AIU-055): tray flyout has no tab stops or focus frames (T-01)`, then review, integrate and report.

### T-02 - Provider marks
- status: done
- depends_on: []
- ownership: provider-marks
- writes: [src/windows/AiUsage.Windows/Features/Ledger/ProviderMarkData.cs, src/windows/AiUsage.Windows/Controls/Ledger/ProviderMark.cs, tests/windows/AiUsage.Presentation.Tests/ProviderMarkTests.cs]
- shared: []
- parallel: true
- isolation: required
- agent: task-worker
- acceptance: [AC-05]
- evidence: docs/specs/AIU-055-tray-miniature-order-refresh/verification.md; integrated into main at 804945f and 13fb1a7 (merged 6f6eebd); Presentation 306/306, Release build 0 warnings, demo startup smoke PASS, mark renders checked, opus review approve

**Model:** opus (drawing judgment). **Requirement:** R-07.

**Interfaces — Produces:**
```csharp
internal static class ProviderMarkData { public static string PathData(ProviderKind provider); }   // fill geometry, 24 × 24 view box
internal static class ProviderMark { public static FrameworkElement Create(ProviderKind provider, Brush brush, double size = 16); }
```

- [x] Test `EveryProviderHasADistinctMark` (in `ProviderMarkTests`). For each `Enum.GetValues<ProviderKind>()`:
  - the path data is non-empty and matches `^(F[01] )?M[MmLlHhVvCcSsQqTtAaZz0-9 ,.\-]+$`;
  - the four strings are distinct.

  Run it and expect a compile failure.
- [x] Author `PathData`: simplified, recognisable single-colour silhouettes of each provider's mark, written by hand.
  - **Claude:** the radial spark.
  - **Codex:** the OpenAI knot simplified to rounded interlocking petals.
  - **Copilot:** the pilot helmet with two goggles.
  - **Antigravity:** its arch mark.

  A research subagent may look at public references; the repository receives no downloaded file. Each mark must read
  at 16 px: no detail below 1.5 px in the 24-unit box.
- [x] Implement `Create` as a `Viewbox` of `size` containing a 24 × 24 `Path` with `Fill = brush`. Parse the data with
  `XamlBindingHelper.ConvertValue(typeof(Geometry), data)`, as `LedgerTrayWindow` does.
- [x] Render check:
  - Write each path into `.ai-usage-local/AIU-055/T-02/<provider>.svg`: a 24 × 24 view box, white fill on `#1F1E1B`, at
    16 px and at 64 px.
  - Render each to PNG with `msedge --headless --screenshot=<png> --window-size=200,120 <svg>`.
  - Open the PNGs and revise until each mark is recognisable at 16 px.
- [x] Run Presentation: PASS. Commit `feat(AIU-055): monochrome provider marks (T-02)`; review (give the reviewer the
  PNGs), integrate, report.

### T-03 - Five-hour ring control
- status: done
- depends_on: []
- ownership: five-hour-ring
- writes: [src/windows/AiUsage.Windows/Features/Ledger/RingGeometry.cs, src/windows/AiUsage.Windows/Controls/Ledger/FiveHourRing.cs, tests/windows/AiUsage.Presentation.Tests/FiveHourRingTests.cs]
- shared: []
- parallel: true
- isolation: required
- agent: task-worker
- acceptance: [AC-03]
- evidence: docs/specs/AIU-055-tray-miniature-order-refresh/verification.md; integrated into main at 8ce42df and f20091a (merged fcafc33); Presentation 305/305, Release build 0 warnings, demo startup smoke PASS, opus review approve

**Model:** sonnet. **Requirement:** R-04 (drawing only; T-07 supplies the values).

**Interfaces — Produces:**
```csharp
internal static class RingGeometry
{
    public const double Size = 16, Stroke = 2, Radius = 7;
    public static double Clamp(double fraction);                 // NaN or < 0 → 0, > 1 → 1
    public static (double X, double Y) ArcEnd(double fraction);  // centre (8, 8), y down, clockwise from 12 o'clock
    public static bool IsLargeArc(double fraction);              // fraction > 0.5
}
internal sealed partial class FiveHourRing : Grid               // 16 × 16
{
    public double Fraction { get; set; }   // dependency property, clamped
    public Brush? ArcBrush { get; set; }   // dependency property
}
```

- [x] Test `ArcEndsFollowTheClock`, tolerance `1e-9`:
  - `ArcEnd(0) == (8, 1)`, `ArcEnd(0.25) == (15, 8)`, `ArcEnd(0.5) == (8, 15)` and `ArcEnd(0.75) == (1, 8)`;
  - `IsLargeArc(0.5) == false` and `IsLargeArc(0.51) == true`;
  - `Clamp(1.4) == 1`, `Clamp(-0.2) == 0` and `Clamp(double.NaN) == 0`.

  Run it and expect a compile failure.
- [x] Implement `RingGeometry`. Implement `FiveHourRing`:
  - a rail `Ellipse` (stroke `Rail`, 2 px, 14 × 14 at 1, 1);
  - for `0.001 < f < 0.999`, a `Path` with a `PathFigure` starting at (8, 1) and one `ArcSegment` to `ArcEnd(f)`:
    `Size (7, 7)`, `SweepDirection.Clockwise`, `IsLargeArc` from the geometry;
  - for `f ≥ 0.999`, a full `Ellipse` stroked with `ArcBrush`; for `f ≤ 0.001`, no arc;
  - stroke 2 and round start and end caps.

  Rebuild on either property change.
- [x] Run Presentation: PASS. Commit `feat(AIU-055): five-hour ring control (T-03)`; review, integrate, report.

### T-04 - Rename by clicking the name
- status: done
- depends_on: []
- ownership: click-rename
- writes: [src/windows/AiUsage.Windows/Features/Ledger/Views/LedgerCardView.xaml, src/windows/AiUsage.Windows/Features/Ledger/Views/LedgerCardView.xaml.cs, src/windows/AiUsage.Windows/Features/Ledger/Views/LedgerWindow.xaml.cs, tests/windows/AiUsage.Windows.Tests/CardEditingSmoke.cs]
- shared: []
- parallel: true
- isolation: required
- agent: task-worker
- acceptance: [AC-06]
- evidence: docs/specs/AIU-055-tray-miniature-order-refresh/verification.md; integrated into main at 6231cbe and c6bd591 (merged bbd9869); ClickingTheNameRenamesTheAccount smoke FAIL then PASS, Ledger launch smoke PASS 3/3, Presentation 322/322, Release build 0 warnings, opus review approve

**Model:** opus. **Requirement:** R-09.

- [x] Smoke `CardEditingSmoke.ClickingTheNameRenamesTheAccount`, in the demo app, with helpers reused from the existing
  smokes:
  1. Click the `Claude Pro` name text, type `Claude Work` and press Enter. A text `Claude Work` appears.
  2. Click it again, type `Claude Home`, then click an empty area of the `Codex Pro` card body. `Claude Home` is shown.
  3. Click it again and type `First`. Then click the `Codex Pro` name: `First` is saved and a box named `Account name`
     with text `Codex Pro` is open. Press Esc: `Codex Pro` is unchanged.
  4. Save `rename.png`.

  Run it under the lock and expect FAIL.
- [x] In `LedgerCardView.xaml`:
  - Wrap the name `TextBlock` in a `lc:LedgerClickRow` (`IsTabStop="False"`) whose `Tapped` calls `ViewModel.BeginRename()`.
  - Inside it, a dotted underline (`Line`, `LedgerUnderlineBrush`, `StrokeDashArray="1,2"`, `Margin="0,0,0,-2"`, as the
    hidden mark) is visible only while the pointer is over the name.
  - Set `ToolTipService.ToolTip="{x:Bind lc:LedgerViews.TextTip('Rename')}"`.
  - Hide it while renaming, as the name is now. Sections keep `BeginRename`'s early return.
- [x] Handle `RenameBox.LostFocus` with `_ = ViewModel.CommitRenameAsync()`.
- [x] In `LedgerWindow.xaml.cs`, a root `PointerPressed` handler (`AddHandler(…, handledEventsToo: true)`) commits any
  card with `IsRenaming` when the press is outside its `RenameBox`.
- [x] Rerun the smoke and the Ledger launch smoke: PASS; open `rename.png`. Presentation stays green. Commit
  `feat(AIU-055): rename an account by clicking its name (T-04)`; review, integrate, report.

### T-05 - Account order in preferences, sources and keyboard
- status: done
- depends_on: []
- ownership: account-order
- writes: [src/windows/AiUsage.Windows/Features/Ledger/Contract/LedgerContract.cs, src/windows/AiUsage.Windows/Adapters/Live/LedgerPreferenceStore.cs, src/windows/AiUsage.Windows/Adapters/Live/LiveLedgerSource.cs, src/windows/AiUsage.Windows/Features/Ledger/Demo/DemoLedgerSource.cs, src/windows/AiUsage.Windows/Features/Ledger/LedgerViewModel.cs, src/windows/AiUsage.Windows/Features/Ledger/LimitCardViewModel.cs, tests/windows/AiUsage.Presentation.Tests/LiveLedgerSourceTests.cs, tests/windows/AiUsage.Presentation.Tests/LedgerPreferenceTests.cs, tests/windows/AiUsage.Presentation.Tests/LedgerCompletionTests.cs, tests/windows/AiUsage.Presentation.Tests/AccountOrderTests.cs]
- shared: []
- parallel: true
- isolation: required
- agent: task-worker
- acceptance: [AC-07]
- evidence: docs/specs/AIU-055-tray-miniature-order-refresh/verification.md; integrated into main at 88b13fb (merged 387e0df); Presentation 302/302, Infrastructure 915/915, Release build 0 warnings, demo startup smoke PASS, opus review approve with 3 deferred minors

**Model:** opus. **Requirement:** R-10 (all but the drag, which is T-09).

**Interfaces — Produces:**
```csharp
// ILedgerSource, after MoveCardAsync:
Task<CommandOutcome> MoveAccountAsync(string accountId, string? beforeAccountId, CancellationToken ct); // null = last
// LedgerPreferenceStore.State:
public string[] AccountOrder { get; set; } = [];
// LedgerViewModel:
public Task MoveAccountAsync(string accountId, string? beforeAccountId);  // on Done raises FocusCardRequested for the account's primary card
public Task MoveAccountByAsync(string accountId, int offset);             // ±1 among visible accounts
// LimitCardViewModel: MoveUpAsync/MoveDownAsync move the account for a host card and the section for an account section.
```

- [x] Test `AccountOrderMovesPersistsAndSurvivesRestart` (`LiveLedgerSourceTests`). Use accounts `a, b, c` (`Account(Guid, at)`)
  and the saved-preferences pattern of `MonetaryCapsStayMatchedAndSeparateAcrossRenameAndSourceRestart`.
  - `MoveAccountAsync(c, a)` returns Done, and `Current.Accounts` IDs are `[c, a, b]`.
  - `MoveAccountAsync(a, null)` gives `[c, b, a]`.
  - After a restart the order is still `[c, b, a]`; a new account `d` gives `[c, b, a, d]`.
  - An unknown id, `MoveAccountAsync(a, a)` and an unknown `before` each return Rejected.
- [x] Test `AccountOrderIsValidated` (`LedgerPreferenceTests`) with `LedgerPreferenceStore.IsValidJson`:
  - `AccountOrder` `["x"]`, a duplicate and 257 entries are each false;
  - a version-1 JSON without `AccountOrder` is true and loads `[]`.
- [x] Tests in `AccountOrderTests`, on the demo app with `LedgerViewModel`, as in the `Start()` pattern of `LedgerTests`:
  - `KeyboardMovesTheAccountAndSectionsStayInside`:
    - after `MoveDownAsync()` on `claude-week`, the account order of `window.Cards` is `acct-codex, acct-claude, acct-copilot, acct-antigravity`;
    - `MoveUpAsync()` on `claude-extra` still reorders only within `acct-claude`;
    - `LedgerTrayViewModel.Project(source.Current, source.Preferences)` rows follow the same account order.
  - `MoveToLastAndBack`: `MoveAccountAsync("acct-claude", null)` puts Claude last, and `MoveAccountAsync("acct-claude", "acct-codex")`
    puts it first.
- [x] Update `LedgerCompletionTests.ReorderingReturnsFocusToTheMovedCardAfterTheOrderChanges` to expect
  `[("claude-week", 2), ("claude-week", 0)]`, because the Claude account moves past Codex's two cards. Run Presentation
  and expect the new tests to fail.
- [x] Implement:
  - **Live source:** `Valid()` checks `AccountOrder`. `BuildAsync` orders `models` by index in `AccountOrder` (unlisted →
    `int.MaxValue`, stable). `MoveAccountAsync` validates IDs against `accounts.Current`, rebuilds the full order from
    `Current.Accounts`, removes the account, inserts it before `before` (or appends) and saves through
    `preferences.ChangeAsync`.
  - **Demo source:** reorders `Current.Accounts` the same way and publishes.
  - **View models:** `LedgerViewModel.MoveAccountByAsync` uses the visible account order: up → before the previous
    visible account; down → before the account two places on, or null when that leaves the end. `LimitCardViewModel`
    routes by `IsAccountSection`.
- [x] Run Presentation: PASS. Commit `feat(AIU-055): saved subscription order, Alt+arrows move the account (T-05)`;
  review, integrate, report.

### T-06 - Core continuity tolerance
- status: done
- depends_on: []
- ownership: reading-continuity
- writes: [src/windows/AiUsage.Core/Budget/ReadingContinuity.cs, src/windows/AiUsage.Core/Budget/ReadingCalculations.cs, src/windows/AiUsage.Core/Budget/ExtraUsageEvidence.cs, tests/windows/AiUsage.Infrastructure.Tests/ReadingContinuityTests.cs]
- shared: []
- parallel: true
- isolation: required
- agent: task-worker
- acceptance: [AC-10]
- evidence: docs/specs/AIU-055-tray-miniature-order-refresh/verification.md; integrated into main at dbb0b8c; Infrastructure 915/915, Presentation 297/297, Release build 0 warnings, demo startup smoke PASS, opus review approve

**Model:** sonnet. **Requirement:** R-13 (Core part).

**Interfaces — Produces:**
```csharp
public static class ReadingContinuity
{
    public static readonly TimeSpan Floor = TimeSpan.FromMinutes(15);
    public static TimeSpan Tolerance(TimeSpan refreshInterval); // max(Floor, 3 × interval); throws ArgumentOutOfRangeException when interval <= 0
}
// New last optional parameter `TimeSpan? tolerance = null` (null = Floor) on:
// ReadingCalculations.DayStart, ReadingCalculations.Track, ExtraUsageEvidence.Calculate
```

- [x] Tests in `ReadingContinuityTests`. Build fixtures like `ReadingBudgetTests`, `ReadingBoundaryTests` and the existing
  extra-usage tests.
  - `ToleranceIsThreeIntervalsWithAFifteenMinuteFloor`: 1 → 15, 5 → 15, 6 → 18, 30 → 90 and 60 → 180 min; 0 throws.
  - `DayStartCarriesWithinTheTolerance`: the last reading before midnight, confirmed 40 min before it with a different
    value from the first after. With tolerance 90 min the origin is `Carried`; with the default it is `Since`; at 100 min
    before midnight and tolerance 90 it is `Since`.
  - `TrackFlagsADropOnlyAfterALongerGap`: a drop after a 60 min gap is not incomplete with tolerance 90 and is
    incomplete with the default.
  - `ExtraUsageEvidenceAcceptsAReadingWithinTheTolerance`: a full window confirmed 40 min ago gives non-null evidence
    with tolerance 90 and `(null, null, null)` with the default.

  Run them and expect a compile failure.
- [x] Implement by replacing the four `TimeSpan.FromMinutes(15)` uses with `tolerance ?? ReadingContinuity.Floor`. Keep
  `LocalBudgetStore`'s merge rule as it is.
- [x] Run Infrastructure: PASS, with every existing test unchanged. Commit
  `feat(AIU-055): continuity tolerance scales with the refresh interval (T-06)`; review, integrate, report.

### T-07 - Tray rows: main limit, one today bar, ring and density
- status: done
- depends_on: [T-01, T-03]
- ownership: tray-today
- writes: [src/windows/AiUsage.Windows/Features/Ledger/LedgerTrayViewModel.cs, src/windows/AiUsage.Windows/Controls/Ledger/LedgerTrayWindow.cs, src/windows/AiUsage.Windows/Features/Ledger/CardVisuals.cs, tests/windows/AiUsage.Presentation.Tests/LedgerTests.cs, tests/windows/AiUsage.Presentation.Tests/AccountCardTests.cs, tests/windows/AiUsage.Presentation.Tests/MonetaryGroupingTests.cs, tests/windows/AiUsage.Presentation.Tests/TrayMiniatureTests.cs, tests/windows/AiUsage.Windows.Tests/LedgerSmoke.cs]
- shared: []
- parallel: true
- isolation: required
- agent: task-worker
- acceptance: [AC-02, AC-03, AC-04]
- evidence: docs/specs/AIU-055-tray-miniature-order-refresh/verification.md; integrated into main at d94b7b8 and 10ed014 (merged 2d8e87e); Presentation 322/322, Infrastructure 915/915, Release build 0 warnings, Ledger launch smoke PASS 3/3 (Compact, Comfortable, live-empty), opus review approve

**Model:** opus. **Requirements:** R-02 to R-06. **Consumes:** `FiveHourRing`, `RingGeometry` (T-03).

**Interfaces — Produces:**
```csharp
internal sealed record TrayRing(double Fraction, Tone Tone, IReadOnlyList<string> Tip);
internal sealed record TrayRow(string AccountId, string Name, bool IsError, IReadOnlyList<string> NameTip,
    TrayStrip? Strip, TrayRing? Ring, string AccessibleName);          // replaces Strips
// LedgerTrayViewModel: public bool IsCompact { get; }                // Preferences.Density == Compact
// CardVisuals:
public static StripCell TodayOnlyCell(LimitCardModel card, AccountModel account, ValueMode mode, DateTimeOffset now);
```

- [x] Tests in `TrayMiniatureTests`, on the demo Brief scenario, `DemoLedgerScenarios.BriefNow`:
  - `EachRowShowsTheMainLimitOnly`: rows `Claude Pro, Codex Pro, Copilot Free, Antigravity AI Plus`, with strip card IDs
    `claude-week, codex-week, copilot-completions, antigravity-g1`. Codex is `SolidCritical`, and Antigravity is
    `IsError` with opacity `0.7`.
  - `TodayIsOneCellWithoutFiveHourSplit`:
    - the Claude strip has one cell, `ShowLabel == false`, part weights `[9, 9.4]` in that order (used, then allowance;
      ±0.001) and tip line 0 `Today`;
    - `TodayOnlyCell` for `copilot-completions` equals `CardVisuals.Build(…).Cells.Single()` by parts and tip.
  - `RingShowsTheCurrentFiveHourWindow`:
    - Claude: ring `0.72`, `Tone.Ok`, tip `["Current 5h window · 72 % used · until 16:05"]`;
    - in Left mode, `0.28` and `… 28 % left · until 16:05`;
    - Codex: ring `0.91` with tip `… 91 % used · until 15:48`;
    - Copilot and Antigravity have `Ring == null`;
    - a window with `CurrentWindowStarted == false` gives `0` and `Next 5h window · starts on first use`;
    - `CurrentWindowUsed == 100` gives `Tone.Critical`;
    - in the `DayOff` scenario, `Tone.Neutral`.
  - `EmptyTrayAndNoteOnlyPrimaryRow`: an empty source gives `IsEmpty` and the empty text. An account whose only card is
    `Note` gives a row with `Strip == null` and `Ring == null`.
  - `DensityFollowsPreferences`: `IsCompact` flips after `SetPreferencesAsync(… Density = Comfortable)`.
- [x] Move the tray assertions of `LedgerTests` (`TrayShowsTheOneWindowCell`, `TrayIsAMiniatureOfTheWindow`) into
  `TrayMiniatureTests` under the new shape, keeping the day-off, rush, extra-usage and open-account checks. Adjust
  `AccountCardTests` and `MonetaryGroupingTests` to `Strip`. Run them and expect failures.
- [x] Implement:
  - **`Project`:** one row per shown account; its strip comes from `AccountCard.Primary(account)` unless that card is
    `Note`. `Kind` rules as now, but `Cells` is `[TodayOnlyCell(…)]`.
  - **`TodayOnlyCell`:** the cell `Build` draws for the same card laid out as `CardLayout.Period` without `FiveHour`, so
    it is `TodayCell` with Build's inputs. Factor out what Build already computes; no copy of its body.
  - **Ring:** from `card.FiveHour` (Used mode: `CurrentWindowUsed / 100`; Left mode: `(100 − used) / 100`). Tone is
    `CardVisuals.ToneOf(card)`; Critical when `CurrentWindowUsed >= 100`; Neutral on `CardState.DayOff`.
  - **Marks:** keep the rush and extra-usage marks from the primary card.
  - **Window:**
    - `TodayStrip { CellHeight = 14, FocusableCells = false }`;
    - the empty dashed and solid red bodies at 14 px with radius 6;
    - a 16 px ring column reserved in every row (`FiveHourRing` with `ArcBrush = LedgerTheme.ToneMark(tone)`);
    - padding from `IsCompact` per Global Constraints.

    Keep the name column and the 360 px width (T-10 replaces them).
- [x] Extend the Ledger launch smoke: capture `tray.png` after opening. Run it in both densities by switching
  Comfortable in settings first in one run; it saves `tray-comfortable.png`. Open both PNGs and check 14 px bars and the
  ring.
- [x] Run Presentation and the smoke: PASS. Commit `feat(AIU-055): tray shows the main limit's today bar and a five-hour ring (T-07)`;
  review, integrate, report.

### T-08 - Refresh interval preference, schedule and staleness
- status: done
- depends_on: [T-05, T-06]
- ownership: refresh-schedule
- writes: [src/windows/AiUsage.Windows/Features/Ledger/Contract/LedgerContract.cs, src/windows/AiUsage.Windows/Adapters/Live/LedgerPreferenceStore.cs, src/windows/AiUsage.Windows/Adapters/Live/LiveLedgerSource.cs, src/windows/AiUsage.Windows/Adapters/Live/LiveLedgerProjection.cs, src/windows/AiUsage.Windows/Features/Ledger/Demo/DemoLedgerSource.cs, tests/windows/AiUsage.Presentation.Tests/LiveLedgerSourceTests.cs, tests/windows/AiUsage.Presentation.Tests/LedgerPreferenceTests.cs, tests/windows/AiUsage.Presentation.Tests/LiveLedgerProjectionTests.cs]
- shared: []
- parallel: true
- isolation: required
- agent: task-worker
- acceptance: [AC-08, AC-09, AC-10]
- evidence: docs/specs/AIU-055-tray-miniature-order-refresh/verification.md; integrated into main at 999a188 (merged a208817, bae967f); Presentation 316/316, Infrastructure 915/915, Release build 0 warnings, demo startup smoke PASS, opus review approve with 3 deferred minors

**Model:** opus. **Requirements:** R-11 (stored value), R-12, R-13 (app part), R-14.
**Consumes:** `ReadingContinuity` (T-06) and the `AccountOrder` code in the same files (T-05).

**Interfaces — Produces:**
```csharp
internal sealed record LedgerPreferences(ValueMode Mode, Density Density, bool ShowSignedOut, bool AlwaysOnTop,
    UpdateMode Updates = UpdateMode.Always, int RefreshMinutes = 5)
{ public const int MinRefreshMinutes = 1, MaxRefreshMinutes = 60; /* Default unchanged */ }
// LiveLedgerSource: internal static readonly TimeSpan DefaultRefreshInterval = TimeSpan.FromMinutes(5); (replaces RefreshInterval)
// LiveLedgerProjection.Account(…, IReadOnlyList<TodayEntry>? entries = null, TimeSpan? tolerance = null); History(…, TimeSpan? tolerance = null)
```

- [x] Tests in `LiveLedgerSourceTests` (`Clock`, `Accounts`, `Source` helpers):
  - `RefreshIntervalDrivesTheScheduleWithTickSlack`: with `RefreshMinutes = 1`, the refresh delegate returns a snapshot
    fetched at `clock.Now + 2 s`; ticks at +1, +2 and +3 min each refresh once. With `RefreshMinutes = 5`, a tick at
    +4 min does not refresh and one at +5 min does.
  - `ChangedIntervalAppliesAtTheNextTick`: fetched at t, interval 5. At t + 2 min nothing is due; after
    `SetPreferencesAsync(… RefreshMinutes = 2)` the same tick refreshes.
  - `BackoffWinsOverAShortInterval`: interval 1 and a failing account (as in `RetryBackoffIsAccountScopedAndStopPreventsFurtherTicks`):
    the account fails at the +1 min tick; ticks at +2 to +10 min do not retry it, and +11 min does (a 10 min backoff).
  - `SummaryReportsTheConfiguredInterval`: `RefreshMinutes = 10` gives `Current.Summaries.RefreshInterval == 10 min`.
- [x] Test `StaleFollowsTheTolerance` (`LiveLedgerProjectionTests`): a failed sync whose reading is 40 min old is
  `SyncFailedFresh` with tolerance 90 min and `SyncFailedStale` with the default; at 100 min with tolerance 90 it is
  `SyncFailedStale`.
- [x] Test `RefreshMinutesIsValidatedAndDefaultsToFive` (`LedgerPreferenceTests`): `RefreshMinutes` 0 and 61 are
  invalid. Old JSON without the field loads 5, through `LedgerPreferenceJson`, as in `PersistedRecordOmittedPropertyTests`.
  Run all of them and expect failures.
- [x] Implement:
  - `Valid()` checks the `RefreshMinutes` range.
  - `LiveLedgerSource` reads `TimeSpan.FromMinutes(preferences.Current.Preferences.RefreshMinutes)` for the due test
    (minus 30 s), the success spacing in `NextRetry` and `Summaries`. It passes `ReadingContinuity.Tolerance(interval)`
    to `Account` and `History`.
  - `LiveLedgerProjection` uses `tolerance ?? ReadingContinuity.Floor` for `stale` and passes it on to
    `DayStart`, `Track` and `ExtraUsageEvidence.Calculate`.
  - `DemoLedgerSource.SetPreferencesAsync` publishes `Summaries.RefreshInterval` from `RefreshMinutes`.
  - Review the refresh diagnostics per the spec.
- [x] Run Presentation and Infrastructure (the Audit* scenarios call `Account` and must stay green). Commit
  `feat(AIU-055): refresh interval preference drives the schedule and staleness (T-08)`; review, integrate, report.

### T-09 - Drag a card by its grip
- status: done
- depends_on: [T-04, T-05]
- ownership: drag-order
- writes: [src/windows/AiUsage.Windows/Features/Ledger/Views/LedgerCardView.xaml, src/windows/AiUsage.Windows/Features/Ledger/Views/LedgerCardView.xaml.cs, src/windows/AiUsage.Windows/Features/Ledger/Views/LedgerWindow.xaml, src/windows/AiUsage.Windows/Features/Ledger/Views/LedgerWindow.xaml.cs, src/windows/AiUsage.Windows/Features/Ledger/LedgerViewModel.cs, src/windows/AiUsage.Windows/Features/Ledger/LimitCardViewModel.cs, src/windows/AiUsage.Windows/Features/Ledger/ReorderMath.cs, tests/windows/AiUsage.Presentation.Tests/ReorderMathTests.cs, tests/windows/AiUsage.Windows.Tests/CardEditingSmoke.cs]
- shared: []
- parallel: true
- isolation: required
- agent: task-worker
- acceptance: [AC-07]
- evidence: docs/specs/AIU-055-tray-miniature-order-refresh/verification.md; integrated into main at 7114ed7 and c1e844c (merged e0b090a); CardEditingSmoke PASS 2/2 (drag red first), Presentation 328/328, Release build 0 warnings, opus review approve; post-merge Ledger launch smoke FAIL 2/3 from the owner's tray icon, smoke fix assigned to T-10

**Model:** opus. **Requirement:** R-10 (drag). **Consumes:** `LedgerViewModel.MoveAccountAsync(accountId, beforeAccountId)` (T-05).

**Interfaces — Produces:**
```csharp
internal static class ReorderMath
{
    public static int InsertionIndex(IReadOnlyList<double> otherMidpoints, double pointerY); // count of midpoints above pointerY
    public static string? BeforeId(IReadOnlyList<string> order, string moved, int insertionIndex); // others[index] or null
}
// LimitCardViewModel: [ObservableProperty] bool CanReorder  (set by LedgerViewModel: host card and more than one visible account)
```

- [x] Tests `ReorderMathTests`:
  - midpoints `[50, 150, 250]`: pointer 10 → 0, 100 → 1, 300 → 3;
  - `BeforeId(["a","b","c","d"], "b", 0) == "a"`, `(…, 1) == "c"` and `(…, 3) == null`.

  Run them and expect a compile failure.
- [x] Smoke `CardEditingSmoke.DraggingTheGripReordersAccounts`, in the demo app: hover `Claude Pro`, press on its grip
  (automation name `Reorder Claude Pro`) and drag it below `Codex Pro` with `Mouse.Down`, stepwise `Mouse.MoveTo` and
  `Mouse.Up`. The card name order then starts `Codex Pro, Claude Pro`. A second drag released after Esc changes
  nothing. Save `drag.png` mid-drag. Run it under the lock and expect FAIL.
- [x] Implement:
  - **Grip:** a first `Auto` header column with a grip glyph (a Segoe Fluent Icons dot gripper; check the glyph renders)
    in `LedgerQuietIconButton` look. It is visible only while the card is hovered and `CanReorder`, with the tooltip
    `Drag to reorder · Alt+↑/↓`.
  - **Dragging:** `LedgerCardView` raises `ReorderPressed(PointerRoutedEventArgs)`. `LedgerWindow`:
    - captures the pointer and starts after 4 px of movement;
    - moves the card with a `TranslateTransform` on Y, with `Canvas.ZIndex` raised and opacity 0.92;
    - draws a 2 px line (`LedgerFocusBrush`) in an overlay at the insertion gap, using only the account card views and
      ignoring the history panel;
    - on release calls `MoveAccountAsync(id, ReorderMath.BeforeId(…))` unless the order is unchanged;
    - cancels on Esc or `PointerCaptureLost`.
- [x] Run Presentation and the two `CardEditingSmoke` smokes: PASS; open `drag.png`. Commit
  `feat(AIU-055): drag a card by its grip to reorder subscriptions (T-09)`; review, integrate, report.

### T-10 - Provider icons and the narrow tray
- status: pending
- depends_on: [T-02, T-07]
- ownership: tray-icons
- writes: [src/windows/AiUsage.Windows/Features/Ledger/LedgerTrayViewModel.cs, src/windows/AiUsage.Windows/Controls/Ledger/LedgerTrayWindow.cs, tests/windows/AiUsage.Presentation.Tests/TrayMiniatureTests.cs, tests/windows/AiUsage.Windows.Tests/LedgerSmoke.cs]
- shared: []
- parallel: true
- isolation: required
- agent: task-worker
- acceptance: [AC-04, AC-05]
- evidence: not-run

**Model:** sonnet. **Requirements:** R-07, R-08. **Consumes:** `ProviderMark.Create` (T-02) and `TrayRow` (T-07).

**Interfaces — Produces:** `TrayRow` gains `ProviderKind Provider` and `IReadOnlyList<string> Tip`, where
`Tip = [Name, ..(IsError ? NameTip : [])]`.

- [ ] Tests in `TrayMiniatureTests`:
  - `RowsCarryTheProviderAndANamedTip`: Brief rows have providers `Claude, Codex, Copilot, Antigravity`. The Claude tip
    is `["Claude Pro"]`; the Antigravity tip starts `Antigravity AI Plus` and continues with its sync-failed lines.
  - `SameProviderRowsAreToldApartByTheirTooltip`: after the demo sign-in of a second Copilot account
    (`DemoLedgerSource` adds `… 2`), the two Copilot rows' `Tip[0]` differ.

  Run them and expect failures.
- [ ] Implement:
  - **Projection:** fill `Provider` and `Tip`.
  - **Window:** replace the name column and warning triangle with `ProviderMark.Create(row.Provider,
    LedgerTheme.Solid(row.IsError ? "CritText" : "Ink"), 16)` in a 16 px column. The tooltip `row.Tip` goes on the mark
    and on the row. `PopupWidth = 260`.
- [ ] Extend the Ledger launch smoke: save `tray-icons.png`; the row found by its `Claude Pro` automation name still opens
  the window. Open the PNG: four marks, 260 px, aligned bars and rings.
- [ ] Run Presentation and the smoke: PASS. Commit `feat(AIU-055): provider icons replace names in a 260 px tray (T-10)`;
  review, integrate, report.

### T-11 - Refresh stepper in Settings
- status: done
- depends_on: [T-07, T-08]
- ownership: refresh-stepper
- writes: [src/windows/AiUsage.Windows/Features/Ledger/Views/LedgerSettingsView.xaml, src/windows/AiUsage.Windows/Features/Ledger/Views/LedgerSettingsView.xaml.cs, src/windows/AiUsage.Windows/Features/Ledger/LedgerSettingsViewModel.cs, tests/windows/AiUsage.Presentation.Tests/LedgerTests.cs, tests/windows/AiUsage.Presentation.Tests/RefreshIntervalSettingsTests.cs, tests/windows/AiUsage.Windows.Tests/RefreshIntervalSmoke.cs]
- shared: []
- parallel: true
- isolation: required
- agent: task-worker
- acceptance: [AC-08]
- evidence: docs/specs/AIU-055-tray-miniature-order-refresh/verification.md; integrated into main at 274091e and f6bf869 (merged d663b58); RefreshIntervalSmoke FAIL then PASS, Ledger launch smoke PASS 3/3, Presentation 325/325, Release build 0 warnings, opus review approve

**Model:** sonnet. **Requirement:** R-11 (control). **Consumes:** `LedgerPreferences.RefreshMinutes` and its `Min`/`Max`
constants (T-08). It depends on T-07 only because both edit `LedgerTests.cs`.

**Interfaces — Produces (on `LedgerSettingsViewModel`):** `int RefreshMinutes`, `string RefreshText`,
`IAsyncRelayCommand DecreaseRefreshCommand` (can execute while > 1), `IAsyncRelayCommand IncreaseRefreshCommand`
(can execute while < 60), `bool AcceptsRefreshText(string text)` (empty or up to two digits),
`Task<bool> CommitRefreshTextAsync(string text)` (1..60 saves; otherwise reverts `RefreshText` and returns false).
`MonitoringText` is removed.

- [x] Tests in `RefreshIntervalSettingsTests` (demo `Start()` pattern):
  - `StepperSavesWithinOneToSixty`:
    - `RefreshMinutes == 5`;
    - Increase gives 6, `source.Preferences.RefreshMinutes == 6` and `Summaries.RefreshInterval == 6 min`;
    - at 60 Increase cannot execute, and at 1 Decrease cannot.
  - `StepperTextEdgeCases`:
    - `AcceptsRefreshText("7a") == false` and `AcceptsRefreshText("") == true`;
    - `CommitRefreshTextAsync("")` and `("0")` and `("61")` return false and restore the saved text;
    - `("05")` saves 5, and `("15")` saves 15.

  Replace the `"every 5 min"` assertion in `LedgerTests` with `window.Settings.RefreshMinutes == 5`. Run them and
  expect failures.
- [x] Implement:
  - **Settings view model:** the members above.
  - **XAML:** a `Refresh` row in the View section: a caption, a `LedgerIconButton` with glyph `&#xE738;` (automation
    name `Shorter refresh interval`), a 36 px `TextBox` (`BeforeTextChanging` → `AcceptsRefreshText`; Enter and
    `LostFocus` → commit; automation name `Refresh interval in minutes`), a `min` caption, and a `LedgerIconButton` with
    glyph `&#xE710;` (`Longer refresh interval`). The tooltip reads `Refresh every <n> min · 1 to 60`.
  - **Footer:** remove the footer `MonitoringText` `TextBlock`; the system status and ⋯ stay.
- [x] Smoke `RefreshIntervalSmoke.StepperChangesTheInterval`, in the demo app: open Settings, click `Longer refresh
  interval`, and the box reads `6`; type `1` and press Enter, and the box reads `1`; save `settings-refresh.png`. Run the
  smoke and the Ledger launch smoke: PASS; open the PNG.
- [x] Run Presentation: PASS. Commit `feat(AIU-055): refresh interval stepper in settings (T-11)`; review, integrate,
  report.

### T-12 - Integrated verification, review and records
- status: pending
- depends_on: [T-01, T-02, T-03, T-04, T-05, T-06, T-07, T-08, T-09, T-10, T-11]
- acceptance: [AC-11]
- evidence: not-run

**Model:** the controller (primary); the reviewer `opus`.

- [ ] In the controller worktree, merge `origin/main`. Run every check:
  - validator tests and document validation with `--final`;
  - the Infrastructure and Presentation suites;
  - the Release build (0 warnings) and `git diff --check`;
  - under the lock: `*LedgerLaunchSettingsHistoryAndExit*`, `*CardEditingSmoke*`, `*RefreshIntervalSmoke*` and, for
    regression, `*WorkBudget*`.

  Open every screenshot.
- [ ] Dispatch one whole-feature reviewer: `aiu-reviewer` (or read-only `general-purpose`), `model: "opus"`.
  - Give it the spec and `git diff <plan commit>..origin/main -- src tests`.
  - Ask for a per-AC verdict and cross-task integration issues (the tray versus the order, the interval versus
    staleness, rename versus drag in the header).
  - One fix round goes to a fresh worker (worktree, the worker procedure), followed by a scoped re-review.
- [ ] Records:
  - `verification.md`: a commands table with actual results, the screenshots list, and the NOT_RUN owner live check
    after deployment (D-190).
  - This file: every task done with its evidence.
  - `spec.md`: status `implemented`.
  - `docs/backlog.md`: status `done` with a completion-note.

  Validate with `--final`. Commit `docs(AIU-055): verification and completion records` and push.
- [ ] Confirm `origin/main` contains the commits. The final reply to the owner (in Ukrainian) lists the results and
  NOT_RUN items and states "Pushed to `main`: <hashes>".

## Acceptance criteria

The binding text is in [spec.md](spec.md#acceptance-criteria) (AC-01 to AC-11). The `acceptance` fields above map each
task to it.
