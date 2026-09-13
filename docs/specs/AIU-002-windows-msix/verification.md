# AIU-002 verification

## Authorization and code reference
Owner selected AIU-002 and disposable Sandbox/VM verification, then approved execution of the Windows MSIX plan. Local branch: feature/AIU-002-windows-msix. Existing AIU-001 changes and an unrelated shortcut were preserved. No remote actions, host package installation, host trust import, elevation or reboot occurred. No bounded execution JSON or budget was fabricated.

The feature remains incomplete: installed UI, process termination, dependency-negative clean guest and native routing runtime proof are BLOCKED. Build and source evidence below do not substitute for those criteria.

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

## Sources
- https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json
- https://www.nuget.org/packages/Microsoft.WindowsAppSDK/2.4.0
- https://www.nuget.org/packages/Uno.Extensions.Navigation.WinUI/7.3.6
- https://github.com/unoplatform/uno.extensions/tree/c12a3c95060137ee51118d2f171775aae0fa2a3b
- https://raw.githubusercontent.com/FlaUI/FlaUI/v5.0.0/src/FlaUI.Core/Application.cs
- https://github.com/actions/upload-artifact/tree/043fb46d1a93c77aae656e7c1c64a875d1fc6a0a
