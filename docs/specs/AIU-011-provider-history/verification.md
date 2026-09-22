# AIU-011 verification

## Scope preparation - 2026-09-22

Base: `7d0bf06` on clean `main`. Documentation-only scope split and initial source
assessment; no product implementation, live-provider read or credential access.

| Check | Verdict | Evidence or limitation |
| --- | --- | --- |
| Owner scope captured | PASS | All four providers; provider-supplied usage history; fewer required clicks; local collection deferred to low-priority AIU-029 after stability. Proposed detailed interaction remains draft. |
| Initial public-source assessment | PASS | Sources, exact source references and access uncertainties recorded in research.md. This does not satisfy complete AC-01 contract discovery. |
| Product implementation and regression suites | NOT_RUN | No product code changed. |
| Authenticated provider history / Windows interaction | NOT_RUN | No live access or actual history UI implementation tested. |
| Document validation | PASS | `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json`: valid=true, diagnostics=[], exit 0. The initial run reported GOAL_SCOPE because the new AIU-029 membership was absent from G-003; adding that membership resolved the finding. |
| Diff and primary acceptance review | PASS | `git diff --check`: exit 0. Reviewed scope split, dependency change, existing-decision amendment, source provenance, explicit draft proposals and absence of invented live/support claims. No production code or credentials changed. |

This preparation did not complete AIU-011. PD-011-01 was unresolved at this point and
was subsequently resolved by the owner as recorded below.

## Existing authorization and OMP parity assessment - 2026-09-22

Base: `8779d55` on clean `main`. The owner excluded additional authorization and directed
matching OMP's history retrieval for each provider if it exists.

| Check | Verdict | Evidence or limitation |
| --- | --- | --- |
| Stable OMP reference | PASS | Public releases/latest returned v18.2.8, published 2026-09-21T17:31:56Z; tag resolved to 5e0fc867f8a58dfe8812b5e99b2e7b6a0313da6c. The recursive source tree was not truncated. Relevant raw files were fetched at that exact commit. |
| AC-01 selected-path assessment | PASS | Inspected all four current OAuth adapters, UsageReport/UsageHistoryEntry, AuthStorage recording and retrieval, SQLite history persistence, CLI history branch, broker route and upstream history test source. Exact references and provider-specific exclusions are in research.md. |
| AC-02 implementation feasibility | BLOCKED | OMP's selected OAuth paths return current quota, while history is accumulated locally. Copilot's billing branch requires api_key credentials. No selected path yields a provider-supplied historical dataset. |
| Credential and upstream preservation | PASS | No real credentials, browser sessions or OMP databases read; local OMP checkout unchanged. Public source inspection only. |
| Product implementation / upstream test execution / live history / Windows UI | NOT_RUN | No eligible provider-history implementation to execute. Source inspection does not establish live-provider behavior. |
| Document validation and diff review | PASS | README validator command: valid=true, diagnostics=[], exit 0. `git diff --check`: exit 0. Primary review confirmed existing-auth-only scope, pinned OMP source, resolved access decision, truthful blocking status and continued deferral of local collection. |

AIU-011 is blocked, not completed or silently expanded. Local sampling remains deferred
in AIU-029, whose unnecessary dependency on remote-history completion was removed.

## Broader provider-history feasibility - 2026-09-22

Base: `c46e708` on clean `main`. Documentation-only follow-up to the owner's question
about general feasibility; existing authorization remains the boundary.

| Check | Verdict | Evidence or limitation |
| --- | --- | --- |
| Codex historical transport | PASS | Inspected official development source at 2c2a42e65de077c5518ea5b4c3999633ef6a12fc: analytics routes, shared auth headers, account/user-bound session, dated response models and upstream test source. Also inspected the pinned Codex Watch client. This establishes a candidate, not live access. |
| Release distinction | PASS | GitHub latest release returned rust-v0.155.1, published 2026-09-18T20:03:04Z; annotated tag resolved to be2951ea34f0d295ed0becf97079f92fa5f6950e. The specific analytics client file returned 404 at that tag. No claim that the development feature shipped in that release. |
| Other provider evidence | PASS | GitHub documents historical reports and fine-grained access; current OAuth eligibility is unresolved. Official Claude and Antigravity help pages describe credit history but do not establish compatible historical transport. See research.md for references and limits. |
| Credentials, provider reads, product code and upstream tests | NOT_RUN | No real credential reads, authenticated requests, source-CLI execution, production edits or upstream test execution. Public code and documentation inspection only. |
| AC-02 real dataset through AI Usage | NOT_RUN | No live history dataset fetched. Candidate source code does not complete the feature. |
| Document validation and diff review | PASS | `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json`: valid=true, diagnostics=[], exit 0. `git diff --check`: exit 0. Primary review checked source provenance, development/release distinction, conditional OMP preference, unchanged authorization boundary and absence of live-success claims. |

