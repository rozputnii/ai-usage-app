---
id: AIU-010
schema_version: 1
---
# AIU-010 staged execution

The primary owns shared contracts, canonical documents and integration. This file is the single progress and resume record for the mock-first frontend delivery defined by [frontend-brief.md](frontend-brief.md) and [frontend-plan.md](frontend-plan.md). After interruption or context compaction, read the brief, the plan and this file before editing. No worker has been launched.

### T-01 - Prepare current and planned UI contract
- status: done
- depends_on: []
- acceptance: AC-01, AC-02, AC-08, AC-09
- evidence: docs/specs/AIU-010-ui-ux/verification.md

Prepare the inventory, semantic contract, deterministic scenario catalog, agent prompts and acceptance gates. Validate documents and fixtures and review the scoped diff.

### T-02 - Claude Design visual system and complete mockup
- status: done
- depends_on: [T-01]
- acceptance: AC-03, AC-05, AC-06
- evidence: docs/specs/AIU-010-ui-ux/frontend-brief.md

The owner identified the selected Claude Design project and file in the brief (owner amendment 2026-09-15). The owner's selection counts as design approval. The artifact's content and revision are verified in T-03, not here.

### T-03 - Import selected design and record revision
- status: done
- depends_on: [T-02]
- acceptance: AC-03, AC-05, AC-06
- evidence: docs/specs/AIU-010-ui-ux/design-reference/README.md

Imported `AI Usage App.dc.html`, `support.js` and the rest of the project through the claude_design MCP. The local snapshot with revision and hashes is in `design-reference/`. All designed surfaces are enumerated in the plan's coverage matrix, and the design decisions that differ from earlier documents (D1–D7) and the design gaps are recorded there.

### T-04 - Presentation contracts, mock services and application composition
- status: ready
- depends_on: [T-02]
- acceptance: AC-02, AC-06, AC-10, AC-11
- evidence: not-run

Plan phase 1, including the design-required contract extensions. All code stays inside `AiUsage.Windows` under `Features/<Feature>/`, within the existing three-project architecture. Adding a project requires a demonstrated need and owner approval first.

### T-05 - Theme resources, reusable controls, motion and navigation shell
- status: pending
- depends_on: [T-03, T-04]
- acceptance: AC-03, AC-05, AC-10, AC-11
- evidence: not-run

Plan phase 2.

### T-06 - Overview, accounts, quotas, connection and tray
- status: pending
- depends_on: [T-05]
- acceptance: AC-04, AC-05, AC-06, AC-10
- evidence: not-run

Plan phase 3: S01, S02, S03, S07 and F01–F08, F15.

### T-07 - History, appearance, monitoring and notification settings
- status: pending
- depends_on: [T-05]
- acceptance: AC-04, AC-05, AC-06, AC-10
- evidence: not-run

Plan phase 4: S04, S05, S06 and F09, F10.

### T-08 - CLI import, diagnostics, data management, recovery and updates
- status: pending
- depends_on: [T-05]
- acceptance: AC-04, AC-08, AC-10
- evidence: not-run

Plan phase 5: S08–S12 and F11–F14.

### T-09 - Scenario coverage, Windows verification and backend handoff
- status: pending
- depends_on: [T-06, T-07, T-08]
- acceptance: AC-03, AC-04, AC-05, AC-06, AC-09, AC-10, AC-11
- evidence: not-run

Plan phase 6. Actual Windows screenshots in Light and Dark, keyboard, scaling, tray and Exit checks, coverage matrix evidence, verification.md record and adapter handoff. Commit and push the task branch after verification; do not merge.

### T-10 - Codex available-service integration
- status: pending
- depends_on: [T-09]
- acceptance: AC-06, AC-07, AC-08
- evidence: not-run

Codex maps existing workflows onto the presentation adapter interfaces, keeps future services unavailable in product mode and adds mapping/capability regressions. If durable-state or credential boundaries change, use security-lifecycle and focused review.

