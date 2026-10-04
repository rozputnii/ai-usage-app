# Synthetic Windows UI/UX audit — in progress

**The requested comprehensive audit is not complete.** Deterministic coverage and repairs have been implemented and executed. The host Windows console remains locked. At the owner's follow-up request, a network-disabled Windows Sandbox supplies an independent interactive desktop: the actual synthetic overview Used/Left mouse/capture/exit scenario passed and its images were inspected. The remaining corpus is now running there. Additional composite replay, control-driver and gallery gaps are still recorded explicitly; the successful overview does not make those rows pass.

## Deliverable records

- [Supported behavior inventory](inventory.md): provider families, layouts, states, scales, precedence, invalid/unreachable combinations and source references.
- [Coverage matrix](coverage.csv): 170 stable parser-to-recorder-to-budget-to-projection scenarios, independent expected values, controlled clocks and native verdicts.
- [Additional scenario inventory](additional-scenarios.csv): 39 composite/transition/lifetime/history/recovery requirements, supporting tests and remaining exact replay gaps.
- [Control inventory](controls.csv): 69 mouse-action/variant rows, 21 keyboard rows and three non-applicable visible-control requests. Used/Left physically verified; remaining controls pending.
- [Gallery status](gallery.md) and [screenshot index](screenshot-index.csv): every corpus scenario mapped to a planned readable Used/Left page and exact synthetic card ID; captured rows remain separate from rendered inspection.
- [Implementation/handoff plan](plan.md).
- [Independent checkpoint review and finding dispositions](review.md).

## Product defects fixed

| Defect | Change | Regression evidence |
| --- | --- | --- |
| Successful empty provider reading rendered “not ready” | Uses existing NoDisplayedLimits state for a successful empty quota; startup remains distinct | LiveLedgerProjectionTests.SuccessfulEmptyQuotaUsesTheSupportedNoDisplayedLimitsState, all four providers; failed before fix |
| Timestamp resets/readings/failures were formatted as UTC clocks in a local-time UI | Convert presentation timestamps, marks and retry time to selected timezone, retaining their instants and date-only precision | LiveLedgerProjectionTests.TimestampResetAndReadingClocksUseTheSelectedLocalZone; Lisbon summer UTC/local difference; failed before fix |
| Supported money precision stopped at six decimals | Formatting/editing/parsing supports the contract's 0..18 exponents | AuditFormattingTests supported exponents and tiny known values; failed before fix |
| Cap input silently accepted a foreign symbol or malformed separators | Strip only the selected money denomination at the boundary; validate English grouping; reject currency symbols on abstract credits | AuditFormattingTests foreign USD/EUR, misplaced code, invalid comma grouping, credits; failed before fix |
| Money text lost cents through double drawing conversions, including small overflow disappearing | Decimal display arithmetic retained; double conversion restricted to drawing geometry/weights | AuditFormattingTests.CardTextKeepsNativeMoneyPrecisionWhenGeometryNeedsDoubles and SmallOverageOnALargeMoneyAmountKeepsItsOverflowLabel; all three cases failed before fix |

These are formatting/projection repairs within existing behavior. No provider endpoint, feature, dependency, credential lifecycle, product layout or installed application was changed. Trivial formatting needs no new logging; existing diagnostics remain in use.

## Test and build results

Audit base: `main` at `383644c`. Initial checkout was clean. Deterministic tests and product builds correspond to production/test sources saved at `2942e6feec44292763799b15660a9853f9087306`; subsequent review corrections affect only the native driver and its guards. The corrected driver was compiled and its blocked execution repeated from sources saved at `ae4f483129d99f8b176c306531b793738b1d0054`. Both checkpoints were pushed to main; the remote head was verified. Native executable: `src/windows/AiUsage.Windows/bin/x64/Debug/net10.0-windows10.0.26100.0/win-x64/AiUsage.exe`. EXE SHA-256: `3A25A0E283144D66D6A58847ED3F4985E5DC41813911D4958402E11838D8763B`; application DLL SHA-256: `F8D1FFEBF30C5065BCF637E91C41D47F4F315CD1669FBF509180E3F925434C16`. Environment: Windows console session, .NET SDK 10.0.401, ordinary host display settings unchanged.

