# AIU-002 verification

## Authorization and code reference
Owner selected AIU-002 and disposable Sandbox/VM verification, then approved execution of the Windows MSIX plan. Local branch: feature/AIU-002-windows-msix. Existing AIU-001 changes and an unrelated shortcut were preserved. No remote actions, host package installation, host trust import, elevation or reboot occurred. No bounded execution JSON or budget was fabricated.

Current status: PAUSED by the owner's explicit switch to Codex library/console integration. The reboot prerequisite is resolved. Real product and routing reports now exist; final visual acceptance and cleanup were interrupted by the focus change. The earlier BLOCKED sections below are historical pre-reboot evidence, not current environmental state.

## Prerequisites
- OMP executable reports 18.1.19. Installed settings-schema and discovery APIs inspected. Actual Settings.loadReadOnly({cwd, agentDir}) resolves Plan enabled/default-on, Advisor enabled/syncBacklog string 1, isolation enabled/apply false/merge patch. Actual discoverAndLoadExtensions returns project ai-usage, command work, no load errors. No inference or bounded grant was created by discovery.
- User-local SDK reports 10.0.401; all .NET commands disable telemetry and ASP.NET certificate generation. Installed VS Community 18 MSBuild reports 18.10.1. Core, Infrastructure, WinUI XAML and MSIX packaging are reachable through VS MSBuild and NuGet tools despite absent Windows Kits registration.
- Targeted vswhere Windows11SDK.26100 query and Windows Kits Installed Roots query return no installed SDK root. Packaging emits one external tools warning: mspdbcmf.exe absent; no symbols package generated. No owned-code warnings/errors on the successful native build.
- WindowsSandbox.exe absent; queried vmrun/VBoxManage/Get-VM unavailable; vmcompute/vmms services absent. Win32_OptionalFeature reports Containers-DisposableClientVM and Microsoft-Hyper-V-All InstallState 2 (disabled). Win32_Processor returns false for the three queried virtualization flags; this observation alone is not a firmware diagnosis. No features were enabled. A supported disposable guest remains an owner/system prerequisite.

## AC-01 — native build and boundaries: PASS
Actual VS MSBuild solution restore/build: Release, x64, win-x64, unsigned. Three production assemblies emitted. Core resolved libraries are empty; Infrastructure references Core only. Generated AiUsage.runtimeconfig.json requires Microsoft.NETCore.App 10.0.0; it does not contain included self-contained frameworks. Generated AppxManifest.xml requires Microsoft.WindowsAppRuntime.2 >=2.4.0.0 with Microsoft publisher. Both app deployment modes remain framework-dependent. Appx manifest executable is AiUsage.exe, entry point Windows.FullTrustApplication, only runFullTrust capability.

Initial restore FAILED with NU1605: Windows App SDK 2.4.0 -> Base 2.0.4 requires BuildTools >=10.0.26100.4654, exceeding the planned 10.0.26100.3916 pin. Raised only BuildTools to the actual stable dependency floor; retained all other selected pins and warnings-as-errors. Subsequent restore/XAML/C# build succeeded. No renderer substitution or dependency-asset exclusion.

## AC-02 / AC-03 — installed offline UI and process lifetime: BLOCKED
Resource-backed Dashboard, No accounts connected., keyboard-accessible Exit and coordinated Host lifetime are implemented. This is source state, not runtime PASS. No host installation/launch was used to evade the requested guest boundary.

Self-contained publish of tests/windows/AiUsage.Windows.Tests succeeded. Actual executable invocation without AIU_SMOKE_AUMID exited 1: three failed prerequisites, zero skipped tests. No product process was activated. Suite serializes desktop use, retains the activated process handle, checks actual exit code/termination and captures app-window PNGs. Exit/title-bar/repeated requests still require installed execution. No real app screenshot exists yet.

