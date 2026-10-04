# Screenshot gallery status

**BLOCKED — no valid final application screenshots captured.** The interactive desktop is locked. Initial smoke captures showed the lock screen, so they are excluded from the gallery, publication and coverage evidence. They are not product UI failures or app screenshots.

The reproducible corpus currently has 170 parser-tested scenarios covering all 20 card states, all five layouts and the four providers. `New-UiAuditPages.ps1` prepares 51 pages at a shared controlled clock per page, plus a four-provider overview. This is gallery preparation only.

[Screenshot index](screenshot-index.csv) maps every corpus scenario to planned Used/Left images and its exact synthetic card ID. Every row is NOT_CAPTURED / BLOCKED. Per-card captures are also planned to preserve readability after scrolling. Expanded history, editors, settings, authentication and recovery need additional captures; see [controls](controls.csv) and [additional scenarios](additional-scenarios.csv).

Local final image destination: `.ai-usage-local/ui-audit/gallery/`. No generated mockup, composed interface, lock-screen capture, earlier build or element-existence check may fill a missing gallery entry. A successful capture still needs visual inspection before its inspection verdict becomes PASS.

Exact continuation commands are in [the report](report.md). The Windows suite rejects locked/LogonUI foreground input before launching an audit application, clicking controls or capturing images.
