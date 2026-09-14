# AIU-004 verification

Historical delivery evidence below describes packages 2026.9.1402.0 and 2026.9.1403.0. The subsequently completed [CR-AIU-004-01 verification](close-to-tray-verification.md) supersedes their close-exits behavior with verified close-to-tray on package 2026.9.1406.0.

## Scope and environment
- Date: 2026-09-14. All three subtasks of AIU-004 are now recorded: dashboard over protected credentials, cached snapshot, and tray presence.
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
| `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release -- -noLogo` after the cache subtask | PASS: 72 tests | Adds cached-snapshot behavior to the previous 68 |
| Clean disposable guest run `guest-evidence-1789395275333` on package 2026.9.1403.0, hash 5B3D17FBB096BDDEFF04719DF93222C13EE65064793BDDD7DB3046BB3CFC6C51 | PASS: `positive: PASS`, `smokeExitCode: 0`, orchestration `FINISHED` | Tray presence asserted in all three scenarios; negatives observed again |
| Inspected screenshot `guest-evidence-1789395275333/product/positive/title-bar.png` | PASS | Installed dashboard with the tray build: Dashboard heading, "No accounts connected.", focused "Connect Codex", disabled Refresh and Disconnect |

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

## Cached snapshot behavior
- A successful read writes the normalized snapshot and its retrieval time; the record holds no token, refresh token or account identifier, asserted by a test that reads the file and checks for each secret.
- When the provider is unavailable the session returns the previous reading with `FromCache` set and the original retrieval time, and the dashboard appends an explicit "not a current reading" notice with that timestamp. Tests assert the stale values and timestamp survive unchanged.
- A relaunch renders the cached reading before any provider request: a second session over the same directory returns cached values with no additional HTTP call, asserted by comparing the request count.
- Disconnect removes the cached reading along with the grant.

## Tray presence
- `H.NotifyIcon.WinUI` per D-067, with a left-click Open command and a context menu offering Open and Exit, both bound to the same commands the window uses.
- The smoke asserts a tray host window owned by the launched process, so a build without tray presence fails. Automating the notification area itself is not attempted.
- Close still exits the process. D-108 wants close to hide to tray, which changes the lifetime that AIU-002 verified through its title-bar close scenario; that behavior change is recorded as follow-up `CR-AIU-004-01` rather than slipped in here.

## Acceptance matrix
| Criterion | Verdict | Evidence and boundary |
| --- | --- | --- |
| AC-01 | PASS | The UI resolves `CodexSession` from dependency injection; no endpoint, header, token parsing or quota normalization exists in the UI layer |
| AC-02 | PASS, deterministic | DPAPI CurrentUser record under the app-owned LocalState `providers` directory; no plaintext secret on disk, in UI text or in failure messages. Real-account persistence on the packaged app is NOT_RUN |
| AC-03 | PASS, deterministic | Resume and invalidated-grant transitions are covered by tests; a live relaunch with a real stored grant is NOT_RUN |
| AC-04 | PASS, deterministic | Five distinct states; absent percentages and reset times render as explicit unknown labels and exhausted windows say so. The installed screenshot confirms the not-connected state and command availability only; quota rendering is covered by tests, since the guest has no networking |
| AC-05 | PASS, deterministic | Disconnect deletes only the owned record, keeps neighbouring files, and states no provider-side revocation |
| AC-06 | PASS | Build, 72 tests, two guest runs with inspected screenshots and canonical validation recorded above; every unexercised path is marked NOT_RUN rather than inferred |
| AC-07 | PASS, deterministic | Cached snapshot with retrieval time, explicit staleness notice, credential-free record and removal on disconnect, covered by tests; a live stale reading from a real unavailable provider is NOT_RUN |
| AC-08 | PASS | Tray presence asserted by the smoke in the clean guest on package 2026.9.1403.0; close-to-tray behavior is deliberately deferred as CR-AIU-004-01 |

## Notes
The previous smoke asserted an `EmptyState` element that the new dashboard replaces with `StatusText`. The smoke was updated to the new surface and strengthened: it now also asserts that the quota list is empty on a first run, that Connect is enabled and keyboard focusable, and that Refresh and Disconnect are disabled while nothing is connected. Assertions on actual process exit and the absence of process-owned windows are unchanged.

Packages retained: 2026.9.1401.0 unsigned, 2026.9.1402.0 signed for the dashboard guest run, and 2026.9.1403.0 signed for the tray and cache run. Earlier candidates remain untouched.
