---
id: AIU-001
type: infrastructure-design
status: implemented
goal: G-001
scope_version: 2
---
# Bootstrap design

Historical evidence: the executable workflow and its instructions were retired on 2026-09-14. Runtime commands, permission records and language prescriptions below describe the past bootstrap only; current development follows CONTRIBUTING.md. Archived sources map to docs/archive/omp/<original path>. Provider-source provenance remains unchanged.

## Approach
Use native OMP profiles, configuration, rules, skills, tasks, Goal Mode, Advisor and extension APIs, plus a thin tested document/selection/handoff bridge. Do not require Codex-only execution skills or an external specification framework.

## File ownership
Adopt the embedded handoff documentation into canonical repository docs paths. Create .omp/AGENTS.md, RULES.md and WATCHDOG.md from the passive seeds; generate .omp/config.yml only after reading the installed schema. Add the minimum necessary skills and agent definitions using supported registration paths.

Implemented validator: tools/AiUsage.ProjectValidation, a small .NET 10 console project, with tests/AiUsage.ProjectValidation.Tests using xUnit v3 and no EF/UI dependencies. The native extension is .omp/extensions/ai-usage.ts, with bounded authorization and checked patch helpers in .omp/lib. tools/start-work.ts normalizes only the child Windows PATH to avoid a reproduced native MSYS ps isolation-setup hang; it does not patch OMP. Keep runtime credentials and personal configuration evidence out of Git.

## Bootstrap exception
A nonexistent validator cannot enforce its own creation. Use an explicitly scoped initial bootstrap with inspected diffs and concrete checks. Once the validator exists, enable the gate and test negative cases; do not leave a permanent bypass. Respect existing repository rules. Discover remote/author configuration before asking only for missing required values.

## Configuration
Inspect the installed stable schema. Keep concrete model mappings profile-local and deterministic project behavior in Git. Do not copy historical YAML blindly. Preserve approved YOLO autonomy without claiming regex containment. Explicitly restrict advisor/reviewer tools. Scope worker access and validate diffs while documenting ambient-access limits.

## Prove runtime before broad automation
Use disposable fixtures for ranking/cancellation, invalid dependency graphs, independent parallel workers, returned patches, fresh-session continuation, cumulative accounting, explicit pause and a clean zero-findings review. Do not use real provider login, release signing or production deployment for this proof. Check actual remote protections separately only when the repository is correctly identified and authorized.

## Simplicity budget
One small validator, one thin entrypoint, three to five compact reusable skills and only necessary custom agents. Every abstraction must serve a concrete acceptance criterion. Do not add a scheduler service, event-sourcing backend, vector memory or duplicate tasks state.

## End state
A fresh OMP session at the repository root opens the verified work interface, displays ranked work/active status and supports owner selection or bounded goal authorization. State survives checkpoints; peers remain isolated. Report only tested capabilities. Then present AIU-002 and AIU-003 without starting the entire product roadmap.

## Language
All authored runtime instructions, skills, command help, test descriptions and documentation are English. Owner-facing conversation may remain Ukrainian.
