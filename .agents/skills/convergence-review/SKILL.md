---
name: convergence-review
description: Independent review of a diff against its acceptance criteria and safety boundaries. Use when CONTRIBUTING requires a per-task, whole-feature or T3 focused review, or when the owner asks for a review.
---
# convergence-review

Use only when CONTRIBUTING.md or the owner requires review. Freeze a code reference and relevant specification/evidence; remain read-only with fresh context and no implementation transcript or unrelated memory. Review acceptance and material safety boundaries. Report PASS, FAIL, NOT_RUN or BLOCKED with path/symbol evidence; zero findings is valid. Unresolved material defects block integration. Record unavailable required review honestly, deduplicate minor findings and use targeted verification after fixes, without a repeated full-review loop.

Report observed checks and limitations honestly. Preserve secrets and opaque user/provider data.