### T-11 - Integrated Windows and visual acceptance
- status: pending
- depends_on: [T-10]
- acceptance: AC-03, AC-04, AC-05, AC-06, AC-07, AC-08, AC-09
- evidence: not-run

Run required regressions/build and actual Windows UI scenarios on the integrated candidate. Record owner visual acceptance separately from deterministic checks. Do not mark future backend AIUs done based on demo success.

## Handoff

Base: `744e4e0` (identical to `codex/aiu-010-design-handoff`). Branch: `codex/aiu-010-mock-frontend`; checkout was clean at start. No application source has changed.

Completed 2026-09-15 (preparation phase):
- Saved the owner brief verbatim as frontend-brief.md.
- Recorded the owner amendment in spec.md (scope_version 2, AC-10, AC-11), design.md and handoffs.md. The Claude Code prompt in handoffs.md is marked superseded.
- Wrote frontend-plan.md with architecture, adapter interfaces, dependencies, phases, commands and the coverage matrix. Every evidence cell is `not-run`.
- Observed host facts: SDK 10.0.401, Windows App Runtime 2.4.0.0 installed, existing `AiUsage.Dev 2026.9.1416.0` registration. Nothing was installed.
- First import attempt failed because design authorization was missing. After the owner ran `/design-login`, the import succeeded. The project "# AI Usage dashboard directions" has 11 paths. The prototype, `support.js`, the Design Specification, the Reference Views and the Directions study were read (none truncated) and saved with SHA-256 hashes in `design-reference/`. Design revision: "Quiet Editorial (1b) · revision 1 · 2026-09-15". `.thumbnail` and `uploads/` (copies of the package documents) were not saved.
- Surface inventory, the S mapping, design decisions D1–D7 and gaps are in frontend-plan.md. spec.md records the design precedence.
- Owner correction 2026-09-15: the proposed separate presentation project is withdrawn.
  - Contracts, view models, navigation and mock services live in `AiUsage.Windows/Features/<Feature>/`.
  - WinUI implementations go in `Platform/`, `Controls/`, `Themes/` and `Motion/`.
  - `AiUsage.Presentation.Tests` keeps compiling linked `Features/**/*.cs` sources, excluding `*.xaml.cs`.
  - The Windows csproj keeps its existing Core/Infrastructure references, and a source boundary test forbids their use in this delivery.
- Implementation has not started; it waits for the owner's `/goal`.

Open owner choices (not blockers; the implementation follows the design unless the owner overrides):
- D1: default thresholds `[50,80]` vs `[20,50]` used, and the dropped per-account overrides.
- D3: the Overview summary strip removed against S01's written counts.
- D4: glyphs in High Contrast.
- D6: provider glyph colours pending rights review.
- The remaining items in specification §13.

Risks:
- Unpackaged launch (`-p:WindowsPackageType=None`) for host screenshots is untested. If it fails, registering the dev package on the host needs owner authorization.
- No new package is currently required. Any later addition needs network restore.

Check results (preparation):
- PASS: `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json` returned `{"valid":true,"diagnostics":[]}` (exit 0).
- PASS: `git diff --check` exit 0 with new files intent-added, after the design import (`design-reference/.gitattributes` keeps imported bytes verbatim). Validator re-run after the import also returned valid.
- An untracked root `install.cmd` appeared during the session. It was not created by this work and was left untouched.
- NOT_RUN: product regressions, builds and Windows UI checks, because no code has changed.

Exact next action (after owner `/goal`): T-04 step 1. Change `tests/windows/AiUsage.Presentation.Tests/AiUsage.Presentation.Tests.csproj` to compile `src/windows/AiUsage.Windows/Features/**/*.cs` excluding `**/*.xaml.cs`, and extend `DependencyBoundaryTests` with the Features-without-WinUI and no-Core/Infrastructure-usage rules. Then create the `Features/Presentation/` contracts.
