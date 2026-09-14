# AIU-004 verification

## Scope and environment
- Date: 2026-09-14. This records the dashboard subtask of AIU-004; the cached-snapshot and tray subtasks remain open in the same item.
- Environment: Windows 11, OMP 18.1.19, user-local .NET SDK 10.0.401, disposable Windows Sandbox guest without networking.
- Change: DPAPI-protected Codex grant storage, a session that owns resume/connect/refresh/disconnect, and a dashboard that renders quota. The provider client from AIU-003 is reused unchanged.
- No source CLI credential store was read or modified. No host installation, host certificate trust change, elevation or remote action occurred.

## Executed checks
| Check | Actual result | Scope |
| --- | --- | --- |
| `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Release` | PASS | Product UI, XAML compilation and bindings |
| `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release -- -noLogo` | PASS: 68 tests, zero failures/errors/skips | Provider protocol plus grant store and session state transitions |
| `Build-Package.ps1 -MsixVersion 2026.9.1402.0 -CertificateThumbprint 771CB0E8...` | Signed candidate retained; host `signtool` verification failed closed on the untrusted development root | Expected behavior recorded in AIU-002; the bytes are guest-verified, not host-installable |
| Clean disposable guest run `guest-evidence-1789392583866` | PASS: `positive: PASS`, `smokeExitCode: 0`, orchestration `FINISHED`, product exit 0 | Installed offline dashboard on the new package 2026.9.1402.0, hash 3482BAD8286FE7DA1526505DF3A5CDEA9BE1FB1A2B4D77832FCF694388FA7814 |
| Inspected screenshot `product/positive/exit.png` | PASS | Installed window shows Dashboard, "No accounts connected.", enabled "Connect Codex", and disabled Refresh and Disconnect. With networking disabled this observes the not-connected state, not quota in the dashboard |
| Canonical project validation | PASS: `{"valid":true,"diagnostics":[]}` | `dotnet run --project tools/AiUsage.ProjectValidation -- --root . --json` |

## Behavior covered by tests
- A stored grant round-trips through DPAPI CurrentUser; the on-disk record contains neither the refresh token nor the account identifier in UTF-8 or UTF-16, and the file name carries no secret.
- Replacing a grant leaves exactly one record; disconnect removes it and leaves an unrelated neighbouring file in the same directory untouched; a second disconnect is not an error.
- An empty or foreign record reads as absent instead of throwing.
- Resume exchanges the stored refresh token, persists the rotated replacement, and shows quota. A record naming another workspace is rejected as an account mismatch, so a swapped file cannot silently switch accounts.
- An invalidated grant produces the reauthentication state and keeps the record so the user can retry a sign-in knowingly.
- A provider-unavailable quota response yields the quota-unavailable state with the grant still connected; no zero quota is fabricated.
- Refresh reuses a live access token instead of renewing the grant on every read.

## Live account boundary
The guest has no networking and host installation is not authorized, so connecting a real account **from the packaged UI is NOT_RUN**. What is verified live is the provider path itself, through the console under AIU-003: real browser sign-in, real quota read, in-memory refresh and a second real quota read. The UI consumes exactly those clients and adds no request of its own.

Also NOT_RUN: a relaunch that resumes a real stored grant end to end, disconnect against a real account, and the reauthentication state driven by a genuinely invalidated provider grant. These are covered by deterministic tests over the same code paths, which is not the same as live proof.

## Acceptance matrix
| Criterion | Verdict | Evidence and boundary |
| --- | --- | --- |
| AC-01 | PASS | The UI resolves `CodexSession` from dependency injection; no endpoint, header, token parsing or quota normalization exists in the UI layer |
| AC-02 | PASS, deterministic | DPAPI CurrentUser record under the app-owned LocalState `providers` directory; no plaintext secret on disk, in UI text or in failure messages. Real-account persistence on the packaged app is NOT_RUN |
| AC-03 | PASS, deterministic | Resume and invalidated-grant transitions are covered by tests; a live relaunch with a real stored grant is NOT_RUN |
| AC-04 | PASS, deterministic | Five distinct states; absent percentages and reset times render as explicit unknown labels and exhausted windows say so. The installed screenshot confirms the not-connected state and command availability only; quota rendering is covered by tests, since the guest has no networking |
| AC-05 | PASS, deterministic | Disconnect deletes only the owned record, keeps neighbouring files, and states no provider-side revocation |
| AC-06 | PASS | Build, 68 tests, guest run with inspected screenshot and canonical validation recorded above; every unexercised path is marked NOT_RUN rather than inferred |
| AC-07 | NOT_RUN | Cached-snapshot subtask not started |
| AC-08 | NOT_RUN | Tray subtask not started |

## Notes
The previous smoke asserted an `EmptyState` element that the new dashboard replaces with `StatusText`. The smoke was updated to the new surface and strengthened: it now also asserts that the quota list is empty on a first run, that Connect is enabled and keyboard focusable, and that Refresh and Disconnect are disabled while nothing is connected. Assertions on actual process exit and the absence of process-owned windows are unchanged.

Package 2026.9.1401.0 was built unsigned first and is retained; 2026.9.1402.0 is the signed candidate used in the guest. Earlier candidates remain untouched.