## AC-04 — development package and clean guest: BLOCKED
- A reusable RSA 3072 SHA-256, nonexportable, end-entity code-signing certificate was created once in CurrentUser/My, Subject CN=AI Usage Development, DigitalSignature and code-signing EKU. Only its public CER is staged. No PFX/private-key export or host TrustedPeople/TrustedRoot import.
- Unsigned first package 2026.9.1301.0 built; SHA-256 F622A74B70530AFD3F7559AD9B26E36EDCDCF8B0456B4329647DEAFC215FA2FC. Validation-only; never installed.
- Failed build reservation 2026.9.1302.0 retained its failure record. An intermediate candidate in reservation 1303 revealed that AppxPackageVersion alone was ignored by the selected packaging targets: embedded version remained 1301. That signed intermediate is rejected, retained locally and not staged/installed. The build command now generates a version-specific manifest without mutating source and validates the embedded identity/version before signing.
- Final staged candidate after focused-review fixes: Identity AiUsage.Dev, Publisher CN=AI Usage Development, App Id App, version 2026.9.1305.0. SHA-256 **B3D71F053423669FEF3062BA2520F93A1CBDF2F36F5DF7A0D67C6915D868C854**. SignTool signing succeeded. SignTool verification exited 1 because the self-signed root is deliberately not trusted on the host; Build-Package.ps1 fails closed and records failed-not-installable. This is signed candidate evidence, NOT a verified installable success. Prior 1304 candidate and its bytes remain retained.
- Actual rerun with 2026.9.1304.0 exited 1 before building; full package bytes remained identical. Prior output was not overwritten.
- Official .NET 10.0.12 x64 runtime installer staged after SHA-512 verification against official release metadata: b937ae539c4ab21f885b033178dadea98018baad777252c76251e760d81c18463b2f802e86b6a67ea066690e9ede2d66a6bc3c3e614e391505e98c4c41121400. Authenticode status Valid. Microsoft.WindowsAppRuntime.2 x64 dependency also reports Valid and its manifest has no further PackageDependency (no VCLibs package required by this selected framework manifest).
- Ignored staging root: .ai-usage-local/AIU-002/staging. Contains the signed candidate, public CER, official runtime installer, Microsoft framework MSIX, self-contained smoke and guest script. Session-specific .wsb maps only this input read-only and an empty separate evidence directory writable; networking, clipboard, audio/video input and printer redirection disabled. No repository/home/key mapping.
- Actual host invocation of Invoke-PackageSmoke.ps1 with all staging inputs exited 1 at disposable-guest guard before trust or installation. Both PowerShell scripts parse without syntax errors. Guest provisioning and missing-framework/missing-.NET probes remain NOT_RUN. Guest harness reports initial inventory, separate negative stages and positive smoke; a present prerequisite requires a fresh negative snapshot, not uninstalling host runtimes.

## AC-05 — native routing feasibility: BLOCKED at runtime
One isolated routing worker returned a text-only patch; primary read the entire patch, checked and integrated it. Concurrent smoke worker hit the five-minute native deadline with no captured patch; primary implemented smoke inline. No worker ran validation during the batch.

Pinned Uno.Extensions.Navigation.WinUI 7.3.6 source/API reference: commit c12a3c95060137ee51118d2f171775aae0fa2a3b. Worker inspected its Windows package XML and source for CreateBuilder, Configure, UseNavigation, NavigateAsync, typed ViewMap/RouteMap, NavigateViewModelAsync and NavigateBackAsync. Primary actual restore/build and unsigned native MSIX packaging succeeded for spikes/windows/AIU-002-routing/AiUsage.RoutingSpike.csproj with Release/x64/win-x64.

Resolved Windows assets:
- Microsoft.WindowsAppSDK.WinUI 2.3.6 supplies compile/runtime Microsoft.WinUI.dll from net6.0-windows10.0.17763.0.
- Uno.Extensions.Navigation.WinUI 7.3.6 selects net9.0-windows10.0.19041/Uno.Extensions.Navigation.UI.dll.
- Uno.WinUI 6.0.465 selects only net9.0-windows10.0.19041.0/Uno.UI.Toolkit.dll, not an alternate XAML renderer assembly.

This asset inspection is not loaded runtime identity proof. Main -> Second -> Main, displayed Microsoft.UI.Xaml.Application assembly identity and final process termination remain NOT_RUN pending guest. Product stays directly composed; no second page or routing package added to product.

