# Contributing

Read the [accepted decisions](docs/decisions/accepted.md), [backlog](docs/backlog.md) and relevant specification before proposing changes. Issues are intake, not automatic implementation authorization. Keep changes scoped to an identified AIU and feature branch.

## Contributions

- External pull requests are welcome under the MIT license and Developer Certificate of Origin (DCO).
- Sign off commits with your own real, authorized Git identity. `git commit -s` records a DCO sign-off; it is not a cryptographic signature. Never fabricate another person's identity or sign-off.
- Keep authored documentation, code comments, prompts, skills and durable task records in English. Quote opaque user input only when necessary and clearly delimited.
- Update affected specifications, acceptance evidence and task records. Do not mark work done from a worker claim alone.
- Keep credentials, local OMP profiles/sessions, personal provider data and generated build output out of Git.

Run the [local checks](README.md#local-checks). Owned-code warnings are errors. Tests should defend observable behavior and meaningful failure boundaries, not implementation wiring. Product UI changes require the actual applicable Windows UI smoke, not just compilation; no such product UI exists in AIU-001.

## Review and integration

The primary owns canonical state and integration. Native write workers use explicit, non-overlapping ownership and isolated patch return without automatic application. Review their actual patches before integration, then run relevant checks on the integrated tree.

Owner amendment (2026-09-13): ordinary development PRs require primary diff/acceptance review, not a mandatory independent full convergence review. Material authentication, secret handling, destructive data lifecycle or privilege changes need focused independent review; public release approval or an explicit owner request still needs the full review. Independent reviewers remain read-only, memory-off and fresh-context. Zero findings is valid; concrete fixes get targeted verification, not another full-review loop.

Current PR CI runs validator regressions (with compilation/analyzers), canonical document validation and workflow/patch regressions. Formatting remains a local check, not a PR gate. New commits cancel obsolete CI runs of the same PR. Do not skip failed behavioral checks merely for speed.

Main protection/rulesets and enforced required checks are explicitly deferred at low priority in AIU-026. Their absence is not a prerequisite or blocker to current development PRs; this accepts the risk that GitHub does not enforce the policy. Relevant checks must still match the proposed head. Feature branches, DCO, no direct main/force push, remote-action authorization and protected production-release permissions remain unchanged. This policy change itself does not push, open or merge a PR.
