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

Use one independent full convergence review with read-only tools, memory disabled and no primary conversation history. Zero findings is valid. Fix material findings and rerun targeted checks rather than starting an automatic full-review loop.

Remote checks and review must apply to the exact proposed head before merge. The bootstrap did not configure main protection or execute GitHub CI. Do not treat local checks, broad repository administration rights or a workflow file as permission to bypass remote controls.
