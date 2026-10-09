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

In progress; see the status log for the result.

## `/goal` trial (R15, OD-1)

NOT_RUN. The trial needs the next multi-part feature with a machine-checkable done condition
(for example "C4, C5 and C8 PASS on the merged tree and the validator `--final` valid"); this
stage had none that an unattended goal session could own. Run it on the next T2 feature and
record the result here.
