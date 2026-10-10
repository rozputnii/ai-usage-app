---
id: T-057
type: feature
status: implemented
goal: G-003
scope_version: 1
approval_basis: owner selection, 2026-10-10
---
# Cheaper store-growth cap check

## Problem

Before each budget store write, `BudgetJsonFile.EnsureCapacity` scans the owned `budget`
directory to enforce the store-growth cap (at most 256 series and 512 MiB). For every entry it
called `Check(path)`, which walks the owned directory and every one of its ancestors again
before it checks the entry, then read the entry's length with another query. A write into a
directory of n entries, with d directories from the owned one up to the drive root, made about
n x (d + 2) attribute queries. The cap test
`LocalBudgetStoreTests.ExcessSeriesCannotGrowTheOwnedStoreIndefinitely` writes 256 series
in one append, so it runs about 33k entry checks, each with its own ancestor walk. The
workflow audit (OD-36) measured about 9.7 s of the test's 12.5 s in `EnsureCapacity`; it is
the Infrastructure suite's critical path, and real writes pay the same cost per entry.

## Requirements

- R-01: The cap scan checks the owned directory and its ancestors for reparse points once per
  write, before it enumerates, and then checks each entry once.
- R-02: Each entry's check uses one attribute query, the same one that reads its length. An
  entry that is a reparse point or a directory refuses the write with the same
  `ProviderException(RecoveryRequired)` as `ProviderStatePaths.CheckFile`. An entry that has
  disappeared since enumeration fails as before, when its length is read.
- R-03: Everything else is unchanged: the stored file formats, the cap values, the checks
  around staging, replacing and deleting in `WriteAsync`, and the refusal kinds and messages.

## Acceptance criteria

- AC-01: Every existing reparse-point refusal keeps its tests and still fails closed. A
  reparse point on the owned directory, on an ancestor of it, or on any entry the cap
  counts refuses the write. New tests cover the two cases that had no test: an ancestor of
  the owned directory that is a junction (`RedirectedAncestorOfTheOwnedDirectoryRefusesWrites`)
  and a junction among the counted entries next to the file being written
  (`RedirectedEntryCountedByTheCapRefusesWritesToOtherSeries`).
- AC-02: `ExcessSeriesCannotGrowTheOwnedStoreIndefinitely` keeps its assertions and gets
  markedly faster. Before and after timings are measured the same way: the test alone, Release,
  `-method`, warm runs.
- AC-03: C4, C5, C7 (Release, 0 warnings), C6 and C2 pass, and C8 passes before the push to
  `main`.

## Boundary

The reparse-point boundary is not weakened, for these reasons:

- The scan only reads metadata (attributes and lengths). It creates, replaces and deletes
  nothing, so the earlier per-entry ancestor walks did not protect a mutation: they only
  repeated the same check of the same directories while the scan was running.
- The single walk still covers every directory the old walks covered: the owned directory
  and all its ancestors, starting at the scan, before any entry is read. No layout that was
  refused before is now accepted. A redirected owned directory or ancestor still fails the
  write. The new ancestor test pins this for `BudgetJsonFile` directly, without the store's
  lease (which checks the chain too); it proves that the write is refused, by the scan or by
  the chain check in `WriteAsync`, not which of the two fires first.
- Every mutation keeps its own checks. `WriteAsync` checks the directory chain, the target
  and the stage file again right before it creates the stage, before the replacing move and
  before it deletes a stage. A redirection that appears during the scan is refused there,
  exactly as before.
- Each entry is still checked: the attribute query that reads its length also supplies the
  reparse-point and directory bits. This is the same Windows query, with the same answer,
  that `ProviderStatePaths.CheckFile` makes, and both apply the same rule,
  `ProviderStatePaths.CheckAttributes`, so an entry that was refused before is still refused.

## Measurement

The test alone, built in Release, run with `dotnet run --project
tests/windows/AiUsage.Infrastructure.Tests -c Release --no-build -- -noLogo -method
"AiUsage.Infrastructure.Tests.LocalBudgetStoreTests.ExcessSeriesCannotGrowTheOwnedStoreIndefinitely"`
on the owner's desktop on 2026-10-10. The test's directory chain has 8 directories, so the
scan's attribute queries per entry drop from 10 to 1 (about 330k to 33k for the test). The
first run after a build is cold; the others are warm.

| Build | Cold | Warm |
| --- | --- | --- |
| Base `1ce4a39` | 11.2 s | 9.6 s, 9.7 s |
| T-057 | 4.0 s | 2.6 s, 2.8 s, 2.7 s |

By the OD-36 split (about 2.8 s of the test outside `EnsureCapacity`), the remaining time is
mostly outside the cap scan: 256 write-through writes and the fixed path checks around each of
them. This is an estimate, not a profile of the new build.

## Out of scope

- The checks in `WriteAsync`, `ReadAsync`, `OwnedPaths` and `ProviderStatePaths`, and the
  per-write cost of write-through flushing.
- Cap values, file names, formats and the store layout.
