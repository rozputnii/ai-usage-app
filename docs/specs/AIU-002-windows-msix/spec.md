---
id: AIU-002
type: spec
status: implementing
goal: G-002
scope_version: 1
approval_basis: owner-selected-AIU-002-and-approved-windows-msix-execution-plan
---
# First runnable Windows MSIX and smoke CI

## Authorization
The owner selected AIU-002, selected disposable Sandbox/VM installation verification instead of host trust, and approved execution of the corresponding Windows MSIX plan in this session. This is local feature authority, not authorization for all G-002, remote publication, paid services, provider accounts, host app installation, host certificate trust, elevation or reboot. No bounded execution budget is inferred.

## Scope
Native WinUI on .NET 10, Windows 11 24H2+ x64; three production projects, framework-dependent .NET and Windows App SDK, Generic Host, resource-backed empty dashboard and coordinated exit. Core remains platform-neutral. No database, credentials, providers, tray, charts, updater, AOT or trimming. Closing exits until tray integration exists. A separate native Uno.Extensions.Navigation spike does not change the one-page product composition.

## Acceptance criteria
- AC-01: Three production projects build for the selected native Windows target, with Core platform-neutral and both deployment modes framework-dependent.
- AC-02: Installed package launches offline and displays the honest empty dashboard, with a usable keyboard-accessible Exit action.
- AC-03: Exit and title-bar close both stop Host and terminate the actual app process; repeated exit requests do not race disposal.
- AC-04: Reusable local self-signed MSIX, package identity/hash/version recorded, clean supported Windows dependency installation and launch proven without a developer SDK/Visual Studio in the guest.
- AC-05: Standalone native routing spike has a reproducible feasibility verdict, including forward/back runtime proof if compatible; incompatibility is recorded without silently replacing native WinUI.
- AC-06: Existing checks remain intact; Windows package build and smoke harness are wired into CI with truthful separation of build, interactive smoke, and remote execution evidence.

## Fixed contracts
Product identity AiUsage.Dev; publisher CN=AI Usage Development; application Id App. Spike identity AiUsage.RoutingSpike. Minimum Windows version 10.0.26100.0. Package versions use YYYY.M.DDNN.0 with NN 01..99. Sign only with an explicitly selected CurrentUser/My development certificate; export public CER only. Guest trust changes occur only inside disposable Windows. No host installation or trust changes.

Primary owns canonical records, shared solution/build configuration, CI, integration, signing and the interactive desktop. Routing and smoke source ownership may be isolated and disjoint. Required missing guest/package proof blocks completion; remote CI remains NOT_RUN absent an authorized run.
