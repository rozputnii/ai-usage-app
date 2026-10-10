# T-057 verification

Code reference: `1f8e27a` and `682869d`, merged into `main` as `a655c31`. Environment: the
owner's Windows 11 Pro 10.0.26200 desktop, .NET 10, Release, desktop unlocked. Date:
2026-10-10. Run as one of three parallel workers (T-056, T-057, T-058) under the owner's
request of 2026-10-10; logs are in the worker worktree's git-ignored `.ai-usage-local/T-057/`.

## Checks

Merge gate on the merged tree `a655c31`:

| Check | Result |
| --- | --- |
| C4 Infrastructure suite | PASS, 919/919 (917 before, plus 2 new tests) |
| C5 Presentation suite | PASS, 330/330 |
| C7 App build (Release, unpackaged) | PASS, 0 warnings, 0 errors |
| C6 Diff check | PASS |
| C2 Document validation | PASS, 0 diagnostics |
| C3 Final document validation | PASS, 0 diagnostics |
| C8 Launch smoke | BLOCKED at the merge: the `(demo: True, comfortable: False)` case failed three times in the harness precondition `LedgerSmoke.Focus` ("Test window must own keyboard input") because another window covered the test window; the other two cases passed. This was the pre-existing harness defect T-058 fixed. The change touches no Windows UI, tray, launch or lifetime code and is covered by unit tests, so it merged under the blocked-smoke rule (CONTRIBUTING, Git flow). C8 then passed 3/3 on the T-058 merged tree `3fb85f4`, which contains this change (see the T-058 verification record). |

## Acceptance results

| AC | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | The existing refusal tests in `BudgetStorageSafetyTests` are unchanged and pass. Two new tests pass: `RedirectedEntryCountedByTheCapRefusesWritesToOtherSeries` and `RedirectedAncestorOfTheOwnedDirectoryRefusesWrites`. The reviewer's mutation run (per-entry check removed) made the first one fail. The reviewer also checked with a scratch program that the shared `ProviderStatePaths.CheckAttributes` rule gives the same verdict as before for each entry kind: file, hard link, directory, junction, symlink to a file or a directory, and a vanished entry. |
| AC-02 | PASS | The assertions are unchanged. The test alone, Release, warm runs: base `1ce4a39` 9.6-10.8 s, after 2.5-2.8 s (implementer and reviewer measured separately). That is about 4x faster but above the backlog's 1-2 s target; the remaining time is in `WriteAsync`'s per-write chain checks and write-through flushing, which are out of scope. |
| AC-03 | PASS | C4, C5, C7, C6, C2 and C3 above. C8 was BLOCKED at this merge (see Checks) and passed 3/3 on `3fb85f4`. |

## Review

`aiu-reviewer` did both the per-task review and the focused T3 review (owned store,
reparse-point boundary), on `1ce4a39..1f8e27a`. Verdict: approve, with 0 Critical, 0 Important
and 4 Minor findings. The reviewer confirmed the tier is T3. The scan adds no window between
check and use: every mutation in `WriteAsync` re-checks the chain itself, and `WriteAsync` is
byte-identical to base. `/security-review` saw an empty diff, so the reviewer applied its
criteria by hand and found nothing at confidence 8 or higher.

- Minor 1, fixed in `682869d`: the inline attribute rule duplicated `CheckFile`. Both now use
  the shared `ProviderStatePaths.CheckAttributes`.
- Minor 2, left open: no test pins the ReparsePoint bit on its own; for example, a symlink to a
  file would be counted, not refused, if only the Directory bit were checked. This gap was already
  there at base, and the change does not widen it.
- Minor 3, fixed in `682869d`: the spec's Boundary wording overstated what the ancestor test
  proves. A no-op line was also removed from that test.
- Minor 4, fixed by this record.

## Not run

| Item | Reason |
| --- | --- |
| Owner check in the installed app | None needed: the change has no live-provider or interactive behaviour. |
