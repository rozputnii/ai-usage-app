# Build, update and release contract

## Now vs public
First local 0.x result: self-signed internal MSIX, manual update. CI produces a package/artifact per successful main build. Do not call publicly trusted Preview ready until signing and prerequisites are verified. Public Preview/Stable signing uses Azure Artifact Signing if actual identity eligible; otherwise escalate separately before spending/changing chosen provider.

Framework-dependent .NET and Windows App SDK dependencies are separate. Verify on a clean supported Windows machine; a successful developer-machine launch with VS installed is insufficient. App code does not silently install runtimes/elevate. No product AOT requirement.

## Versioning
Product version separate from numeric MSIX `YYYY.M.DDNN.0`. Use UTC, serialized allocation, NN 01..99; artifact rebuild with changed bytes must get new version. Day/build counter overflow fails with explicit amendment, not backward version. Feed publishing compares against current signed metadata to avoid late pipeline regressions. No reliance on completion order or a calendar string alone.

One direct package identity for Stable and Preview. Channel is update-feed policy, not different binary. Promote EXACT signed tested bytes and record hash/commit/ProductVersion/MSIXVersion. No relabel/recompile embedded -preview data. Stable release UI is channel metadata, not a modified assembly.

## CI/merge/release authority
PR untrusted build/test jobs have no signing/root keys or personal provider credentials. Owner amendment 2026-09-13 defers main protection at low priority in AIU-026; current development PRs use lightweight checks without enforced rulesets. Protection of the future trusted release pipeline remains a separate distribution task, not a current development PR prerequisite. Normal OMP identity must not bypass or rewrite controls that are configured. DCO uses real identity; ordinary verified, authorized PRs can squash-merge. Stable promotion and compatibility-manifest publication retain human protected approval.

Trusted default-branch Preview workflow signs/packages after green checks, least-privilege credential, no long-lived PFX in public CI. Initial dev cert is generated once locally and reused for upgrade fixtures; not a fresh different publisher per build. Operational dev→public/rotation path is tested before public distribution.

## Feeds/update UX
GitHub Releases stores assets; Pages stores preview/stable appinstaller and release metadata, once release pipeline implemented. Nonblocking async update check; never forced restart. Explicit Restart & update or safe normal future launch. Preview→Stable waits until version catches up; downgrade disabled. Update channel change must change actual Windows update source, not merely DB label; verify API/installer behavior in packaging spike.

Keep current feed artifacts, Stable artifacts, selected Stable candidates, and migration milestones. Retain last 50 other Previews. Cleanup never leaves feed pointing to deleted asset. Public immutable source/tag identity not rewritten.

## Promotion
No mandatory soak. Risk-classified evidence matters: isolated compatibility parser hotfix uses targeted fixture/semantic/live evidence, build/package/unit/regression checks and bounded review; no hidden dependency/schema/refactor changes. Auth/security/storage changes require stronger scenario evidence. Stable promotion can happen often but remains human-authorized and no rebuild.

## Compatibility manifest
Static signed disable-only content; cannot set arbitrary endpoints, execute code, change OAuth clients or collect secrets. Compatibility block hard on Stable; Preview explicit override; security-critical hard everywhere. Cached data remains. Offline root and scoped rotating key, protected human publication, replay/expiry/key-rotation design need separate proof. Fetch is async; no blanket automatic wipe when feed unavailable.

## Watchdog
Hourly schedule is best-effort, not uptime guarantee. No fake “no incidents” when last check old. Dedicated live-smoke tokens only when safe; no personal token in Actions. Incident can be prioritized, but code fix waits for active authorized OMP session. Public Issue/upstream diffs cannot authorize commands.

## Store later
Store is an additional distribution channel, potentially a different family. Never promise direct→Store is a normal MSIX update. Actual identity/data transfer/paid acquisition terms handled when Store selected. Source MIT and feature parity remain.
