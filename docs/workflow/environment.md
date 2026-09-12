# Bootstrap environment and evidence

Observed 2026-09-12 through OMP on the owner workstation. Repository paths below are relative; the checked Git root was the current `ai-usage-app` checkout under the owner's projects directory. No credential stores or tokens were inspected.

## Preflight

- `git rev-parse --show-toplevel`, `git status --short --branch`, `git log -1 --format=%H`: existing repository, main tracking origin/main, base `e96b735975ced39646ab16fe6f9d38897ae0cc3c`. Only the supplied bootstrap directory was untracked. Existing README and GPL-3.0 LICENSE found.
- Origin: `https://github.com/rozputnii/ai-usage-app.git`. `gh repo view rozputnii/ai-usage-app --json nameWithOwner,viewerPermission,defaultBranchRef,isPrivate`: public, main, ADMIN. This is broad ambient authority, not least privilege.
- `gh api repos/rozputnii/ai-usage-app/branches/main/protection`: HTTP 404, Branch not protected. No remote mutation is authorized or performed. Local guards cannot supply independent remote protection.
- `git var GIT_AUTHOR_IDENT`: initially failed (missing identity). Owner supplied `rozputnii` and `daniil.rozputnii@gmail.com`, authorizing repository-local configuration. Feature branch: `feature/AIU-001-omp-bootstrap`.
- Owner explicitly authorized replacing the existing license with MIT and installing a user-local official .NET 10 SDK. No machine-wide settings, elevation or paid resources.
- Windows: x64, build 26200.9445, DisplayVersion 25H2. Windows registry ProductName returns legacy `Windows 10 Pro`; build and workstation identify Windows 11. This discrepancy is not hidden.
- `dotnet --info`: host 10.0.12, .NET and WindowsDesktop runtimes 9.0.5 and 10.0.12; initially no SDK. Alternate standard SDK roots and Windows Kits Include directory absent. Windows SDK remains an AIU-002 prerequisite, not a reason to change the stack.
- Official .NET metadata resolved SDK 10.0.401 at `https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json`. The win-x64 ZIP was SHA-512 verified before extraction: `24b670ad3d923bfcf47df6c3b034152398b42f6dbc388e10d783aee1cfb5e5817d399fc0ae2a12cfa822a55e61d34830ccb15c50ef6efee437ab874bb7c79430`. Installation is user-local under `.dotnet/ai-usage-sdk`; PATH unchanged.

## OMP compatibility

- `omp --help` and installed package metadata: 18.1.18. GitHub latest release API returned stable v18.1.18, not a prerelease. No upgrade or historical pin applied.
- Installed `src/config/settings-schema.ts`, `src/advisor/config.ts`, extension types and native task documentation are the API references. Public provenance: `https://github.com/can1357/oh-my-pi/tree/v18.1.18`.
- `omp --profile ai-usage config path` resolved the named profile. Only specific nonsecret settings were queried. No full settings/auth dump.
- Profile-local roles are configured: default/plan GPT-6 Astra; slow/advisor Claude Opus 5; smol Gemini 3.8 Flash. The model catalog lists these selectors. Catalog membership is NOT proof of authenticated inference; runtime checks are recorded separately.
- Verified settings: `memory.backend` enum; `autolearn.enabled`, `plan.enabled`, `plan.defaultOnStartup`, `advisor.enabled`, `async.enabled`, `task.batch`, `task.isolation.enabled`, `task.isolation.apply`, `compaction.enabled`, `compaction.asyncEnabled` booleans; `task.maxConcurrency` number; `task.isolation.merge` enum; `advisor.syncBacklog` enum requiring string `"1"`.
- Advisor tools are NOT an `advisor.tools` setting. Installed WATCHDOG YAML schema supports explicit `tools: [read, grep, glob]`. Concrete model selection stays in the ai-usage profile.
- Native isolation is explicit per spawn. `task.isolation.apply: false` retains patches without automatic parent integration; `merge: patch`. Isolation and textual ownership are not an OS sandbox.
- No language server configured at preflight.

## Baseline adoption

The full embedded goal was read. A bounded record parser found all 28 records: 25 canonical docs and three passive OMP seeds. All 25 docs matched their embedded text exactly immediately after adoption. Accepted headings contain every sequential D-001 through D-176 exactly once. The original handoff directory is preserved and ignored as immutable, non-authoritative input; only `docs/backlog.md` is maintained.

## Runtime verification

Pending implementation. See `../specs/AIU-001-omp-bootstrap/verification.md` for criterion verdicts; settings and documents alone do not prove the workflow.