The OMP-specific result remains true. It no longer justifies treating all remote history
as unavailable; the backlog returns to research-needed with Codex as the first concrete
existing-session candidate. The low-click UX and local-history deferral are unchanged.

## Provider-history implementation - 2026-09-22

Implementation base: `c38467a`; intermediate save point: `45eef10`. The owner explicitly
requested implementation. Final review fixes retain existing authorization only. The
historical research-only verdicts above describe earlier stages, not the implementation.

| Check | Verdict | Evidence or limitation |
| --- | --- | --- |
| Infrastructure regression suite | PASS | `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo`: 307/307. Includes parser semantics, request identity/date headers, independent report denial, Retry-After, no-grant access, token rotation persistence under cancellation, and external grant removal/replacement. |
| Presentation regression suite | PASS | `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo`: 154/154. Includes account-scoped automatic loads, clock independence, stale recovery, navigation/re-entry, account cache invalidation, shared scheduling and shutdown/disconnect draining. |
| Unpackaged Windows build | PASS | README Debug/x64/WindowsPackageType=None command, no restore: 0 warnings, 0 errors. |
| Unsigned MSIX build | PASS | Existing VS MSBuild, Release/x64/win-x64, no restore or signing, version 2026.9.2202.0. Artifact: `.ai-usage-local/AIU-011/packages/2026.9.2202.0/AiUsage.Windows_2026.9.2202.0_x64_Test/AiUsage.Windows_2026.9.2202.0_x64.msix`. SDK warning: mspdbcmf.exe unavailable, so no symbols package. No owned-code warning. This is build evidence only; no new host installation, signing or trust change. |
| Actual demo Windows smoke | PASS | 7/7 against final unpackaged build. Evidence: `.ai-usage-local/AIU-011/ui-demo-final`. History navigation asserts report rows and visible `12000 tokens` without a Load action. Inspected `page-NavHistory.png`: native units, UTC periods, synthetic provenance, account/range controls. Launch, navigation, themes, tray restore, tray exit, repeated exit and capability interactions passed. |
| Actual product Windows smoke | PASS | 7/7; evidence `.ai-usage-local/AIU-011/ui-product-final`, explicitly empty `.ai-usage-local/AIU-011/empty-product-final-state`. Product History is available without connecting or loading first; launch/navigation/theme/tray/exit/capability scenarios pass. This is empty-state UI evidence, not a live provider read. |
| Console probe help/build | PASS | `dotnet run --project tools/AiUsage.ProviderConsole --no-restore -- --help`, exit 0. The probe was compiled; authenticated execution remains blocked below. |
| Focused independent review | PASS | Luna/max read-only review of credential-consuming history paths found three material issues in the intermediate version: cancellation during rotation, cross-process account/mismatched-response stale retention, and malformed Copilot items reported empty. Primary reproduced and fixed them; targeted reviewer confirmation passed. Primary additionally fixed external Copilot removal and denial of one billing report hiding the other. No unresolved material finding. |
| AC-02 real history fetched and displayed | BLOCKED | Standard unpackaged development state was absent. The installed package provider directory had only a Claude lock file and no Codex/Copilot grants. No source CLI credentials or browser sessions were read, and no new sign-in was attempted. An alternate app-owned provider directory was requested; none was supplied during this run. No authenticated history request was made. |

### Review dispositions and limits

A rotating Codex exchange already in progress now finishes its bounded transport and
persists the returned grant under the durable lease before honoring navigation cancellation.
The regression first failed and then passed. This does not recover a network response lost
after provider-side rotation; that ambiguity remains a possible reauthentication outcome.

External account replacement and response identity mismatch produce AccountChanged, discard
partial reports and clear cached history rather than presenting the previous identity's data
as stale. Copilot external deletion clears session state without a network request. Tests
also observed malformed Copilot items and report-denial isolation fail before their fixes.

The first UI runs passed 6/7 in each mode but failed the physical-click tray-popup step;
a targeted physical-click retry also failed. The smoke now invokes the observed tray
button's primary UI Automation action, and the targeted action plus final demo suite pass.
This proves primary-action popup/restore behavior, not a diagnosed fix for physical mouse
input. Production tray behavior was not changed, and the physical-click automation issue
remains a separate limitation rather than a silently upgraded PASS. The tray case has its
own test method so it can be selected without rerunning unrelated scenarios.

Codex endpoints are observed in official development source, not a public stable analytics
contract. Top plugin/skill requests are bounded to 100 and that limit is not live verified;
retention, paging/truncation, plan coverage and optional summaries remain incomplete.
Copilot billing eligibility with the existing read:user grant is unverified; calendar month
aggregates remain aggregates. Claude/Antigravity explicitly show Unsupported. All history
results are memory-only, with no local sampling, durable history, extra permissions or login.