| Check | Actual result |
| --- | --- |
| Infrastructure baseline | PASS 613/613 |
| Presentation baseline | PASS 128/128 |
| Final deterministic Infrastructure so far | PASS 783/783, zero skipped; includes 170 corpus cases and serialized observation replay assertions |
| Final deterministic Presentation so far | PASS 161/161, zero skipped; 33 added tests relative to baseline |
| Project validator regressions | PASS 80/80 |
| Debug unpackaged app build | PASS, zero warnings/errors |
| Windows test driver compilation | PASS, zero warnings/errors; does not establish interactions |
| Unsigned MSIX validation build | PASS 2026.10.409.0, identity/version validated, no installation/signing; external tooling warns mspdbcmf.exe is unavailable so no symbols package |
| Actual initial Ledger smoke | BLOCKED after launching isolated unpackaged app: could not acquire foreground input; three foreground failures before stopping the suite; no valid app evidence |
| Read-only desktop prerequisite after harness correction | BLOCKED: LockApp/LogonUI input; xUnit reports one failed prerequisite, not a product defect |
| New replay/control/auth native driver | BLOCKED 11/11 attempted tests at the input guard; repeated after review corrections with the same blocker; no application interaction/capture executed |
| Windows Sandbox desktop prerequisite | PASS; independent guest session while host stays locked |
| Actual synthetic overview in Sandbox | PASS one native test: Used/Left clicks, four expected card states, ten captures and clean exit; Used/Left overview images inspected |
| Parser corpus in Sandbox | In progress; completed page results are recorded individually in coverage.csv; a single looping test is not counted as passing before all its pages finish |
| Final screenshots and rendered visual inspection | In progress; overview captured, remaining corpus/control/gallery evidence pending |
| Document validation | PASS, no diagnostics |
| Integrated/fresh independent review | Source review executed against frozen checkpoint; two material driver findings corrected and compiled. Their guards now permit verified owned guest interaction; see review.md |

Local execution logs/results: `.ai-usage-local/ui-audit/infrastructure.xml`, `presentation.xml`, `desktop-prerequisite.xml`, `native.xml`, `native-after-review.xml`; earlier regression failures stay separately in `product-regressions-red.xml`, `native-precision-red.xml`, `overflow-precision-red.xml`. Generated output and all captures remain outside Git. Invalid lock-screen captures are excluded from deliverables and publication.

Harness corrections are distinct from product fixes. Shared-project concurrent compilation collided in compiler output; sequential reruns passed. The first input-desktop-name probe falsely considered Default sufficient while LockApp still owned input; a foreground process check was added and its actual read-only rerun correctly reported BLOCKED. Audit root validation now runs before diagnostics creates directories, requires explicit `--demo`, a synthetic marker and an isolated marked temporary root, and rejects redirection. Replay includes the observations actually submitted by the production recorder, so cached startup cannot lose first-observation baselines. PowerShell page generation retains ISO timestamp strings rather than converting them through the host timezone. Rehydrated corpus inputs were actually rerun against the real store and projection.

## Reproduction and continuation

Use PowerShell 7.5+ from the repository root. All provider data is generated from synthetic JSON in AuditScenarioTests; no credential fields or provider services are present in the explicit replay composition. The native app starts from the parser-normalized exported inputs. Parser execution and real Windows rendering are separate linked stages, not a claim that a mocked transport was exercised inside the native process. Live HTTP/quota/authentication coverage is excluded by the current request.

