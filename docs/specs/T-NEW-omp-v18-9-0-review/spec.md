---
id: T-NEW
type: feature
status: implemented
goal: G-003
scope_version: 1
approval_basis: owner decision, 2026-10-11 (request and scope decisions in the task brief)
---
# Provider sources reviewed against OMP v18.9.0

## Problem

Each provider record pins the oh-my-pi (OMP) commit whose sign-in, refresh and quota code AI
Usage followed: Codex at v18.1.19, Claude at v18.1.22 (and v18.3.2 for the T-034 money audit),
Copilot and Antigravity at v18.2.6. Upstream has moved on, partly because providers changed. A
change that a provider forced upstream can break or degrade our integration without notice.

## Requirements

- R-01: For each provider, review the upstream commits from its pin to tag `v18.9.0`
  (`eec114a9226da698ebc015fd8996a8ca3bc4625c`) on the files its record names, plus the files they
  moved or were split into, and decide each change: adopted, backlog or not applicable.
- R-02: Adopt compatibility changes that upstream made because a provider changed: endpoints,
  headers, client version or User-Agent strings, refresh behavior, request bodies and response
  fields already read. Never vendor a third party's OAuth client id or secret.
- R-03: New capabilities (quota fields, windows, plans, providers or flows) become `T-NEW`
  backlog items with status research-needed and their upstream evidence, not code.
- R-04: Update each provider record's source references and `source_verified_at`; keep
  `live_verified_at`. A provider with no relevant change says so with the new reference.

## Acceptance criteria

- AC-01: For each provider, the upstream changes since its pin that touch its sign-in, refresh or
  quota path are listed in the verification record with commit, file and function, each with a
  decision; each provider record's source references and `source_verified_at` are updated and
  `live_verified_at` is unchanged.
- AC-02: Adopted changes are implemented red to green with tests against fixtures derived from
  the upstream source, and C4, C5, C7, C2 and C6 pass.
- AC-03: New capabilities appear only as `T-NEW` backlog items, not as code.
- AC-04: A provider with no relevant change has a record saying so with the new reference, and
  no code change.

## Out of scope

- Credential reads, sign-in, live provider calls and live verification (pending owner checks,
  R-190).
- OMP behavior that serves its own credential pool, model routing or inference (ranking, block
  healing, plan gates, rate-limit headers from inference responses).
- The official Codex repository pins in codex.md, which this review does not move.