AC-01, AC-03 through AC-05 and the implemented scope of AC-06 have deterministic/source/UI
evidence. AC-02 is not satisfied, and no whole-feature completion claim is made. The exact
next action is retained in tasks.md; AIU-029 remains deferred.

Final document validation: PASS (`valid=true`, no diagnostics). Final primary integrated
review and `git diff --check`: PASS. The generated package manifest was inspected and
matches AiUsage.Dev version 2026.9.2202.0. All smoke-created app processes exited; generated
artifacts and screenshots remain local and ignored. The final product/demo smoke verdicts
refer to the primary-action UI Automation tray path, with the separate physical-click
limitation retained above. No actual history connection was silently claimed as verified.

## Authorized browser-login live verification - 2026-09-22

Base: `d3817ed`. The owner explicitly requested verification using the existing browser
login automatically. This run used the normal AI Usage connection UI, the existing Chrome
OpenAI/GitHub sessions and the same implemented scopes. The normal product flows wrote
app-owned protected grants. No browser cookie extraction, source CLI credential import,
new billing/admin permission, provider purchase, prompt or inference call was used.

| Check | Verdict | Observed result |
| --- | --- | --- |
| Codex browser connection | PASS | Selected the existing OpenAI browser account and normal personal-workspace sign-in flow; AI Usage displayed Connected and a real fresh quota. The authorization callback completed during browser interaction; no claim that every consent click was performed by the agent. |
| AC-02 real Codex history in Windows | PASS | Opened History directly from the connected account detail. The default 30-day query loaded real dated usage, activity input/cached/output/total tokens, client/model breakdowns, plugin invocations and skill invocations. Nonzero historical values were observed in the accessibility tree; the rendered History page was visually inspected. No initial Load action. |
| Codex seven-day console read | PASS | Product-session command `history codex <owned-provider-directory>` returned Available for usage (199 dated rows), activity (196), plugins (3) and skills (1); credits returned Empty. No unknown numeric values in those returned rows. Counts are a snapshot, not a promise about all periods. |
| Codex optional workspace reports | OBSERVED_LIMITATION | A temporary status-only handler on the normal hardened typed client observed HTTP 400 for both workspace token variants and HTTP 403 for the four enterprise credit breakdowns. UI preserved the available reports and showed failures/access-denied separately. A 400 was not represented as proof of missing permission. |
| Codex top-list limit | PASS_WITH_LIMIT | The implemented plugin/skill requests with limit 100 returned HTTP 200 for this account. This verifies acceptance of that request, not completeness, retention or applicability to every plan. |
| Copilot browser/device connection | PASS | Used the current GitHub browser session and the device code displayed by this AI Usage connection. GitHub showed Existing access / Read all user profile data for the existing OpenCode registration. Confirmation reached device connected; AI Usage stored the grant and displayed the initial quota. No new scope. |
| Copilot historical reports | UNAVAILABLE_WITH_CURRENT_CONNECTION | Both AI-credit and premium-request requests returned HTTP 404, while `/user` returned HTTP 200. The product UI and seven-day console probe showed failed reports and no historical rows. A 404 does not distinguish account-plan restrictions, grant eligibility or endpoint availability; no specific cause is claimed. No extra credentials or permissions were requested. |
| Credential and evidence handling | PASS | Reads used `%LOCALAPPDATA%/AiUsage/Development/providers` through product sessions. The console printed fixed report/status/count fields only; temporary transport instrumentation printed fixed labels and HTTP status codes only. No token, raw response, identity, native private value or screenshot was added to repository evidence. The temporary handler was removed; HistoryConsole.cs has no resulting diff. |
| Restored console build | PASS | `dotnet build tools/AiUsage.ProviderConsole --no-restore`: zero warnings/errors after removing temporary instrumentation. Product code is unchanged from the previously tested implementation. |

The earlier missing-grant blocker is resolved by this explicitly authorized verification
run. AC-02 is satisfied by a real Codex dataset fetched and displayed through the selected
product path. This closes AIU-011's capability-dependent scope; it does not claim working
Copilot history, every Codex workspace report, or remote history for Claude/Antigravity.
The physical-click smoke limitation and provider coverage limits recorded above remain.
Connections are retained for the owner; no disconnect/revocation was requested.

Final live-evidence document validation: PASS (`valid=true`, diagnostics empty).
`git diff --check`: PASS. Primary review confirmed the owner authorization amendment,
real-data acceptance, distinct 400/403/404 outcomes, absence of private data in the diff,
and removal of temporary instrumentation. This follow-up changes documentation only;
the earlier deterministic/build/interactive checks remain the product-code evidence.

## Owner-requested follow-up verification - 2026-09-22

After the live-verification report, the owner directed that this feature remain pending
further verification and receive lower priority. The backlog now records review / low;
the previous completion disposition is superseded. Existing observed PASS results and
provider limitations remain unchanged. No additional provider or UI checks were run for
this status-only amendment.
