Historical record, frozen 2026-10-06 (AIU-045 AUD-10); not maintained.

# Screenshot gallery — interim evidence

The real unpackaged Windows application is running with synthetic data in a network-disabled Windows Sandbox. All 170 baseline scenarios have captured Used/Left images and passed native assertions. The overview and pages 001–012 and 018 have been visually inspected. Remaining inspection and final-build recapture are pending; this is not the complete final gallery.

[Screenshot index](screenshot-index.csv) maps every baseline scenario to its actual image and exact card ID. [Controls](controls.csv) and [additional scenarios](additional-scenarios.csv) record the remaining editor, history, authentication, recovery, transition and lifetime coverage. Generated images remain outside Git.

Inspected captures (local, git-ignored paths):

- Synthetic four-provider overview, Used: `.ai-usage-local/ui-audit/sandbox-probe/evidence/corpus-gallery/overview-used.png`
- Synthetic money overage, Used: `.ai-usage-local/ui-audit/sandbox-probe/evidence/corpus-gallery/page-018-used.png`
- Synthetic money overage, Left: `.ai-usage-local/ui-audit/sandbox-probe/evidence/corpus-gallery/page-018-left.png`

All current baseline captures: `.ai-usage-local/ui-audit/sandbox-probe/evidence/corpus-gallery`. Native first-run and duplicate-provider captures: `.ai-usage-local/ui-audit/sandbox-probe/evidence/controls-gallery`. Failed earlier attempts are preserved separately under `sandbox-probe/evidence/failures`; lock-screen captures are excluded.

See [the report](report.md) for build hashes, execution results and reproduction commands. A generated capture establishes neither visual inspection nor successful interaction by itself. Final evidence must follow all remaining fixes.
