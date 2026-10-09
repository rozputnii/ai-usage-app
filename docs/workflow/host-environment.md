# Host environment

Facts about the owner's Windows development host that are not derivable from the code and
cost time to rediscover. Check them before planning tooling or desktop work. Last checked
2026-10-09.

## Tools

- Only Windows PowerShell 5.1 is installed; there is no PowerShell 7 (`pwsh`). Scripts that
  need 7, such as `tools/windows/New-UiAuditPages.ps1` (`-AsHashtable`, `-DateKind`), are
  BLOCKED here. Under `$ErrorActionPreference = 'Stop'`, a native `2>&1` redirect throws
  `NativeCommandError` in 5.1.
- `python` resolves to the Microsoft Store stub. Use `py -3.13 -I` for scripts.
- Node.js is not installed.
- The .NET SDK from global.json is on PATH; the user-local fallback is
  `$HOME/.dotnet/ai-usage-sdk/dotnet.exe`.
- Optional tools installed for the Stage 2 trials (2026-10-09): `csharp-ls` 0.28.0 as a
  global .NET tool (C# language server), and the Microsoft `winapp` CLI 0.7.1 through winget
  with `WINAPP_CLI_TELEMETRY_OPTOUT=1` set for the user. See
  [the trials](claude-workflow-audit/trials.md) for use and removal.
- Claude Code sessions in this repository enable the `csharp-lsp@claude-plugins-official`
  plugin (1.0.0, project scope in `.claude/settings.json`; OD-38), which starts `csharp-ls`
  for diagnostics after edits. Each machine needs `dotnet tool install --global csharp-ls
  --version 0.28.0`; without it a session simply has no language server. Undo: delete the key
  from `.claude/settings.json`; the plugin's cached copy can stay or be removed with
  `claude plugin uninstall csharp-lsp@claude-plugins-official`.
- Long paths fail: a worktree under a deep temporary directory can push build outputs past
  the Windows path limit ("The filename or extension is too long"). Keep worktrees under
  `.claude/worktrees/` of the main checkout.
- In Git Bash, an argument that starts with `/` is rewritten into a Windows path. Run
  slash-prefixed arguments from PowerShell, or set `MSYS_NO_PATHCONV=1`.

## Tests and builds

- Microsoft Defender "Block at First Sight" (and Smart App Control in evaluation mode)
  delays the first start of freshly built binaries by 4 to 19 seconds. The first full
  Infrastructure run after a rebuild that changes Core, Infrastructure or the
  DiagnosticsProbe can fail the precondition wait in `DiagnosticCrashTests`. A plain rerun
  on the same binaries decides; never relax the bound.
- A fresh worktree has no restored assets; use the restore one-liner in
  [verification](verification.md#checks).

## Desktop

- Desktop smokes need an unlocked interactive desktop. The desktop is often locked during
  long unattended runs; schedule desktop work while the owner is present, and check the
  lock state instead of asking.
- The owner's installed Preview app runs on this host, auto-updates, and has its own tray
  icon. Smokes must not act on it.

## Network

- The IPv6 path to GitHub Pages (Fastly) and some Microsoft hosts resets about 60 % of
  TLS handshakes, while IPv4 is clean. This caused App Installer error 0x80072EFE. Test
  network paths per address family (`curl -4` and `curl -6`, `--resolve`) before blaming
  the hosting.

## Claude Code

- Agent definitions added to `.claude/agents/` during a running session load only in a
  later session. Until then, dispatch a general-purpose agent with the role contract
  inlined.
