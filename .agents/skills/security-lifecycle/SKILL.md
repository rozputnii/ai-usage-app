---
name: security-lifecycle
description: Use when inspecting credential or durable-state safety boundaries
---
# security-lifecycle

Read the relevant scope in docs/platforms/windows/security-and-lifecycle.md. Check DPAPI CurrentUser, app-owned storage, owned-root cleanup, last-good recovery and forward migrations. Never mutate source CLI stores or invent atomicity across files, databases and provider rotation. Report concrete boundary defects without redesigning unrelated code. Follow CONTRIBUTING.md for required focused review.

Report observed checks and limitations honestly. Preserve secrets and opaque user/provider data.
