# Synthetic Windows UI/UX audit implementation plan

**Goal:** Execute the owner's 2026-10-04 audit against the ordinary Windows application, repair evidence-backed defects, and retain an honest coverage matrix and readable final gallery.

**Architecture:** Reuse the existing provider parsers, Core budget engine, live Ledger projection/source, local budget and preference stores, view models, XAML, and FlaUI UIA3 tests. An explicitly selected demo-only replay adapter substitutes synthetic accounts and browser/support boundaries; fixture tests export parser-normalized inputs and the production recorder's actual observations. The default product composition remains unchanged. No real provider service is registered in replay mode.

**Scope and authority:** The current owner request authorizes implementation and verification. Work on main, preserve existing work, use only synthetic fixtures and fresh isolated roots. No installed-app operations, credentials, live authentication/quota requests, trust changes, or release publication. Save commits with CI skipped because main's enabled Preview workflow publishes releases; execute applicable checks locally.

**Acceptance:** Every applicable inventory row needs deterministic assertions, real UI evidence where applicable, a readable final screenshot, and an accurate verdict. Every supported clickable control needs a physical mouse click and observed outcome/target. Compilation, UIA existence, demo figures, and screenshot generation alone are insufficient. Unsupported, unreachable and blocked cases must be explained.

- [x] Inventory provider families, parser policies, contracts, state precedence, layouts, controls, shortcuts and existing tests; remaining exact composite scenarios are explicit.
- [x] Add reproducible replay inputs, controlled clocks and fake request receipts, keeping external boundaries isolated; deterministic replay assertions pass and native corpus execution is in progress.
- [x] Add independent arithmetic/threshold assertions and parser-to-projection corpus tests; export the tested 170-case corpus for Windows replay. Exact additional composite transitions remain pending.
- [ ] Run actual Windows mouse/keyboard scenarios, ordinary resize/scroll checks and native lifetime/tray checks. Preserve failures separately.
- [ ] Reproduce and repair product defects with meaningful regression tests; rerun affected checks.
- [ ] Capture and inspect final screenshots, produce scenario/control indexes and actual results.
- [ ] Run required regressions, document validation, native/package builds, integrated diff review and a fresh independent review of the audit changes.
- [ ] Commit/push save points and deliver commands, build reference, counts, fixed defects and remaining evidence gaps.

## Review focus

Null readings must remain unknown; percentage subscriptions must suppress only the Codex credit card, retaining native facts. Monthly money budgets must reset at local calendar boundaries without importing previous-month baselines. Daily exhaustion, personal caps, provider exhaustion, day off, five-hour fullness and stale/auth states must obey precedence. Hit testing must survive overlays, scrolling and snapshot replacement. Replay must never fall through to real accounts, browsers or installed-app storage.

## Initial evidence

Base: main `383644c`, clean checkout. Presentation baseline PASS 128/128; Infrastructure baseline PASS 613/613. Current Infrastructure PASS 783/783 (170 corpus cases with recorder/export/rehydration); Presentation PASS 161/161; validator tests PASS 80/80. Debug unpackaged and Windows driver builds PASS with zero warnings/errors. Unsigned MSIX 2026.10.409.0 PASS; external symbol-tool warning recorded. Five product defects reproduced and repaired with regression tests. Existing demo is presentation-only and cannot establish parser/budget correctness.

Native blocker: desktop is locked. Initial smoke app launches could not acquire input; invalid lock-screen captures are excluded. Corrected read-only desktop prerequisite reports BLOCKED before input/capture. Unlock request is pending. 170 corpus rows, 39 additional scenario rows and 93 control/keyboard/non-applicable rows record actual gaps, with zero final screenshots. See report.md.

Checkpoint `2942e6f` freezes deterministic product/test changes. Fresh independent review found two native-driver safety/lifetime issues: physical input lacked foreground/hit-test ownership guards and failed construction could leave its child process alive. Both are corrected at `ae4f483`; driver compilation PASS and all 11 native tests actually rerun BLOCKED at the locked-desktop prerequisite. See review.md. Product suites need no repeat for these driver-only corrections. Document validation PASS including review records. Both save points were pushed to main and the remote head verified; `[skip ci]` prevents release publication. Final publication metadata changes only these records.

## Handoff

Owner follow-up authorized investigating Windows Sandbox while the host stays locked. Concrete reason: the host cannot provide an interactive test desktop. A network-disabled disposable guest exposes only a read-only synthetic input directory and an empty writable evidence directory; clipboard, microphone, camera and printer sharing are disabled. No host trust changes, installed-app operations or real credentials are involved. Self-contained unpackaged application and test driver builds run inside the guest.

Guest desktop prerequisite PASS. Actual overview Used/Left mouse clicks, state checks, captures and clean exit PASS; both overview images inspected. UIA reports ProcessId=0 for the guest WinUI window, so the harness now establishes ownership using the native window handle and native ancestor before focus/scroll, retaining foreground and point hit-test guards. The failed startup-filter attempts are harness failures, not application failures; the corrected driver was actually rerun successfully.

Current checkpoint: all 170 baseline scenarios passed native Used/Left assertions. Presentation now passes 169/169; Infrastructure passes 783/783. Four further product defects were reproduced and fixed: provider-overage denominator, retried-auth cancellation, workday name notification and last-workday availability. First-run and duplicate-provider native test PASS; auth success native test PASS. Final control reruns use the current workday-fix build. Gallery visual inspection is partial; composite/lifetime/recovery/control gaps remain.

Current additional corpus: 65 independent scenarios cover full-short-window precedence, before/at/after short resets and replacement, historical numeric thresholds under D-186, cap/day-off precedence, count/money CapClose thresholds, last-workday fit counts, and controlled Lisbon DST boundaries. Infrastructure PASS 848/848; Presentation PASS 178/178. Paired expired short-window readings now preserve the period budget with a qualified past-reset mark. Binding-limit tooltip wording is corrected. Actual cap-form Tab regression advanced past the formerly failing Save/Shift+Tab assertions; later failures were driver focus selection, corrected for a real rerun.

Exact next action: collect expanded-native-1 results for the 65 added scenarios, preferences and native tray tests; retain and repair any failures, then implement the remaining composite/transition/recovery/history evidence before final gallery capture. The comprehensive audit remains in progress.
