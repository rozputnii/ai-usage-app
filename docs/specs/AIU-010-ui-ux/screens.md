# Screen and interaction inventory

All rows are required design and interactive demo scope. “Existing” means a current service building block, not that the redesigned screen already exists. Future provider methods and numerical examples are illustrative, not verified provider contracts.

| ID | Surface and required content | Required interactions/states | Real implementation owner / availability |
|---|---|---|---|
| S01 | Overview and account-card grid; counts, lowest known remaining, nearest reset with source/freshness, warning/critical/reauth counts | First-run Add account; empty filters; refresh all with partial failures; manual order; one/two/three-plus columns; no global usage percentage | AIU-010; individual Codex/Claude readings exist, aggregate presentation is new |
| S02 | Account detail: provider, custom label, plan, contexts, primary windows, expandable groups, native balances, last observation | Expand/collapse, change context, rename, move up/down and drag, hide only or hide+mute, show hidden/disconnected, refresh, reconnect, disconnect | AIU-010; readings exist; multi-account/context management and preference storage need adapters |
| S03 | Add account sheet: four-provider picker and supported connection methods | Browser handoff, waiting, manual-code fallback where supported, cancel, denied/expired/failed, connected-but-quota-unavailable; duplicate identity result | AIU-004/007 existing; AIU-008/009 future; do not assert future OAuth methods |
| S04 | History: account/context/group/window filters, sparkline and detailed chart, 24h/7d/30d/90d/1y/custom | Range validation; tooltip with timestamp/unit; loading, empty, disabled collection, gaps, resets, partial data; no line joining unknown gaps or summing reset periods | AIU-011; all synthetic |
| S05 | Appearance and layout settings | System default/Light/Dark, density proposal, always-on-top off, order/visibility, keyboard equivalents; immediate in-memory demo preview | AIU-010/desktop preferences; future durable adapter |
| S06 | Monitoring and notifications settings | Global/provider/account/limit inheritance; 25/10/0 remaining defaults; override/reset-to-inherited; quiet/OS-blocked preview; fresh crossing preview; power/metered reason; manual refresh retained | AIU-012; all policy simulation, existing manual refresh retained |
| S07 | Tray mini-dashboard and menu | All accounts/groups, reset/status, per-account/all refresh; no charts; single click popup, double click main, right-click Open/Refresh all/Settings/Exit; severity priority per D-126 | AIU-010; current tray Open/Exit and close-to-tray exist; expanded tray is new |
| S08 | CLI import under Add account / account management | Progressive synthetic discovery; no candidates, duplicate, unsupported, individual/bulk/re-import, per-candidate result, cancellation; explicit user initiation | AIU-005; no filesystem discovery or credential reading in this work |
| S09 | System Status | Build/version, provider and refresh status, storage/schema status, update status; synthetic diagnostic preview, health check progress/result, log preview | AIU-013; real diagnostics adapter deferred; health check never repairs |
| S10 | Data and privacy | History collection/retention; export contents/privacy preview; Replace import validation/confirmation; settings reset vs factory reset; delete-account-data separate from disconnect | AIU-011/013; all synthetic; preserve D-149–153 semantics |
| S11 | Recovery surface | Interrupted migration, newer unsupported schema, retry, restore checkpoint, diagnostics preview, data-folder action preview; success/failure | AIU-006/013; all synthetic, no automatic wipe or restore |
| S12 | Updates and compatibility | Checking/unavailable/current/available/ready/failure; explicit Restart & update; Preview-to-Stable waiting; compatibility/security block retains cache/history | AIU-014/015; all synthetic, no installer/feed/trust changes |

## Required cross-screen rules

- Navigation proposal: Overview, Accounts, History, Settings, System Status. Add account is contextual; recovery is a dedicated blocking surface when normal operation is unsafe. Claude Design may refine navigation while preserving coverage. Superseded on 2026-09-24 by [D-180](../../decisions/accepted.md) and [AIU-030](../AIU-030-single-window/spec.md): one usage view without tabs, a Settings icon, and History and account detail opened from the account panel.
- Stable identity hierarchy: provider -> account -> context -> group -> window. Labels are editable display values. Do not expose opaque IDs as primary UI copy unless no safe label exists.
- Keep primary quotas visible. Auto-expand warning groups only when there is no explicit user collapse/hide preference. Hiding does not stop collection; muting alerts is a separate choice.
- Overview minimum uses comparable known remaining percentages and identifies the exact source. Unknown-only data produces “Unavailable”; stale candidates retain a stale badge. Counts may include all visible accounts; document filter scope beside summary definitions. Shared pools are counted once by stable pool identity.
- Distinguish connected-without-quota from disconnected. One account error never replaces every card. Disable only actions that are genuinely unavailable or conflict with admitted work.
- Relative reset time also exposes the exact local timestamp. A passed reset reads “Awaiting update” until a new observation arrives.
- Disconnect means local credential removal/monitoring stop; it does not promise remote revocation. Future retention/reconnect semantics are demo-only until their service exists. Settings reset preserves accounts/history/labels/order; factory reset has an explicit full-data confirmation.
- English v1 text uses resources; user labels and provider protocol values are never translated. Neutral provider marks are the default until asset rights are reviewed.
- No persistent custom alert-history screen. OS notification previews deep-link to the account/group; Windows Notification Center owns alert history.

## Design deliverables per surface

For every S ID provide a layout reference, state variants, component references, actions and outcomes, keyboard/focus order, accessible labels, narrow-width behavior, and demo scenario IDs. Deliver light/dark for all reusable components; show high contrast and reduced-motion behavior on representative screens. Provide editable design source plus exported review views and a clickable prototype or equivalent interaction specification. Mark prototype images as design, not screenshots of a built app.