Final routing candidate is separately signed and staged at staging/routing/AiUsage.RoutingSpike.msix, identity AiUsage.RoutingSpike, version 2026.9.1301.0, SHA-256 BE3CC0D0218E335BA0B03EE4568C9CEB57485E4E32C06BFD1E1E871921331209. Trusted verification and runtime remain NOT_RUN. After product guest provisioning succeeds, install this package inside that guest only, derive `$(Get-AppxPackage -Name AiUsage.RoutingSpike).PackageFamilyName + '!App'`, activate it, capture Main/assembly identity, invoke Next then Back, and observe its launched PID exiting on close. Do not install it on the host.

## AC-06 — local build/checks PASS; remote and interactive CI NOT_RUN
Windows package job preserves existing immutable checkout/setup-dotnet/setup-bun pins, contents-read, no persisted checkout credentials and PR cancellation. Official upload-artifact v7.0.1 resolves through GitHub API to 043fb46d1a93c77aae656e7c1c64a875d1fc6a0a. Explicit unsigned-output allowlists only; no signing keys or remote publication. UI suite is published, not run on windows-latest. Manual unsigned candidates require explicit MsixVersion; ordinary validation uses source manifest version. Remote CI is NOT_RUN.

Actual existing checks: xUnit validator regressions 49 passed, zero failures/errors/skips; Bun workflow suite 12 passed, zero failed, 76 assertions (including the owner's pre-existing tool-gate test). Canonical validation initially rejected missing approval_basis/task metadata and a non-path done-task evidence field; metadata was corrected without changing validator rules. Final canonical validation is recorded below. Workflow YAML parses; its native unsigned package command ran locally and exited 0 with source version 2026.9.1301.0 and status unsigned-validation-only. This is not a remote workflow run.

## Focused independent review and targeted corrections
Native in-memory review session 01a09b02-5458-770f-aac5-6b6581cd662c used Anthropic Claude Opus 5, exactly read/grep/glob, memory off, Advisor off, no extensions or inherited conversation, zero initial messages. A seven-file SHA-256 fingerprint was frozen before its single review prompt. Response and usage were captured before disposal. Verdict: PASS on the source boundary, acceptance BLOCKED on guest proof; no blocking security/lifetime defect. Reported aggregate usage: 124,981 tokens including cache accounting, not a provider-product quota.

Addressed the medium evidence issue and scoped low findings: smoke now records an activation-attempt marker only after all harness prerequisites; guest negative inference requires that marker, otherwise reports HARNESS_PREREQUISITE_FAILURE. Removed the stale negative field. Guest reports dependency/runtime names, hashes and signer thumbprints. Replaced inherited AIU_DISPOSABLE_GUEST authorization with explicit -ConfirmDisposableGuest for non-Sandbox VMs. MSBuild resolution selects Current/Bin exactly; package directory uses a trailing forward slash. Startup recovery catches a second XAML/resource failure and exits nonzero rather than leaking an exception through async void. No full review loop was run.

Targeted verification: republished corrected smoke successfully; missing-AUMID invocation again exited 1 with zero activation markers. Host harness invocation with inherited AIU_DISPOSABLE_GUEST=1 still exited 1 at the new explicit guard, before trust/installation. Rebuilt and signed corrected product as 1305, retained previous candidates, and exercised unsigned CI packaging successfully under powershell.exe (Windows PowerShell, not PowerShell 7). Formatting commands exited 0; the product workspace loader emitted a warning, so native VS MSBuild output is the compilation proof. Actual startup-failure UI and positive activation-marker branches remain guest-dependent, NOT_RUN.

Local raw evidence is retained under the ignored .ai-usage-local/AIU-002 root. No feature completion claim is made while required guest proof is absent.

Final canonical check at 2026-09-13T13:55:53Z returned `{"valid":true,"diagnostics":[]}` with exit 0. Both final PowerShell scripts passed parser validation. The independent reviewer process exited 0 after preserving its result; its disposable runner is removed, while review evidence, package versions, public certificate and offline bundle remain retained.

## Owner stop decision
After the reachable local work and verification, the owner explicitly selected **Leave BLOCKED** instead of enabling Windows Sandbox or providing a disposable VM. Preserve code, signed candidates and the offline bundle. No host optional-feature changes, elevation or reboot are authorized by that selection. AIU-002 remains blocked and incomplete; do not start AIU-003.

## Authorized Sandbox resumption
The owner later requested unblocking/continuation and explicitly chose Windows Sandbox enablement with UAC elevation and required dependencies, without automatic reboot. Elevated `Enable-WindowsOptionalFeature -Online -FeatureName Containers-DisposableClientVM -All -NoRestart` completed with exit 0. Its report, started at 2026-09-13T15:04:43.9906323Z, records `ENABLE_COMMAND_SUCCEEDED`, state `Enabled`, `restartNeeded: true`, and `automaticRestart: false`. The report is retained at .ai-usage-local/AIU-002/sandbox-enable-result.json. No guest launch, host app installation or certificate trust import occurred. Acceptance remains BLOCKED until an owner-controlled host restart makes the disposable guest available; this is not a failed feature-installation command.

## Post-reboot execution checkpoint and owner redirection
The owner reported completing the reboot and approved the post-reboot guest plan. Root and OMP 18.1.19 were checked; both retained package hashes matched the records above. Sandbox guest-produced readiness was observed with only read-only staged input and a separate writable evidence directory; network, clipboard, audio/video input and printer redirection remained disabled. No host installation, host trust import, automatic reboot or remote action occurred.

Four fresh guest runs are preserved under .ai-usage-local/AIU-002: guest-evidence-1789316201653, guest-evidence-1789316609114, guest-evidence-1789318134245 and guest-evidence-1789318982408. The first exposed a Windows PowerShell StrictMode null-array failure at the SDK inventory guard. The harness now wraps the complete conditional pipeline in an array for runtime/SDK inventories; source and staging SHA-256 became B1109E9FC16643AB1720217EDB1CCBCCE2E07FC44C2DC2F0DCC0A6BDE84F3DB4. Host guard still rejected execution before trust or installation.

Runtime smoke exposed UIA3 ProcessId=0 for real windows. The retained-process ownership check now resolves each native HWND owner through GetWindowThreadProcessId; no process-name matching. Keyboard.Press left Enter down, so the smoke now uses Keyboard.Type for press/release. Native title-bar readiness is polled within the same fifteen-second startup deadline. Assertions on dashboard text, keyboard focusability, title-bar button, actual process exit/code and absence of process-owned windows remain intact. Multiple self-contained publishes succeeded. A 500 ms pre-capture compositor settling interval was added after visual inspection found one screenshot ahead of the painted frame; final report success alone is not visual acceptance.

Clean guest run guest-evidence-1789318982408 started at 2026-09-13T17:03:36.9433232Z. product/guest-report.json reports PASS_REQUIRES_EVIDENCE_REVIEW, positive PASS, runtime installer exit 0 and Microsoft.NETCore.App 10.0.12. Exact installed product remains 2026.9.1305.0 with unchanged hash. Original positive exit/title-bar/repeated-exit reports record passed=true, exited=true, exitCode=0 for PIDs 6156/3276/1716. Deployment event 628 proves missing Microsoft.WindowsAppRuntime.2 >=2.4.0.0, not a signature failure. Three activation-attempt markers and .NET Runtime event 1023 prove absent-runtime activation failures. Captured installer/event evidence is under orchestration. The final compositor-aware rerun reports positive PASS at orchestration/positive-visual-result.json; its three scenario JSON/PNG files are under product/positive-visual and still require the final consolidated inspection.

Original routing 1301 activated a genuinely blank native window, independently captured and inspected at orchestration/diagnostic-6628.png; this was not a renderer incompatibility verdict or merely failed PID lookup. Its empty root route had neither a view nor view model for the ContentControl navigator. Main and Second are now registered directly, with Main default. Routing project reuses the existing AiUsagePackageManifest override pattern. A temporary build runner derived from Build-Package.ps1 reserved routing version 2026.9.1302.0 under routing-reservations; native build/sign succeeded and host trust verification correctly failed closed. Old bytes remain retained. Guest signature was Valid, installed version checked, and routing-1302/report.json records Main -> Second -> Main, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b, PID 5112, exit 0 and no remaining window. Early captures could lag route presentation; the probe now waits 500 ms before capture. The final routing-visual/report.json records the same passing transitions and native identity with PID 6628, exit 0 and no remaining window. Final routing-visual PNGs have not yet been inspected.

The owner explicitly switched focus to Codex library/console integration before final AIU-002 closure. Keep spec implementing and backlog paused, not done. Remaining: inspect final product/positive-visual and routing-visual scenario reports/screenshots; record the 1302 package hash from orchestration/routing-1302-hash.json; consolidate acceptance; format final changed source; archive temporary probes/diagnostic publishes outside reusable staging; validate canonical documents. Guest ID 20bdc993-7e0c-4bca-b006-07fe4f7ddfd8 was retained; do not dispose it before evidence inspection. The final supervised verification process exited 0. Remote CI remains NOT_RUN. No AIU-002 code work should displace the owner's newly selected provider work.

## Final acceptance review, 2026-09-14
Resumed on the owner's decision to close AIU-002 before further provider work. No guest was launched and no code behavior changed in this pass: the artifacts below are from the retained clean run guest-evidence-1789318982408, and the review is inspection plus record consolidation.

Inspected scenario reports of the compositor-aware rerun, `product/positive-visual`: `exit` PID 5248, `title-bar` PID 3136 and `repeated-exit` PID 4144, each `passed: true`, `exited: true`, `exitCode: 0`. `orchestration/positive-visual-result.json` reports positive PASS with exit code 0.

Opened the actual screenshots rather than treating file existence as proof. `product/positive-visual/exit.png` shows the installed packaged window titled "AI Usage" with the "Dashboard" heading, the "No accounts connected." empty state and the focusable "Exit" button. This is the offline installed UI, not a development-host window.

Routing 1302 is accepted: `routing-1302/report.json` records AUMID `AiUsage.RoutingSpike_951d0pt9hnds0!App`, PID 5112, observed states `Main` -> `Second` -> `Main`, assembly identity `Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b`, `exited: true`, `exitCode: 0`, `remainingWindow: false`, verdict PASS. `main.png` and `second.png` visually confirm the Main and Second routes and the displayed runtime identity. The guest routing package hash from `orchestration/routing-1302-hash.json` is AED8DCCE926886BF530E5B3D23A2957E8E5F06AB84033F74B1D3E1E3B8E044D0; the earlier 2026.9.1301.0 bytes remain retained.

One honest limitation: `routing-1302/back.png` still shows the Second route, so the return to Main is proven by the UIA-observed state sequence and `lastObservedState`, not by that screenshot. The capture ran before the back navigation repainted. The 500 ms settling interval fixed the product capture but not this one. Acceptance rests on the observed state transitions; the screenshot gap is recorded as a non-blocking follow-up rather than described as visual confirmation.

Acceptance matrix:

| Criterion | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | Native three-project solution builds; installed packaged product activates in a clean guest |
| AC-02 | PASS | Installed offline UI shows Dashboard, empty state and keyboard-focusable Exit; three scenarios passed with inspected screenshots |
| AC-03 | PASS | Each scenario ended with actual retained-process exit code 0 and no process-owned window left |
| AC-04 | PASS | `Build-Package.ps1` produced signed 2026.9.1305.0, hash B3D71F053423669FEF3062BA2520F93A1CBDF2F36F5DF7A0D67C6915D868C854; guest signature Valid; host verification fails closed on the untrusted development root; version reuse rejected without byte changes |
| AC-05 | PASS | Routing 1302 navigated Main -> Second -> Main under Microsoft.WinUI and exited 0; the 1301 blank-window failure was diagnosed as a missing root-route view, not renderer incompatibility |
| AC-06 | PASS locally, remote NOT_RUN | Validator regressions, Bun workflow suite and canonical validation pass locally; GitHub Actions execution and interactive UI CI remain NOT_RUN by authorization, not by failure |

Negative prerequisites stay genuinely observed, not simulated: deployment event 628 proves the missing `Microsoft.WindowsAppRuntime.2` framework, and three activation-attempt markers with .NET Runtime event 1023 prove absent-runtime activation failure. No host runtime was uninstalled to manufacture a clean snapshot.

## Sources
- https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json
- https://www.nuget.org/packages/Microsoft.WindowsAppSDK/2.4.0
- https://www.nuget.org/packages/Uno.Extensions.Navigation.WinUI/7.3.6
- https://github.com/unoplatform/uno.extensions/tree/c12a3c95060137ee51118d2f171775aae0fa2a3b
- https://raw.githubusercontent.com/FlaUI/FlaUI/v5.0.0/src/FlaUI.Core/Application.cs
- https://github.com/actions/upload-artifact/tree/043fb46d1a93c77aae656e7c1c64a875d1fc6a0a
