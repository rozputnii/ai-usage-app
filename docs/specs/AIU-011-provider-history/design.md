---
id: AIU-011
type: design
status: implementing
goal: G-003
scope_version: 4
---
# Provider history implementation design

Owner selected implementation on 2026-09-22 after reviewing broader feasibility.
Existing authorization and deferred local history remain binding. Execute sequentially
in the current main checkout under CONTRIBUTING, using executing-plans and test-driven
development. The implementation permission covers routine design and verification.

Core owns inclusive UTC date ranges, native metric rows, independent report outcomes and
credential-free history ports. Infrastructure implements Codex analytics and Copilot
personal billing reports on existing hardened HTTP clients and session gates. History
never exposes credentials, imports another application's grant or changes permissions.
Optional report denial does not disconnect a working quota session. Refresh-token
rotation must use the existing durable lease and persist before the next provider read.

Codex retrieves daily consumption, credit events, personal message/activity, plugin and
skill reports, plus applicable workspace breakdowns. Each report preserves its own unit,
dimensions and date semantics; partial availability is explicit. Copilot resolves the
current numeric identity and login before reading personal credit/request period reports.
It queries calendar-month intersections; daily splits are requested only for partial
months. It never represents a monthly aggregate as a daily point. Both providers honor
throttling and cancellation, with bounded date ranges and response sizes.

The live adapter owns scheduling alongside quota/connection/disconnect work and drains
history before exit. The History page loads all connected accounts automatically, or the
account selected from a card. It defaults to 30 UTC days, with optional 7/90/365-day and
custom dates (maximum two years per query). Native values and dimensions appear in
report tables; no quota-percentage graph is fabricated. Cached results are memory-only,
marked with retrieval time and cleared on disconnect/reconnect. Failed refresh retains
same-account data as stale. Navigating away cancels work. Clock ticks do not fetch history.
Demo history uses explicit synthetic provider rows; legacy local samples remain demo-only.

Claude and Antigravity expose an unsupported-history state until a compatible remote
contract is established. There is no attempt to bypass access failure using another
credential source. A safe console history command uses an explicitly selected app-owned
provider directory and prints statuses/counts only for live verification.

## Implementation plan

### T-01 - History contracts and transport

- Add Core `HistoryRange`, `HistoryValue`, `HistoryReport`, `ProviderHistoryResult`,
  `IProviderHistorySession`, and `IProviderHistorySource` under `Core/History`.
- Add Infrastructure Codex/Copilot history clients and explicit parsers. Tests assert
  actual requests, date scope, signed credits, missing numeric values, preserved opaque
  dimensions, malformed payload rejection, permission errors and Retry-After.
- Red/green command: Infrastructure regression executable, filtered by history tests.

### T-02 - Existing sessions and scheduling

- Add session history operations under existing per-provider gates/state leases and
  register clients through hardened DI. Extend LiveUsageSource with history scheduling,
  cancellation, disconnect and shutdown draining. No credential leaves Infrastructure.
- Add a sanitized console probe using the same product session. Test no-grant behavior,
  account mismatch, persisted refresh rotation and history/connection serialization.
- Run an authorized read with existing AI Usage credentials when available; record
  failure or absence honestly rather than sign in automatically.

### T-03 - Automatic provider-history presentation

- Add provider History view model and replace the page's local-observation controls with
  account sections, range presets, custom dates, refresh and report rows. Register live
  and synthetic sources and enable navigation. Keep quota sparklines' contracts separate.
- Presentation tests exercise automatic loads, independent results, no clock polling,
  coalescing/cancellation, account invalidation, missing fields and stale refresh.
- Update interactive smoke to require History in product mode and synthetic report data
  in demo mode. No initial Load action or required filter selection.

### T-04 - Integrated acceptance

- Run Infrastructure and Presentation suites, document validation and diff checks.
- Build unpackaged Windows and unsigned package; run actual isolated product/demo UI
  smoke. Record live-provider results separately from synthetic tests and screenshots.
- Focused independent review covers new credential-consuming paths, session races and
  honest native metric presentation. Integrate fixes and targeted verification.
- Update provider/spec/backlog/evidence, commit and push to main. Do not mark AC-02 or
  Windows checks PASS unless the corresponding real result was observed.

## Review focus

Cross-process grant replacement, cancellation during token rotation, delayed responses
after disconnect, successful empty versus denied/malformed reports, and absent provider
units/dates must retain their distinct meanings. A changing quota snapshot must never
trigger historical traffic. A provider timestamp or label is data, never a network target.
