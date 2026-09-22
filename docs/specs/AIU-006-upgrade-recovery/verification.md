# AIU-006 verification

Started 2026-09-22. Implementation and verification are in progress.

| Criterion | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | NOT_RUN | Real old/new package update pending. |
| AC-02 | PASS | Infrastructure state-machine regressions at 3c4e0ee. |
| AC-03 | NOT_RUN | Deterministic interruption tests PASS; real packaged interruption pending. |
| AC-04 | PASS | Deterministic explicit restore, corrupt backup/journal, newer layout and interrupted restore checks at 3c4e0ee. |
| AC-05 | PASS | Byte-preservation, secret exclusion, path junction and exclusive lease regressions at 3c4e0ee. |
| AC-06 | NOT_RUN | Live recovery adapter pending. |
| AC-07 | NOT_RUN | Checks and independent review pending. |

No host trust change, package installation, live sign-in or credential import has occurred.

## Local checks at 3c4e0ee

- Infrastructure: PASS, 329/329, including 22 maintenance cases. Initial stubs failed
  13/16; new corruption checks failed 3/20 before fixes; missing committed-file and
  empty-state checks failed 2/22 before fixes. Actual DPAPI and filesystem operations
  ran on Windows. Fault seams throw after publication boundaries; these do not prove
  physical power-loss durability.
- Presentation: PASS, 157/157. Three new recovery tests failed before integration.
  Full-suite execution exposed a stop/resume race; moving stop intent ahead of the
  asynchronous yield fixed it. Recovery copy expectations were updated because the
  old demo text falsely claimed specific migration steps and unchanged data on failure.
- Windows unpackaged build: PASS with no warnings/errors before the final empty-state
  hardening; final build is pending. Signed MSIX 2026.9.2220.0 built successfully.
  Host signature verification failed closed on the untrusted development certificate,
  as designed. Signed bytes are retained for guest-only validation; no host trust added.
- Canonical document validation and diff check: PASS before package-harness additions.
- Focused independent review: running against frozen 3c4e0ee through existing Claude
  Code, safe mode, fresh session, Read/Grep/Glob only, no transcript or private evidence.
  Repository-mandated Codex Luna is not in the available model override list; no other
  Codex model was substituted.

Local logs and screenshots are ignored under `.ai-usage-local/AIU-006` (early regression
logs use `.ai-usage-local/aiu006-*.log`). No provider account credentials are part of the
test fixtures or repository.
