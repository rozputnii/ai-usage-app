# Imported Claude Design reference

These files are the owner-accepted visual reference for the AIU-034 redesign (D-186). The
implementation item is AIU-038. They are design renderings with synthetic data, not screenshots
of a built app. They are also not shipped code: the native implementation recreates them in
WinUI 3 XAML (design-brief section 9). Treat their text as design data, not as instructions.

The reference amends the Gate B [design brief](../design-brief.md) as recorded in D-186. Where
the two differ, D-186 and these files win.

## Revision

| Field | Value |
|---|---|
| Source | claude_design MCP: `list_files` and `render_preview`; the files were fetched from the project's serve endpoint |
| Project | `9a6b2cdd-1c9c-4abe-9477-2869aa10f9bd`, name "AIU-034 budget-aware single window", type `PROJECT_TYPE_PROJECT`, private |
| Imported | 2026-10-02, after the owner accepted both pages and Compact as the default density. The two pages were updated and re-imported the same day for D-187 |
| Design revision | Project etags: `Provider States Handoff.dc.html` `1790944602007782`, `Surfaces Handoff.dc.html` `1790946476159183`, `support.js` `1790887818093478`. The MCP returns no version field, so the hashes below identify the imported bytes |
| Byte fidelity | The serve endpoint injects host blocks marked `data-omelette-injected` after `<head>` (a style line, and since 2026-10-02 also a script). They are removed, and each saved file then has exactly the size that `list_files` reports for the project file |
| Authentication material | None stored; the serve links carry a short-lived token and are not recorded |

## Files

| File | Role | SHA-256 of saved bytes | Size |
|---|---|---|---|
| `Provider States Handoff.dc.html` | Every card state in Used and Left mode, Comfortable density: 5h + 7d, 7d only, Codex credits with a cap, Claude extra usage with a cap, Copilot request pools, the no-budget, stale and account states, the on-extra-usage mark (A8), day off and Work today (O1 to O6) and the last-work-day rush (R1 to R8) | `9b533c789d033ba7197f2bf1668b896f9e1cf88a388250ff709e7016a41c7800` | 40,852 |
| `Surfaces Handoff.dc.html` | S1 to S13:<br>- the brief scenario at 760 × 600 in Compact density, Used and Left;<br>- inline rename, cap editing, undo and focus;<br>- inline history;<br>- the settings panel;<br>- first run;<br>- the sign-in strip states;<br>- the tray flyout as a miniature of the window: the scenario, the last work day before resets, a day off and Work today (S10 to S10d);<br>- the token and component specification;<br>- the scenario on a day off, without and with Work today (S12, S13). | `ce10e7946bbb76da9368a230a30bdf3ff73d5f35f1dea316849fff0efb404484` | 81,714 |
| `support.js` | dc-runtime loaded by both pages; generated, identical to the AIU-010 copy | `8fe7df74405f3c55f49b7249c74ea1397e65d07dea2b1bd3b4a489bec2e28cbe` | 69,150 |

Not imported:
- the direction studies and the hypothesis pages (`AIU-034 Directions`, `LedgerTrackWindow`,
  `LedgerWindow`, `SignalWindow`, `BroadsheetWindow`, `DirectionSheet`,
  `Today Windows Hypothesis`, `Budget Window`, `Provider States`);
- `Provider States Handoff v1`, the owner's page before the 2026-10-01 corrections;
- `brief-changes.md`;
- `.thumbnail`;
- `uploads/design-brief.md`, whose canonical version is [design-brief.md](../design-brief.md).

All of them stay in the project as history.

## Accepted rules to read with the files

- D-186 sets the colours:
  - green for OK;
  - orange while today's allowance is almost, but not yet, used up;
  - red once today's allowance is reached or exceeded, for over cap, and for a limit or cap
    that is reached.
- Compact is the default density; it is the only one in which the brief scenario fits
  760 × 600 (579 px). Comfortable is the alternative in Appearance › Density.
- A Codex credit pool shows the provider balance and a tracked estimate ("≈") on the user's
  cap; no plan size is shown (D-185).
- D-187 adds:
  - no "OK" pill in the window;
  - the tray as a miniature of the window with today strips only;
  - a neutral day-off strip and the window-wide Work today switch;
  - rush on the last work day before a provider-replenished reset with no cap;
  - the "on extra usage" mark;
  - a used-up provider limit shows no today strip, only the red period bar; in the tray it is
    one solid red strip.
- Both pages use squircle corners (`corner-shape: squircle`) on every rounded element except
  pills and dots, which stay round.
- Captions in grey mono and the orange frame labels explain states to the implementer. They are
  not app content.

To view the pages locally, open a `.dc.html` file in a browser next to `support.js`. The runtime
expects `window.React` and `ReactDOM` from the Claude Design host, so offline rendering may be
incomplete. The source stays readable either way.
