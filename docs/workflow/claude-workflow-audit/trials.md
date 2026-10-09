# Tool trials (Stage 2, step 8)

Owner decisions OD-27, OD-28 and OD-29, plus the `/goal` trial in the OD-1 bundle. Run on
2026-10-09 on the owner's Windows host. Every trial stays outside the merge gates.

## OD-27: C# language servers

**Setup.** Both servers were installed as pinned global .NET tools: `csharp-ls` 0.28.0 and
Microsoft `roslyn-language-server` 5.12.0-1.26475.2 (prerelease; telemetry is off by default
and was also passed as `--telemetryLevel off`). Each ran through a session-only plugin
directory with the same configuration as `csharp-lsp@claude-plugins-official`
(`--plugin-dir`, so no settings changed); roslyn got `--stdio --autoLoadProjects`. Both
sessions ran the same six-step task in a throwaway worktree of this branch (built, restored),
with a process memory sampler.

| | csharp-ls 0.28.0 | roslyn-language-server 5.12 prerelease |
| --- | --- | --- |
| Solution discovery (no solution at the root; `src/windows/AiUsage.slnx` exists) | Yes: Core and Windows projects loaded | No: every request answered for the single file only |
| documentSymbol, `EffectiveLimit.cs` | Correct; 24 s on the first call (server start) | Correct; about 9 s |
| findReferences, `EffectiveLimit` | 15 references in 3 files across Core and Windows | 3, all in the same file (wrong) |
| WinUI: hover on XAML-generated `InitializeComponent()` | Resolves | Does not resolve |
| goToDefinition, `LedgerTheme` | Correct target | Not found |
| Diagnostics after a planted `CS0029` | Exactly that error, within 1 s | None in 65 s |
| False positives on WinUI code-behind (`LedgerWindow.xaml.cs`) | None in 61 s | None, but it reported no diagnostics at all |
| Peak working set of the server processes | 541 MB | 339 MB (projects never loaded) |
| Whole session | 172 s | 231 s |

**Result.** Keep `csharp-ls`. It found the projects without a root solution, resolved
cross-project references and XAML-generated members, and reported diagnostics quickly and
without WinUI false positives. `roslyn-language-server` did not load any project with the
documented auto-load option in this harness; making it work would need client-side
solution-open support that Claude Code does not send. Removed: `dotnet tool uninstall
--global roslyn-language-server` (done). Kept: `csharp-ls` 0.28.0 (undo: `dotnet tool
uninstall --global csharp-ls`). Enabling the plugin for every session is OD-38.

## OD-29: Microsoft `winapp ui` CLI, read-only

**Setup.** `winget install --id Microsoft.WinAppCli --version 0.7.1 --exact --source winget
--scope user`; telemetry opted out with the user environment variable
`WINAPP_CLI_TELEMETRY_OPTOUT=1`. Undo: `winget uninstall --id Microsoft.WinAppCli` and remove
that variable. Not installed: the winappcli and WinUI plugins; never run: `winui-setup`,
`cert`, `sign` or Developer Mode commands.

**Use.** Only `status`, `inspect`, `search` and `screenshot`, against the Release unpackaged app
started with `--demo` and an empty `AIU_DEVELOPMENT_STATE_DIRECTORY`, under the desktop lock;
the app was closed by its process ID afterwards. Each verb answered in 0.2-0.7 s with JSON
(window, DPI, element tree with AutomationIds and selectors). The screenshot showed demo data
only. Note: `screenshot` writes `screenshot.png` into the current directory unless told
otherwise; run it from a scratch directory.

**Debugging value, one real finding.** `search Settings` showed that the header Settings
button and every card's "Limit settings" button share the AutomationId `SettingsButton`
(8 matches), so a smoke that looks up `SettingsButton` by id alone is ambiguous. Of 13
invokable elements in the main window, 12 have an AutomationId. This is the kind of locator
fact that smoke fixes used to discover by trial and error.

**Result.** Useful for exploring locators and capturing variant screenshots; keep it as an
optional local tool. It is exploratory only and never replaces FlaUI smoke evidence.

## OD-28: Stryker.NET spike

**Setup.** `dotnet tool install -g dotnet-stryker --version 5.0.0`. Stryker 5 does not support
Microsoft.Testing.Platform, which the xUnit v3 test projects use, so the run needed a throwaway
detached worktree (`zz-lsp`, commit 804096a) with uncommitted changes: the VSTest adapter
packages `xunit.runner.visualstudio` 3.1.5 and `Microsoft.NET.Test.Sdk` 18.10.1, and a direct
Core `ProjectReference` in `AiUsage.Infrastructure.Tests` (without it Stryker reported "No
project found"). Run from `src/windows/AiUsage.Core` with Infrastructure.Tests as
`--test-project`, `--mutate **/Budget/*.cs --concurrency 4 --configuration Release`.

**Run.** 975 mutants created; 112 compile errors and 122 ignored left 741 tested: 33 killed,
708 survived, 0 timeouts, score 4.45 %. Coverage capture failed, so every mutant ran all 917
tests. Wall time 259 min, past the half-day box, so the Infrastructure parsers run was not done.

**The score is a setup artifact, not a test weakness.** Infrastructure.Tests is the main home
of the budget tests (`BudgetEngineTests`, `SessionEstimateTests`, `ReadingBudgetTests`,
`ReadingContinuityTests` and seven more files; Presentation.Tests adds four ledger files), so
the right project ran. The mutants were not active in the code those tests executed:
- All 33 kills are attributed to one unrelated test,
  `DiagnosticCrashTests.ManagedChildCrashLeavesCriticalStackBeforeTermination`, which waits on a
  child probe process with time bounds. No budget test killed a mutant; the kills are failures
  of that test under load.
- Survivors include mutants an existing assertion cannot miss: `ReadingContinuity.cs:15`,
  `refreshInterval * 3` to `/ 3`, survived although
  `ToleranceIsThreeIntervalsWithAFifteenMinuteFloor` asserts that 30 minutes gives 90.

The likely cause is that the xUnit v3 adapter runs tests in the test executable's own process,
which Stryker's mutant switch and coverage capture do not reach. No genuine weak spot can be
established from this run.

**Recommendation.** Not adopted. Do not run the parsers spike now: it would meet the same
activation failure. Revisit when Stryker.NET supports Microsoft.Testing.Platform, and then
rerun the budget scope first, with no project changes. Do not keep the tool.

**Host changes and undo.** `dotnet-stryker` 5.0.0 as a global .NET tool; undo `dotnet tool
uninstall -g dotnet-stryker`, run on 2026-10-09 (done; not kept). The throwaway worktree
`zz-lsp`, with its uncommitted package and reference changes, was removed (done); nothing from
it was committed.
The spike changed no repository files.

## `/goal` trial (R15, OD-1)

NOT_RUN. The trial needs the next multi-part feature with a machine-checkable done condition
(for example "C4, C5 and C8 PASS on the merged tree and the validator `--final` valid"); this
stage had none that an unattended goal session could own. Run it on the next T2 feature and
record the result here.
