# Agent-neutral migration verification

Executed locally on 2026-09-14, completed checks at approximately 17:11 Europe/Lisbon. Scope: the owner-approved [migration plan](agent-neutral-migration-plan.md), executed sequentially by one integrator. No product feature was selected.

## Baseline and preservation

Baseline HEAD: `1e4f0ced764132027acc13f7d58d7957d66242be`, initially `main...origin/main`. Created `codex/agent-neutral-workflow` from that checkout before implementation. HEAD is unchanged; the result is an uncommitted working diff, not a commit or isolated worktree.

Initial `git status --short --branch` recorded modified `.omp/AGENTS.md`, `.omp/RULES.md`, `docs/constitution.md`, `docs/decisions/accepted.md`, `tools/AiUsage.ProjectValidation/ProjectValidator.cs`, `tests/AiUsage.ProjectValidation.Tests/ValidatorTests.cs`, `src/windows/AiUsage.Windows/App.xaml.cs` and `tests/windows/AiUsage.Windows.Tests/PackageSmoke.cs`. Untracked work was `AGENTS.md`, `.agents/`, `.codex/`, the migration plan and `Videos - Shortcut.lnk`. Initial `git diff --stat`: 8 tracked files, 151 insertions and 19 deletions. Migration-target diffs were inspected before editing.

The archive contains exactly the 20 explicitly listed authored sources under `docs/archive/omp/<original repository-relative path>`. Copy-time SHA-256 comparisons passed before removing the active originals; the OMP entry was copied before replacement. This preserves the pre-existing entry/rules edits and authored local configuration bytes. Empty retired authored directories were removed non-recursively only after resolving their paths inside the repository. Runtime/session/credential files were not copied, read or removed. Protective `.gitignore` entries are unchanged.

The application and package-smoke diffs remain pre-existing work: `App.xaml.cs` has 16 additions/8 deletions and `PackageSmoke.cs` has 21 additions/0 deletions, matching the inspected starting changes. They implement an unfinished close-to-tray change outside this migration. No migration edit touched application code, product tests or packaging behavior. The shortcut and original migration plan remain untouched.

No staging or commit was performed. Migration-owned changes overlap the pre-existing constitution, decisions, validator/tests, root instructions and shared guidance. Retired fingerprint/language behavior was intentionally replaced under the approved plan; the metadata and path-safety substance of the earlier validator changes remains. These overlapping files must not be bundled into a commit under the assumption that the checkout was initially clean.

## Observed commands

Windows; installed user-local SDK `10.0.401`, matching `global.json`. Commands below used `& "$HOME/.dotnet/ai-usage-sdk/dotnet.exe"` in place of `dotnet`. Existing restored assets sufficed; no installation, package restore, external network, model or credential access was used for verification.

| Check | Verdict | Actual result |
| --- | --- | --- |
| Baseline `dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo` | PASS | 62 tests; 0 errors, failures, skips or not-run; exit 0 |
| Baseline `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json` | PASS | `valid: true`, no diagnostics; exit 0 |
| Required test-first transition | FAIL, expected | All three new ordinary-work cases rejected by the old validator: missing OMP counterpart, missing tasks, missing runtime metadata; 65 total, 3 failures |
| Expanded contract tests before implementation | FAIL, expected | 72 total, 12 failures exposing the new scan, evidence and neutral ownership contracts |
| Evidence-path review regression before correction | FAIL, expected | Four unsafe full-path cases were accepted; 78 total, 4 failures |
| Final `dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo` | PASS | 78 tests; 0 errors, failures, skips or not-run; exit 0. Includes actual reparse-boundary tests |
| `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo` | PASS | 72 tests; 0 errors, failures, skips or not-run; exit 0 |
| Current-tree document validation with the command above | PASS | `valid: true`, no diagnostics; exit 0; repeated after documentation reconciliation and after this report |
| `git diff --check` | PASS | Exit 0, no whitespace diagnostics after correcting added end-of-file blank lines |

The product regression suite was inspected before enabling CI: `CodexTestServer` is a synthetic `HttpMessageHandler`; the actual HTTP callback tests use loopback endpoints, synthetic codes and state. Credential/session tests use synthetic grants and owned temporary directories; DPAPI checks ran on Windows. Provider URLs in request assertions and synthetic JWT claims do not cause external requests. No real grant or browser consent was exercised.

One auxiliary hash-inventory invocation failed because PowerShell required an array for multiple paths. The corrected invocation succeeded; this was not a product or validation failure.

## Contract review and walkthrough

PASS by local document/diff inspection:

- Root `AGENTS.md` leads to `CONTRIBUTING.md`, the single current procedure and Git policy. A short written plan suffices; no named mode, runtime, plugin, Advisor or model family is required. Current owner authority governs external actions and main integration; old permissions do not carry forward.
- From AGENTS alone, the reading map reaches G-002, the backlog and completed AIU-002/003/004 evidence without visiting `.omp` or the archive. AIU-005 remains research-needed; no next feature or automatic execution is selected. Internal next-action records belong in the selected tasks document. AIU-002/003 stale handoffs are preserved as history in the superseded register, with concise current handoffs.
- Goals hold outcomes and membership, not a session permission ledger. Spec lifecycle labels remain, while `execution_status` and repeated current-progress prose were removed from AIU-002/003/004 specs. AIU-004 cache/tray scope no longer says it is still open. Its acceptance criteria and retained verification were not weakened or relabeled as new live success.
- A reference scan also found workflow prescriptions in product vision and Windows release policy. Those references were reconciled with the approved target; product intent, localization (D-120), release authority and provider provenance remain intact. AIU-017/025 stay deferred with their IDs and statuses unchanged.
- Shared guidance has one active copy in `.agents/skills`, valid name/description metadata and matching directories. Provider evidence, credential lifecycle and scoped-review boundaries are retained. Native discovery is optional.
- The validator accepts absent tasks and minimal sequential tasks, still checks optional designs, and requires safe existing completion evidence, completed task dependencies and valid AC references. Explicit write workers retain isolation/integration checks; parallel work requires ownership metadata. Unsafe full evidence paths cannot be hidden by a safe suffix.
- Authored scanning is restricted to docs excluding archive, shared skills and named root/adapters. Archive reparse boundaries are checked before skipping contents. Obsolete archive specs do not become active work; adapter Markdown links participate in validation. No language detector or fingerprint pairing remains.
- `CLAUDE.md` contains only `@AGENTS.md`; Copilot and OMP adapters contain exactly the planned root-pointer sentence. All three point to the same existing root file. Only `.omp/AGENTS.md` remains as active authored OMP configuration; the named extension/config/WATCHDOG/launcher/test paths are absent.
- The CI diff only removes Bun setup/workflow regressions and adds the deterministic Windows product regression command. Action pins, permissions, concurrency, unsigned package build, routing reproduction and smoke-harness publish are unchanged. Build/publish is not an interactive smoke pass.
- Primary integrated review found no unresolved migration defect. Review stayed sequential; no independent agent/client compliance test is claimed.

Reference review used `rg -n 'OMP|omp|/work|canonical_sha256|Advisor|approval|English|language' AGENTS.md CLAUDE.md CONTRIBUTING.md README.md SECURITY.md .agents .github docs`, followed by scoped inspection. Matches fall into: current neutral authority/retirement references; clearly labeled historical bootstrap and superseded decisions; unchanged product localization and provider-source provenance; or the migration plan/report. Historical OMP commands remain evidence only. D-176 is retained only as a superseded historical decision. No active repository language prescription remains and none was added.

## Code reference

SHA-256 of reviewed working files, in addition to the baseline HEAD above:

| File | SHA-256 |
| --- | --- |
| `AGENTS.md` | `823745034FB8D218C4D127D35BE762C854A530B593910C776E16EBBA52E123DC` |
| `CONTRIBUTING.md` | `10BB5AF58175B8E0762475E4CACC1D389F597F9CB3B21A645C60083BF6384E7D` |
| `tools/AiUsage.ProjectValidation/ProjectValidator.cs` | `EC2DA5B0411C1EC0B4DB06CE24135A7D4DF8542849539F6AC767DC9EFF6A9AC5` |
| `tests/AiUsage.ProjectValidation.Tests/ValidatorTests.cs` | `ADFC1216D12577B025256B181E1E2FCF5B16DC477A2DB8EE7B333877D7AFD55A` |
| `.github/workflows/validation.yml` | `D98AF0B05FA6F23002DD07D9215BA11EDDA6AAC8F89615BA50E51201F7BDED5F` |

## Limitations and handoff

- NOT_RUN: actual Claude, Copilot and OMP client adapter loading, skill behavioral pressure tests and independent client review. Pointer inspection and validator tests do not prove client loading or compliance.
- NOT_RUN: remote GitHub Actions, push, merge, release, package rebuild and interactive guest smoke for this migration. The unchanged product scope did not require a new build or UI run.
- NOT_RUN: packaged live sign-in, real-grant resume, live stale-cache behavior and close-to-tray verification. Earlier console live success and guest screenshots remain historical evidence only. The pre-existing close-to-tray edits are not verified by workflow tests; CR-AIU-004-01 is not closed here.
- No offline prerequisite is BLOCKED and no unresolved local check is FAIL. Expected test-first failures and corrected whitespace diagnostics above are distinct from final results.
- Next action: owner review of the uncommitted migration diff and its overlap with pre-existing work. Do not select a product feature, stage mixed changes, push or merge from this record.

## Owner-authorized commit and push follow-up

After the local handoff, the owner explicitly requested committing and pushing this migration. That current request authorizes the task-branch publication, superseding the earlier uncommitted handoff disposition for this action only. The commit includes the reconciled policy/validator work and its original approved plan; the unrelated application, package-smoke and shortcut changes remain outside the commit. No main integration or merge is requested. Remote CI and client loading remain NOT_RUN; publication alone does not verify them.