```powershell
$env:AIU_AUDIT_INPUT_DIRECTORY = Join-Path (Get-Location) '.ai-usage-local/ui-audit/inputs'
dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -xml .ai-usage-local/ui-audit/infrastructure.xml
dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -xml .ai-usage-local/ui-audit/presentation.xml
./tools/windows/New-UiAuditPages.ps1 -InputDirectory .ai-usage-local/ui-audit/inputs -OutputDirectory .ai-usage-local/ui-audit/pages -CoverageDirectory docs/workflow/ui-ux-audit-2026-10-04
dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Debug -p:Platform=x64 -p:WindowsPackageType=None --no-restore
dotnet build tests/windows/AiUsage.Windows.Tests -c Release --no-restore
$env:AIU_SMOKE_EXE = Join-Path (Get-Location) 'src/windows/AiUsage.Windows/bin/x64/Debug/net10.0-windows10.0.26100.0/win-x64/AiUsage.exe'
$env:AIU_AUDIT_PAGE_DIRECTORY = Join-Path (Get-Location) '.ai-usage-local/ui-audit/pages'
$env:AIU_SMOKE_EVIDENCE_DIRECTORY = Join-Path (Get-Location) '.ai-usage-local/ui-audit/gallery'
dotnet run --project tests/windows/AiUsage.Windows.Tests -c Release --no-build -- -class '*AuditDesktopPrerequisite' -parallel none -xml .ai-usage-local/ui-audit/desktop-prerequisite.xml
# Only after the prerequisite passes on an unlocked desktop:
dotnet run --project tests/windows/AiUsage.Windows.Tests -c Release --no-build -- -class '*AuditWindows' -parallel none -xml .ai-usage-local/ui-audit/native.xml
```

The driver performs physical clicks, checks fake request receipts, and records native assertion results separately from visual inspection. Guest runs exposed harness issues: WinUI UIA ProcessId=0, denied programmatic activation, a replaced UIA provider during capture, and a Note-layout expectation incorrectly requiring retained usage to be visible. Native ownership, an owned-caption activation click, one fresh capture binding retry and contract-specific independent expectations correct these issues. Actual reruns are required and recorded; no compile/source check upgrades a row to PASS. Remaining controls/composite cases must be implemented and exercised as listed, including native tray Open/Refresh, physical caption close, restart persistence, all tooltips, overlay/snapshot transitions, actual isolated recovery/cleanup and in-flight exit.

Sandbox continuation uses Windows Sandbox CLI, guest Windows 11 build 26100, and a self-contained unpackaged Release build. Networking and clipboard/device sharing are disabled. Only the synthetic input staging directory (read-only) and an initially empty evidence directory (writable) are mapped. The host console stays locked; no authentication, trust, display settings or installed-app changes are involved. Source reference `f5adf034ebc70d91259b1d645dc56531d3a8bdf8`; app DLL SHA-256 `17EB1F30BE31A597B9472192F06882B8632628372CC71D4C3D15BE83069DFAE2`. Release build and driver publish commands:

```powershell
dotnet publish src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Release -r win-x64 -p:Platform=x64 -p:WindowsPackageType=None -p:SelfContained=true -p:WindowsAppSDKSelfContained=true -o .ai-usage-local/ui-audit/sandbox-probe/input/app -v:minimal
dotnet publish tests/windows/AiUsage.Windows.Tests -c Release -r win-x64 --self-contained true -o .ai-usage-local/ui-audit/sandbox-probe/input/smoke --no-restore -v:minimal
```

```powershell
dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo
dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json
git diff --check
./tools/windows/Build-Package.ps1 -MsixVersion 2026.10.409.0 -OutputDirectory .ai-usage-local/ui-audit/packages -NoRestore
```

The recorded package command already produced that version and intentionally refuses overwrite. A changed/repeated package build needs a fresh unused local version. Package SHA-256: `7508F40F7C82CFF7852C2BEADB64071D9F3018FE8CFADFFA7D5EEF620F914F1B`. It is unsigned validation evidence, not release/install evidence. Git save points use `[skip ci]` to avoid the main-push Preview release workflow; no release is authorized by this audit.

Completion requires real native outcomes and inspected readable final captures for every applicable row, plus the remaining exact scenario/control coverage and required review. No screenshot or full-coverage completion is claimed while these gaps remain.
