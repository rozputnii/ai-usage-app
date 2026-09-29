# AIU-034 verification

## T-14 independent brief review - 2026-09-29

Primary [astra] review of base `9115984a67af971338819356c952edbd2d2126ab`. Initial
`git pull`: already up to date; `main` and working tree clean. T-14 was set in-progress
before review. This is a fresh review of the committed brief against the accepted spec and
research, not reliance on T-12/T-13's results. The convergence-review skill was applied by the
primary; no subagents or write workers were used.

**Result:** arithmetic PASS; brief consistency FAIL pending **four open findings, F-13 to
F-16**, for T-15. No mechanical correction was needed; design-brief.md is unchanged.
Publishing the review does not approve the design or resolve its findings. Only T-14's task
and verification records changed; no product code, research rules, provider records or backlog.

### Clause-by-clause review (Step 1)

Locations are in design-brief.md unless stated otherwise. PASS means the brief carries the
clause, not that a prototype or implementation has passed it.

| Clause | Brief evidence and result |
| --- | --- |
| B-1 user and all four questions | PASS: section 2 names the developer and work/personal subscriptions, copies all four questions verbatim and maps each to a displayed element. |
| B-2 kinds and every Phase A state | Catalogue checked against research 5.4, 6.4, 6.5, 7.5, 8.1, 8.5 and 8.8. Sections 3.1-3.12 cover native kinds, unknown/stale/assumed/exhausted, signed out/expired sign-in, day off/not-ready estimate, caps set/unset and currencies. FAIL on unlimited/cap and session rule overlaps, F-13/F-14; other required states are present. |
| B-3 R-01/R-12/R-13; hierarchy; editing | PASS: 5.1 provider/account/limit hierarchy; 5.2 inline rename/caps/confirmation/undo and immediate sign-out; 5.3 inline settings and secondary tray. |
| B-3 decision information and density | Coverage present in 5.4, DA-6 and PA-2: no tooltip-only decision information, four-account reference. Alternative default size conflicts with section 9, F-16. Actual layout fit NOT_RUN. |
| B-4 forbidden items | PASS: all five bullets copied verbatim in section 6; DA-2 explicitly checks all eight constituent prohibitions plus R-08's default theme; PA-7 repeats the check. |
| B-4 concept and palette | PASS: 6.1 name/rationale and consequences for palette/type/visualization; 6.2 all six semantic colours, dark surfaces, text/non-text contrast including stale; DA-1/DA-3/PA-8. |
| B-4 type and visualization | Coverage present: 6.3 distinctive pairing/tabular figures; 6.4 bar/budget/pace/sessions with per-kind exceptions; DA-4/DA-5. Exhausted marks need F-15. |
| B-4 kept clauses | PASS: 6/9 retain dark-only, non-colour status, WinUI 3/Windows 11 and licensed MSIX fonts; source checks below. |
| B-5 directions and selection | PASS: 7.1 requires two or three distinct directions and owner selection before 7.2. |
| B-5 prototype and surfaces | PASS: 7.2 requires every section 3 state, main window, tray, settings, inline editing/confirmation, first run and sign-in strip; PA-1/PA-11. History and undo also included. |
| B-5 specifications/accessibility | PASS: 7.2 tokens/components, keyboard, accessible-name and reduced-motion notes; PA-9/PA-13. |
| B-6 both acceptance checklists | Present: DA-1-DA-7 and PA-1-PA-13. FAIL on consistency issues F-15/F-16, not on missing checklists. |
| R-01 | PASS: 3.1/5.1 include scoped windows/pools in one main window, inline names/status, no account-detail/modal/pop-up view; PA-11. |
| R-02 | PASS: 3.1 preserves percent/requests/credits/money; 3.12/PA-6 forbid conversion, sums of limits and combined percentages. |
| R-03 | 3.8/5.2 carry local caps, native units, provider/cap separation and lower-of-two binding; B3 demonstrates cap with unknown provider limit; percentage windows have no cap. FAIL for unlimited with cap, F-13. |
| R-04 | PASS: 4/5.3 use one global Monday-Friday default; 7.2 exposes its settings. |
| R-05 | PASS: 3.6/4 carry U0/U, fixed N, partial-day T, baseline B, deviation, used/left today and recomputation triggers; 3.5 carries Wr = 0. All scenario calculations match research 8. Zero-limit exceptions follow D-185. |
| R-06 | PASS: 3.3/4 preserve supplied resets, label assumed starts/month bounds, and keep unknown-duration periods unknown. Copilot displays the date only and uses UTC month subtraction for its assumed start. |
| R-07 | 3.7/A2 carry weekly/today sessions from the same pool, ready-only display and estimate labels; 3.1 excludes shared estimates for scoped limits. FAIL on zero versus less-than-one, F-14. |
| R-08 | PASS: section 6 and DA-2(i)/PA-7 reject Claude Design's default theme/colours. |
| R-09 | PASS: 3.10/A1/B1/6.4/PA-4 carry amber at <= 30 %, red at <= 10 % or exhaustion, countdown beside the bar from amber, no hour segments. Red takes precedence within amber's threshold. |
| R-10 | PASS: 3.11 gives accepted precedence, names the binding limit and dims stale status; all four 4.4 results reproduce it. |
| R-11 | PASS: 3.1/3.5 exclude five-hour daily budgets; day-off budget is neutral while 3.4/3.11 preserve exhausted/over-cap states. No holiday/vacation feature is introduced. |
| R-12 | PASS: 5.2/7.2/PA-11 carry in-place Confirm/Cancel, undo and immediate sign-out retaining history, consistent with D-093. |
| R-13 | PASS: 5.3/7.2/PA-11 keep settings inline and tray secondary. |
| R-14, amended by D-184 | PASS: 3.2/5.1/7.2 require inline local history and gaps; provider history is excluded. |
| R-15 | Explicit rules present in 3.2/3.3/3.7/3.12 and PA-5/PA-6 for unknown, stale, estimates, assumed bounds and no combined percentages. F-13/F-14 qualify edge-state consistency. |
| D-183 | Coverage present for single-window structure, subscription limits, caps, work days, adaptive/baseline budgets, periods, sessions, five-hour colours and binding status, subject to F-13/F-14. Sections 2/3 enumerate subscription families, not API billing. Section 5.5 keeps current behaviour until implementation; Gate A preceded Phase B. |
| D-184 | PASS: 5.1 excludes provider history; 1/3.6/3.7 reference accepted local-series derivations for day-start, tracked use and sessions. No new transport or retrieval removal is performed. |
| D-185 | Balance-only cap/estimate (3.1/3.6/B3), UI-only exclusion (3.12), period unknown (3.3/D2), day-off precedence (3.4/3.11) and zero entitlement/cap (3.4) are present. Unlimited's cap exception needs F-13. The future CLI attribute does not resolve PD-034-02 or start AIU-005. |

The T-13 25-row B-4/B-5/B-6 mapping was re-read against the actual spec and brief, including
the verbatim lists. Gate A amendments govern R-05/R-06/R-11/R-14; superseded literal readings
were not reinstated.

### Independent recomputation (Step 2)

PASS: newly authored `recompute_t14.py` in the session temporary directory parses section 4's
tables and recomputes from their inputs using exact fractions and `Europe/London` zone rules.
Neither script nor JSON output is committed. **166 comparisons, 0 mismatches.**

Coverage: all seven budget rows (N/T/used today/left today/B/deviation); all 28 bar positions;
displayed shares of T; all W/Wr/E weights; Copilot's 23/24 first day and UTC month subtraction;
October DST day; both five-hour readings/countdowns; exhausted weekly countdown; ready
sessions; native remainders/provider facts; 42-minute staleness; no-budget/zero-entitlement
cases; all four account states and their binding limits. Invented provider balance and ready
C are inputs, not observations of an account.

| Limits | W | Wr | E | N | B | Deviation |
| --- | --- | --- | --- | --- | --- | --- |
| A2 | 5 | 27/8 | 21/8 | 496/27 | 20 | 11/2 |
| A3 | 22 | 13 | 10 | 7 | 150/11 | -898/11 |
| B2 | 5 | 115/48 | 173/48 | 192/115 | 20 | -335/12 |
| B3 | 22 | 13 | 10 | 10950/13 | 8500/11 | 13720/11 |
| C1 | 527/24 | 13 | 239/24 | 810/13 | 48000/527 | -159670/527 |
| C2 | 527/24 | 13 | 239/24 | 3 | 1200/527 | 5626/527 |
| D1 | 5 | 3 | 3 | 70/3 | 20 | 24 |

T equals N on this synthetic work day. Figures use each row's native unit. B2 left today is
-268/115, floored to -2.4 %; C1 pace is 23900/527 = 45.351... %, rounded to 45.4. B rounds down
by brief choice 4, not an additional research rule. A2's whole-session results are 4 and 1.

### Phase B review focus and official sources (Step 3)

| Focus | Result |
| --- | --- |
| 1 Tooltip-only information | PASS: 5.1 keeps left today/state/deviation visible; 5.4/PA-2 require other decision figures visible or one keyboard step away. Tooltips only repeat visible content. |
| 2 Figures without data | PASS on the data boundary: 3.12/PA-5 exclude UI-only pools; unknown is not zero/unlimited; not-ready sessions hidden. D2's known 19 %/81 % and reset remain facts despite its unknown period, with no budget. C3 has no bar/zero budget. F-13/F-14 concern known-data exceptions. |
| 3 Default-look drift | PASS: every forbidden component is named in DA-2(a)-(i), repeated by PA-7. Functional sign-out icons are not decorative icons. |
| 4 WinUI 3/MSIX | PASS as a document feasibility/constraint review with evidence below. No selected font/design or packaged UI is tested; runtime/packaging acceptance NOT_RUN. |
| 5 Density/accessibility | DA-3/DA-4/DA-6 and PA-8/PA-10 require four accounts/eleven limits, 100 %/150 %, contrast and non-colour signals. FAIL on default-size consistency, F-16. Actual fit, palette contrast and assistive-technology behaviour NOT_RUN. |

Official pages read without sign-in on 2026-09-29:

- **OFL 1.1:** [official text](https://openfontlicense.org/open-font-license-official-text/),
  permissions and conditions 1-5 permit bundling/embedding/redistribution with software.
  Copies retain copyright/licence; modified fonts respect reserved names and remain under
  OFL. Section 9's example is supported, conditional on actual files being licensed that way.
  It is not approval of an unnamed future font.
- **Apache 2.0:** [official text](https://www.apache.org/licenses/LICENSE-2.0), sections 2/4
  permit reproduction/distribution in source or object form, including application bundling.
  Licence and applicable notices must be retained, modified files identified, and applicable
  NOTICE attribution carried. Section 9's example is supported. Naming a licence does not
  waive its conditions; per-font verification remains due before import. Unlike T-13, this
  review fetched and read the complete page.
- **Target/native effects:** the Windows project declares WinUI, Windows 11 build 26100 and
  MSIX tooling. [XAML/composition interop](https://learn.microsoft.com/en-us/windows/apps/develop/composition/xaml-comp-interop),
  [composition visuals](https://learn.microsoft.com/en-us/windows/apps/develop/composition/composition-visual-tree),
  [brushes](https://learn.microsoft.com/en-us/windows/apps/develop/composition/composition-brushes)
  and [ThemeShadow](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.media.themeshadow?view=windows-app-sdk-1.6)
  support the named primitives. Solid backgrounds, no WebView/Mica/Acrylic and native
  equivalents for effects are brief constraints, not claims that Windows lacks those effects.
- **Dark-only:** D-182 is the authority. [RequestedTheme](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.frameworkelement.requestedtheme)
  alone does not enforce it in contrast themes; [HighContrastAdjustment](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.highcontrastadjustment?view=windows-app-sdk-1.8)
  controls automatic adjustments. The brief requires own tokens and the resulting appearance,
  not RequestedTheme alone. Actual contrast-theme behaviour remains an implementation check.
- **Typography/fonts:** [Windows typography](https://learn.microsoft.com/en-us/windows/apps/design/signature-experiences/typography)
  supports 12-pixel regular-text guidance and Segoe UI Variable as a system font; it also
  provides weight/language-specific guidance, so 12 is not proof of every font's legibility.
  [NumeralAlignment](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.documents.typography.numeralalignment?view=windows-app-sdk-1.8)
  supports tabular figures. Native .ttf/.otf files and not packaging Windows fonts are compatible
  choices, not an exhaustive list of Windows formats. Per-file glyph/tabular support and
  rendering remain NOT_RUN until fonts are selected.
- **Accessibility:** [AutomationProperties.Name](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.automation.automationproperties.name?view=windows-app-sdk-1.8)
  and [AnimationsEnabled](https://learn.microsoft.com/en-us/uwp/api/windows.ui.viewmanagement.uisettings.animationsenabled?view=winrt-28000)
  provide the named mechanisms. Keyboard reachability and instant changes with motion off
  are implementable requirements, not runtime results. [WCAG 2.2](https://www.w3.org/TR/WCAG22/)
  supports non-colour information, normal-text 4.5:1 and applicable non-text 3:1. The uniform
  brief thresholds are stricter where WCAG allows exceptions.
- **Scaling/sizes:** [effective-pixel guidance](https://learn.microsoft.com/en-in/windows/apps/design/layout/screen-sizes-and-breakpoints-for-responsive-design)
  supports the unit model. 1920/1.5 by 1080/1.5 is 1280 by 720; at 2 it is 960 by 540 before
  subtracting the taskbar. MainWindow scales its 760/600 defaults on load; TrayPopupWindow
  scales its 360 width. The 200 %/larger-text scroll fallback is a proposal, not existing
  behaviour verified here. F-16 concerns the allowed alternate default.

### Re-check of all 23 brief choices (Steps 1, 3 and 4)

PASS means a compatible design choice, not a new owner decision.

| Choice | Verdict and reasoning |
| --- | --- |
| 1 Antigravity staleness | PASS: whole-account refresh failure legitimately leaves both readings stale; the plan does not require a fresh second group. |
| 2 Group 2 not weekly | PASS: research 8.3 rule 3/D-185 forbid inferring a week from an unknown token. |
| 3 Pace/budget positions | PASS: B x E visualizes the accepted deviation reference; U0 to U0 + T visualizes today's allowance without changing formulas. |
| 4 Extra rounding | PASS: B/share flooring is explicit and conservative; rounded bar marks never feed calculations. All figures match. |
| 5 Healthy five-hour rank | PASS: OK completes the non-warning case without hiding higher-ranked limits. |
| 6 Stale emphasis | PASS: dependent current-use figures/state dim; fixed N/T remain, as research 8.8 requires. |
| 7 Stable order | PASS: compatible presentation choice; visible binding status preserves R-10 without row movement. |
| 8 Extra catalogue rows | PASS: the eight additions carry research/history/sign-in requirements, without a new provider source or budget kind. |
| 9 Claude counter/tracked equality | PASS: valid synthetic case if the counter begins at zero at month start with continuous tracking; not a provider guarantee. |
| 10 Exhausted budget figures | PASS on model: research 8.8 changes displayed state, not arithmetic eligibility. Marks/disclosure acceptance needs F-15. |
| 11 Amber/red state colours | PASS: compatible semantics for research 8.8 ranks. |
| 12 Solid background | PASS: explicit constraint for predictable contrast, compatible with B-4. |
| 13 Own tokens | PASS: compatible with D-182; implementation still must control contrast-theme adjustments. |
| 14 Minimum text size | PASS: matches cited regular-text guidance; actual font/script readability still needs checking. |
| 15 Contrast arithmetic | PASS: truncation cannot turn a sub-threshold ratio into a pass; stale text retains its minimum. |
| 16 Stale bar parts | PASS: U0/T and baseline end-of-day position are fixed; current used fill/deviation/state carry staleness. |
| 17 Exhausted marks optional | FAIL on acceptance consistency, F-15; no arithmetic defect. |
| 18 Session graphics | PASS: inherit readiness/estimate requirements; shared zero-boundary ambiguity is F-14. |
| 19 Neutral colours | PASS: keeps neutral distinct from OK, preserving all six required semantic colours. |
| 20 Scaling/scroll | Compatible fallback, but FAIL on fixed-size wording after allowing another default, F-16. Actual fit not claimed. |
| 21 Font files/fallback | PASS as constraints: native files, OS fonts not redistributed, script coverage disclosed, fallback allowed. Specific fonts unselected. |
| 22 Extra rubric rows | PASS on scope: check existing truthfulness/identity/accessibility/surfaces/specification requirements. Wider PA-5 serves focus 2. |
| 23 Accessible-name example | PASS: illustrative wording carries account/limit/state/return time without fixing product copy. |

### Open findings (Step 4)

All four are OPEN. They require semantic/rubric decisions, so T-14 did not edit the design.
No new owner decision was created; T-15 owns resolution or escalation from PD-034-04.

#### F-13 - Unlimited with a personal cap also matches the no-budget rule

- **Location:** design-brief.md 3.4 `unlimited`, 3.5 `no budget, with reason`, 3.8; PA-1/PA-5.
- **Problem:** the unlimited row requires no budget while also saying a set cap becomes L.
  The generic no-budget row includes unlimited without that exception. A designer can suppress
  the cap budget that R-03 requires.
- **Evidence:** research 5.4 makes a known personal cap L when the provider is unlimited,
  irrespective of a reported amount; 8.1 permits its budget with known period/use. Synthetic
  unlimited provider, cap 100 requests, known month and U = 20 is budgetable but matches both
  brief rows.
- **Suggested resolution:** qualify no-budget/unlimited as no comparable personal cap; keep
  provider unlimited as a fact while showing an eligible cap budget and its binding source.
  Include this combination in prototype coverage.

#### F-14 - Less-than-one session forbids the exhausted zero exception

- **Location:** design-brief.md 3.7 `sessions ready`/`less than one session`, 6.2/6.4; PA-1.
- **Problem:** sessions ready requires 0 for exhausted weekly quota, but less-than-one applies
  to any figure below 1 and forbids 0. Both match an exhausted window with a ready estimate.
  B2 is not-ready, so the scenario does not exercise this conflict.
- **Evidence:** research 7.5 preserves exhausted zero. Weekly U = 100 and ready C = 12 gives
  weekly remainder/C = 0, conflicting with the other catalogue row's prohibition.
- **Suggested resolution:** state exhausted-zero precedence for the weekly figure and scope
  less-than-one accordingly; retain readiness/estimate rules for text and graphics. Derive
  today's figure from T/C separately from the weekly exhausted-zero exception.

#### F-15 - Optional exhausted budget marks become mandatory in PA-3

- **Location:** design-brief.md 4.2 B2, 4.3 B2, 6.4 B2, PA-3; choices 10/17.
- **Problem:** 6.4 permits omission of exhausted budget marks, but PA-3 requires every 4.3
  position to be shown. A prototype following the optional treatment can fail literal
  acceptance.
- **Evidence:** B2 has four 4.3 positions, including U0 + T = 97.7 and pace = 72.1. Section 6.4
  permits subordinate budget marks; PA-3 makes no omission exception. Arithmetic passes.
- **Suggested resolution:** choose mandatory or optional exhausted marks and align PA-3;
  if optional, check positions when drawn and specify access to kept-available budget figures.

#### F-16 - Alternate default size conflicts with the fixed platform requirement

- **Location:** design-brief.md 5.4, 7.1 item 5, DA-6, PA-10, section 9 Display scaling;
  choice 20.
- **Problem:** another default size with a reason can pass DA-6, but section 9 still requires
  the scenario to fit 760 by 600. It does not clarify the 150 % example for that alternative.
- **Evidence:** T-12 Step 5 permits another stated default; 5.4/7.1/DA-6 retain that option.
  Section 9 fixes 760 by 600 again, while PA-10 says only the default. An 800 by 640 proposal,
  for example, has two possible acceptance targets.
- **Suggested resolution:** use one owner-selected default consistently, with 760 by 600 as
  the reference starting size; specify the work-area/scroll fallback and 150 % density test
  when the size changes.

### Publication checks and limits (Steps 5 and 6)

PASS: the scratch privacy scan examined all of design-brief.md, tasks.md and verification.md
for e-mail addresses, bearer/JWT/API tokens, secret assignments, UUIDs, local user paths and
owner identity strings: 0 hits. Manual review of added text found only synthetic quota
examples and public-source/document references. All 15 relative document links resolve.

PASS: `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json`
returned `{"valid":true,"diagnostics":[]}` (exit 0); `git diff --check` was clean (exit 0).
These checks are repeated on the final task/handoff record before committing because those
record edits change the checked tree. Integrated diff review is limited to this record and
tasks.md; design-brief.md remains byte-identical to the review base.

Source/web review was read-only.
No live checks, sign-in, provider requests, CLI credential access or Claude Design contact.
No screenshots, real-account data or scratch artifacts enter this commit. AC-08/Gate B remain
NOT_RUN; T-15 must resolve the four findings before preparing Gate B. Whole-item AC-10 remains
NOT_RUN until the later Phase B work is complete.

## T-13 design brief part 2 - 2026-09-29

Primary [opus] session. Base `edeefc3`; the initial `git pull` reported `main` up to date and
the tree was clean.

**Authorization.** In the session conversation the owner stated that they approved the Phase B
plan on 2026-09-29 and authorized T-13 as written, including the commit and push to `main`.

**Scope.** `design-brief.md` sections 6 to 10, this record, and the T-13 status and handoff in
tasks.md. Sections 1 to 5, the section 3 state names and the section 4 scenario are unchanged.
Documents only: no product code, stored format, transport, provider request, sign-in, live
check or Claude Design contact. Read-only web access confirmed the cited pages only: the SIL
Open Font License official-text page (title and version 1.1) and Microsoft Learn
(`Typography.NumeralAlignment` in `Microsoft.UI.Xaml.Documents`; the Windows 11 type ramp and
its 12-pixel minimum for regular text). The Apache License page could not be fetched from this
session. The licence statements themselves are verified by T-14.

### Clause mapping (Step 6)

Each B-4, B-5 and B-6 clause of spec.md, with the brief sentences that carry it. The quoted
spec text is verbatim from spec.md; the quoted brief text is verbatim from design-brief.md.

| Clause | Spec text | Brief sentences |
| --- | --- | --- |
| B-4 forbidden 1 | "a near-black slate or zinc background with a single indigo, violet or blue accent" | Section 6 list, verbatim; 8.1 DA-2 "(a) a near-black slate or zinc background" |
| B-4 forbidden 2 | "purple-to-blue gradients and glassmorphism" | Section 6 list, verbatim; DA-2 "(b) a purple-to-blue gradient" and "(c) glassmorphism"; 9 "without Mica or Acrylic" |
| B-4 forbidden 3 | "a grid of identical rounded cards with soft shadows" | Section 6 list, verbatim; DA-2 "(d) a grid of identical rounded cards" |
| B-4 forbidden 4 | "Inter or a system font as the only typographic idea" | Section 6 list, verbatim; 6.3 "may be one half of the pairing, never the only typographic idea"; DA-2 "(e) Inter or a system font" |
| B-4 forbidden 5 | "KPI tiles, pill badges everywhere, and decorative emoji or icons" | Section 6 list, verbatim; DA-2 "(f) KPI tiles", "(g) pill badges everywhere" and "(h) decorative emoji or icons" |
| R-08 | "does not reuse Claude Design's default theme and colors" | Section 6 "does not reuse Claude Design's default theme and colours"; DA-2 "(i) Claude Design's default theme and colours" |
| B-4 required 1 | "a named design concept with its rationale" | 6.1 "Each direction has a name and a concept"; 7.1 "its name, concept and rationale"; DA-1 |
| B-4 required 2 | "a palette derived from that concept, with semantic state colors (ok, attention, critical, stale, estimate, assumed) meeting WCAG AA contrast on the dark background" | 6.2 "the six semantic state colours, each tied to the section 3 states it marks" and "at least 4.5:1 against each"; 7.1 "its palette tokens and contrast table"; DA-3; PA-8 |
| B-4 required 3 | "a distinctive type pairing with tabular numerals" | 6.3 "Every figure uses tabular numerals"; 9 "Typography.NumeralAlignment"; DA-4 |
| B-4 required 4 | "a signature limit visualization that combines the bar, today's budget, the pace mark and five-hour sessions" | 6.4 "Each direction delivers one visualization used on every limit row" and "The visualization is shown for every limit kind of the section 4 scenario"; DA-5 |
| B-4 kept 1 | "dark-only (D-182)" | 6.2 "all dark (D-182) and solid"; 9 "One dark appearance in the main window" |
| B-4 kept 2 | "status never relies on color alone" | 9 "Every coloured state also has a word"; PA-4 "the colour never stands alone"; PA-8 |
| B-4 kept 3 | "everything implementable in WinUI 3 on Windows 11" | 9 "A WinUI 3 desktop app on Windows 11, packaged as MSIX" and "Only what WinUI 3 XAML and the Windows composition layer render"; DA-7; PA-12 |
| B-4 kept 4 | "fonts licensed for embedding in an MSIX package" | 9 "under a licence that permits embedding and redistribution in an"; 6.3 "the direction names its licence"; DA-4; PA-12 |
| B-5 directions | "Two or three distinct directions, of which the owner selects one." | 7.1 "Claude Design first delivers two or three distinct directions, and the owner selects one." |
| B-5 prototype | "a full prototype of the main window in every state" | 7.2 "a state list that names every"; PA-1 |
| B-5 tray flyout | "including the tray flyout" | 7.2 "The secondary flyout, currently 360 effective pixels wide"; PA-11 |
| B-5 settings panel | "the settings panel" | 7.2 "The inline panel with the work days"; PA-11 |
| B-5 inline editing and confirmation | "inline editing and confirmation" | 7.2 "Inline editing, confirmation and undo."; PA-11 |
| B-5 first run | "first run and the sign-in strip" | 7.2 "The providers listed directly, each with one-click sign-in."; PA-11 |
| B-5 sign-in strip | "first run and the sign-in strip" | 7.2 "Sign-in in progress and its success"; PA-11 |
| B-5 specifications | "Token and component specifications." | 7.2 "Token specification." and "Component specification."; PA-13 |
| B-5 notes | "Keyboard, accessible-name and reduced-motion notes." | 7.2 "Keyboard, accessible-name and reduced-motion notes."; 9 "Every action is reachable and operable by keyboard"; PA-9 |
| B-6 direction | "The criteria the owner uses to accept or reject a direction" | 8 "section 8.1 to accept or reject a direction"; DA-1 to DA-7 |
| B-6 prototype | "and the final prototype" | 8 "section 8.2 to accept or reject the final prototype"; PA-1 to PA-13 |

### Review focus to rubric rows

| Phase B review focus | Rubric rows |
| --- | --- |
| 1 Tooltip-only information | PA-2; DA-6 (left today, budget state and deviation visible on each row) |
| 2 Figures without data | DA-5 (the "Never draws" column of section 6.4); PA-5 |
| 3 Default-look drift | DA-2 (a) to (i), each forbidden item by name; PA-7 |
| 4 Not buildable in WinUI 3 or MSIX | DA-4 (font licence); DA-7; PA-12 |
| 5 Density versus accessibility | DA-3; DA-6 (100 %); PA-8; PA-10 (100 % and 150 %) |

### Brief choices for T-14, continued

T-12 listed choices 1 to 10. Sections 6 to 10 add these; T-14 checks them and T-15 raises any
that needs the owner.

11. **R-09 colours are state colours.** Attention is the amber and critical the red of R-09,
    so a five-hour window and a budget in the same rank share one colour.
12. **Solid background.** No Mica or Acrylic, so every contrast ratio holds against a known
    colour. B-4 forbids glassmorphism; excluding Mica is a brief choice.
13. **Own tokens only.** No Windows accent colour or system theme brushes, so that the dark
    appearance holds in light app mode and in contrast themes (D-182).
14. **Minimum text size.** 12 effective pixels, the Windows 11 minimum for regular text.
15. **Contrast arithmetic.** WCAG 2.2 relative luminance, truncated to two decimals; the dimmed
    stale level must meet AA like any text.
16. **Stale bar parts.** On D1 the used fill is dimmed, while today's budget and the pace mark
    stay at full emphasis because `U0`, `T` and `B x E` are fixed for the day, as section 3.2
    keeps `N` and `T`.
17. **Exhausted weekly budget marks.** B2's section 4.3 marks may be drawn, subordinate to the
    exhausted state (choice 10).
18. **Graphic session marks** follow the states of the session figure.
19. **Neutral is not a semantic colour,** and neutral states never use ok.
20. **Scaling behaviour.** The fit applies at 100 % and 150 % display scaling and at 100 % text
    size. At 200 % on a 1920 × 1080 display, or at a larger text size, the content scrolls.
21. **Font files.** .ttf or .otf, not WOFF or WOFF2; a system font is used from Windows and not
    packaged; each direction states the scripts its fonts cover, because account names are
    user text.
22. **Rubric rows beyond the plan list:** DA-7, PA-6, PA-7, PA-8, PA-11 and PA-13. PA-5 widens
    the plan's "no figures for UI-only pools" to all of review focus 2.
23. **Accessible-name example.** "Codex Pro weekly, exhausted, back Friday 09:30" is
    illustrative wording, not fixed copy.

### Checks (Step 6)

- **Clauses and rubric:** `check_t13.py`, a scratch script that is not committed, parses this
  record and the brief. Sections 1 to 5 are byte-identical to `edeefc3`, and the ten top-level
  headings are in order. All 13 B-4 bullets appear verbatim in section 6. The 25 mapping rows
  cover every expected B-4, B-5, B-6 and R-08 clause: 25 of 25 quoted spec texts are found in
  spec.md and 42 of 42 quoted brief texts in sections 6 to 10, and every cited rubric row
  exists. Review focus 1 to 5 each cite existing rows. The rubric has DA-1 to DA-7 and PA-1 to
  PA-13 without gaps, and DA-2 names all nine forbidden items. The 51 state names used in the
  section 6.2 and 6.4 tables are section 3 identifiers or the section 4.1 kind "percentage
  window". The 17 scenario figures quoted in section 6.4 are found in section 4, and the 4
  relative links resolve. Result: 0 failures. A mutation run of the same script detected 8 of 8
  injected defects: a changed forbidden bullet, a renamed rubric row, a changed figure, a
  changed state name, a changed section 5 heading, a missing clause row, a changed brief quote
  and a review focus without a row.
- **Privacy:** `scan_privacy_t13.py`, a scratch script that is not committed, scanned the whole
  of design-brief.md, verification.md and tasks.md for e-mail addresses, bearer, JWT, `sk-`,
  GitHub, AWS and Slack tokens, secret assignments, UUIDs, local user paths and owner identity
  strings: 0 hits. The amounts on lines added by T-13 are the spec's generic examples (USD
  300.00, USD 500.00, 17,000 credits) or synthetic section 4 values (USD 81.63, 1,247, 10,160).
- **Documents:** on the final tree, `dotnet run --project tools/AiUsage.ProjectValidation
  --no-restore -- --root . --json` returned `{"valid":true,"diagnostics":[]}`, and
  `git diff --check` reported no errors.

AC-08 stays NOT_RUN: the brief is complete for review, but T-14 has not reviewed it and Gate B
has not been held. AC-10 stays NOT_RUN for the whole item; this task's own scan and document
checks pass.

## T-12 design brief part 1 - 2026-09-29

Primary [opus] session. Base `3d544cd`; the initial `git pull` reported `main` up to date and
the tree was clean.

**Phase B plan approval.** The owner was asked in the session conversation whether the Phase B
plan (T-12 to T-18) is approved as written. They replied with the instruction to execute T-12,
and after a tool-permission interruption confirmed "approved, create the brief" (translated
from Ukrainian). Phase B started on that instruction; this is the separate owner instruction that
spec.md's Gate A section requires.

**Scope.** `design-brief.md` sections 1 to 5, with sections 6 to 10 as headings and one-line
content statements for T-13. Documents only: no product code, stored format, transport,
provider request, sign-in, live check or Claude Design contact.

### Brief choices for T-14

The research and the plan leave these points open or state them differently. Each is a brief
choice, not a new decision; T-14 checks them and T-15 raises any that needs the owner.

1. **Antigravity staleness.** The plan names one stale group and one "period unknown" group.
   The model has staleness per reading, not per limit, so the scenario makes the whole
   Antigravity refresh fail: both groups are stale, group 1 keeps a dimmed budget and group 2
   has "period unknown".
2. **Group 2 is not called weekly.** Its window token is unrecognized, so its duration is
   unknown (research 8.3 rule 3). Calling it weekly would imply a duration.
3. **Pace mark.** Research does not place the pace mark. The brief puts it at `B x E`, the
   baseline position at the end of today that the deviation is measured to, and today's budget
   on the bar from `U0` to `U0 + T`.
4. **Rounding.** Research 8.9 does not give a direction for `B`; the brief rounds it down like
   `N`. The share of `T` left, shown for review, rounds down. Bar marks round to the nearest 0.1.
5. **Five-hour window above 30 % remaining ranks as OK.** Research 8.8 ranks only amber and red.
6. **Stale emphasis.** The reading and every figure computed from it are dimmed; `N` and `T`
   stay at full emphasis because they are fixed for the day (research 8.8 with R-15).
7. **Stable limit order.** Rows do not reorder when a state changes; the account status carries
   the binding limit. This is a brief requirement not stated in the spec.
8. **Catalogue rows beyond the plan list:** other percentage window of a day or longer,
   secondary amount of unknown unit, gap in history, assumed start, pace mark, tracked
   estimate (with the provider's own limit state, research 8.8), no today session figure, and
   sign-in in progress.
9. **Claude extra usage.** The provider counter equals the tracked amount, USD 218.00, because
   the app tracked the counter from the start of the month without a gap. The catalogue row
   still requires both to be shown when they differ.
10. **Exhausted weekly budget figures.** For Codex weekly (B2) the budget figures are computed
    and kept available, but the displayed state is "at limit" (research 8.8).

### Checks (Step 6)

- **Figures:** `check_brief.py`, a new scratch script that is not committed, recomputes the
  section 4 scenario from its inputs with exact fractions and the 2026 Europe/London rule, then
  parses the brief's section 4 tables. The first run found 4 mismatches: the C1 pace mark
  (45.3, expected 45.4) and three shares of `T` rounded to nearest instead of down. After the
  fixes: 68 comparisons, 0 mismatches.
- **States:** `check_coverage.py` checks 62 states named in research 5.4, 6.4, 6.5, 7.5, 8.1,
  8.5 and 8.8, spec B-2 and the T-12 Step 3 list against the 61 catalogue rows, plus the never
  list and the account ranking: 0 missing.
- **Privacy:** `scan_privacy.py`, a scratch script that is not committed, scanned the whole of
  design-brief.md, tasks.md, verification.md and docs/backlog.md. E-mail, bearer, JWT, `sk-`,
  GitHub, AWS and Slack tokens, secret assignments, UUIDs, owner identity strings and local
  user paths: 0. Currency and credit amounts are the spec's generic examples (USD 500, USD 300,
  17,000 credits) or the synthetic section 4 values. No account names, balances, captures or
  payloads.
- **Documents:** on the final tree, `dotnet run --project tools/AiUsage.ProjectValidation
  --no-restore -- --root . --json` returned `{"valid":true,"diagnostics":[]}` after the T-12
  evidence field was corrected to the file path (the first run reported
  `DONE_WITHOUT_EVIDENCE`), and `git diff --check` reported no errors.

AC-08 stays NOT_RUN: the brief is incomplete until T-13, and Gate B has not been held. AC-10
stays NOT_RUN for the whole item; this task's own scan and document checks pass.

## Gate A owner review - 2026-09-29

Base `b53e1d0`. The owner reviewed the Phase A outputs in the session conversation, answering
one question at a time with a suggested answer each, and accepted Phase A. Decisions, recorded
as D-185 and in spec.md:

- PD-034-01: option (b), local tracking of balance decreases for balance-only pools.
- PD-034-03: option (a), UI-only limits are not shown until a current quota source exists.
- R-06 reading accepted: a known provider reset without a known duration gives no budget.
- R-11 reading accepted: a day off makes only the daily budget neutral.
- R-05 reading accepted: a zero limit is shown as a state, not as a zero budget.
- PD-034-02 stays pending until an AIU-005 implementation is selected.

Gate A: PASS. AC-07: PASS, recorded before any Phase B work. Phase B has not started and
starts only on a separate owner instruction. The owner also chose to replace the local clone
path in `docs/providers/codex.md` line 53 that the T-11 scan reported; that is a separate
commit, and published history is not rewritten. Checks for this record: validator `--json`
`valid:true` with no diagnostics and `git diff --check` clean before the commit.

## T-11 findings resolution and Gate A package - 2026-09-29

Primary [opus] session. Base `9a96024`; the initial `git pull` reported `main` up to date, and
the tree was clean. T-11 was set to in-progress before work. Scope: resolve F-01 to F-12,
complete research section 11, check AC-01 to AC-06, scan the Phase A files and hand off to the
owner's Gate A review. No product code, stored format, transport, provider request, sign-in,
browser, live check or credential access. This is a primary review, not an independent review.

### Findings (Step 1)

All twelve findings are resolved by changes to the research design; none became a new
PD-034 decision. Each resolution is recorded under its finding in the T-10 section below.

| Finding | Resolution | Research sections | Cases |
| --- | --- | --- | --- |
| F-01 | Runs end at every gap, period instance, plan or source change; explicit day-start rules; samples read from covering runs | 6.2, 6.4, 7.3, 7.6 | P01-P04, P11, S05, S07 |
| F-02 | Series stores signed balances; tracked consumption defines baseline, top-ups and reset policy | 5.5, 6.2, 6.5, 11 | E02 |
| F-03 | Readiness needs a median above 0 and 5 weekly points across the samples | 7.2 | S01, S03 |
| F-04 | One period-instance rule for `U0` and the estimator; `U0 = 0` when the restart is known to be today | 6.3, 6.4, 8.6 | P05-P09, E06b |
| F-05 | Under an assumed period, `U` is tracked from the series; the provider counter stays a fact | 6.5, 8.3, 8.6, 8.8 | E12a, E12b |
| F-06 | Today's sessions are `T / C` in both sections | 7.5 | S08, S09 |
| F-07 | R-06 applied per supplied fact; an unknown duration gives "period unknown" | 8.3, M-05 | E13 |
| F-08 | Copilot request pools get an assumed start one month before the provider reset | M-04, 5.2, 5.5, 8.3 | E08a, E08b |
| F-09 | CX-I budgets only its percentages; raw strings are a secondary amount of unknown unit | M-07, 5.2, 5.3, 5.5 | none |
| F-10 | Limit state (a fact on every day) separated from budget state (neutral on a day off) | 8.5, 8.8 | E10a, E10b |
| F-11 | Unlimited flag precedence; zero entitlement and cap 0 presented as states; R-05 arithmetic unchanged | M-01, 5.2, 5.4, 8.1, 8.8 | E11a-E11d |
| F-12 | PD-034-03 depends only on a current quota source; LC-11 and LC-17 cannot supply data | 10, 11 | none |

The Step 3 coverage check also found a gap in AC-01: CL-C had no cells for Max, Team and
Enterprise. Section 3 now records them as unknown/none; the family was observed only on the
Pro UI. Section 6 was reorganized (series 6.2, period instances 6.3, day-start amount 6.4,
tracked consumption 6.5, retention 6.6), cross-references in sections 5.1, 5.6, 5.7 and 6.1
follow the new numbering, and section 12 gained project references.

### Checks written before the changes

Both scripts are scratch files in the session's temporary directory and are not committed.

- **AC-01 matrix coverage** (`ac01_coverage.py`, SHA-256
  `371AAECF02D4B25D1A19BA6082CC8AD6B9924C62DDE41F6DB001F0CDD593EB06`): parses every section 3
  field table and requires a class/level cell for every family, plan and A-2 field. Before the
  change it failed as expected: 679 cells, 0 malformed, 21 missing (CL-C for Max, Team and
  Enterprise). After: 700 of 700 (25 families, 4 plans each, 7 fields), 0 malformed. PASS.
- **Section 9 recomputation** (`recompute_t11.py`, SHA-256
  `503732B3C73130D7DC2921FFE48CD001A248EFDBD10CD97BD067831380477539`), newly written and
  independent of the T-09 and T-10 scripts: recomputes every 9.1 row from its inputs with the
  section 8 rules, explicit Europe/London 2026 transition instants and exact decimals; compares
  `W`, `Wr`, `N`, `B`, deviation, used and left today, the state and percentage notes, the
  note-level figures and the S01 to S04 readiness results. Before the change it failed as
  expected: the 15 existing rows reproduced exactly and 13 new rows were missing. After: 213
  comparisons, 0 mismatches. PASS.

### Requirement re-check

Each clause of R-03 to R-07 and R-11 was re-read against the changed research.

| Requirement | Result |
| --- | --- |
| R-03 | PASS. Local cap on every represented countable or monetary pool with a known unit, including pools reported as unlimited or without a limit; the lower of cap and provider limit; cap 0 allowed; never provider data; no cap on percentage windows. A currency mismatch leaves the cap unapplied because R-02 forbids conversion. UI-only pools depend on PD-034-03. |
| R-04 | PASS. One global weekday set, Monday to Friday by default (8.2). |
| R-05 | PASS. `U0`, `U`, `W`, `Wr`, `N`, `B`, deviation and used and left today match. `N` is fixed for the day and recomputed only at midnight, for a new period instance and after a cap change; jitter and corrections do not recompute it (6.3, 8.6). Partial days are defined (8.3). Under an assumed period `U` is the use in that period (6.5). A zero limit presents R-05's result (8.1). |
| R-06 | PASS, with the per-family confirmation R-06 asks Phase A for (8.3): supplied resets are always kept, the calendar-month fallback applies where no reset is supplied, and an unknown duration gives "period unknown". Listed for the owner at Gate A. |
| R-07 | PASS. `C` from local paired readings; weekly remainder and today as `T / C`; labelled estimates; hidden until ready; a zero `C` can never be ready. |
| R-11 | PASS. Work days apply only to windows of a day or longer; the day-off budget state is neutral; day-off use lowers the next norm; holidays deferred. Exhausted and over-cap limits keep their limit state (R-10). |
| D-184 | PASS. `U0`, tracked consumption, the estimator and history derive only from the local series; no provider history remains a dependency (F-12). |

Review focus re-check: 1 PASS, absent, zero and unlimited stay distinct, now including an
unlimited flag with an amount; 2 PASS, the money triple is unchanged; 3 PASS, reset identity and
assumed periods are defined (F-04, F-05); 4 PASS, no plan generalization and the Copilot start
is no longer overstated (F-08); 5 PASS, `N` is never negative and zero limits are defined
(F-11); 6 PASS, straddling spans are rejected by period instance and a zero `C` is not ready
(F-01, F-03, F-04).

### Acceptance criteria (Step 3)

| AC | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | Section 3 has a matrix for every provider and plan in scope, plus Google AI Plus, with class and evidence level for every field; coverage script 700 of 700, 0 malformed. |
| AC-02 | PASS | T-06 verdict re-checked: every A-3 gap keeps its evidence or an explicit unknown with a closing live check. After D-184, LC-11 and LC-17 cannot supply data, but the Codex allotment and Copilot credit gaps keep their other closing checks (LC-08 to LC-10, LC-12 to LC-16). Unrun checks stay NOT_RUN. |
| AC-03 | PASS | Section 5 covers kind, unit, used, limit, remaining, period start and end, reset source, personal cap and snapshot source; 25 of 25 families are mapped in 5.5 (recounted); stored-format impact is in 5.7. |
| AC-04 | PASS | Section 8 was re-checked against R-03 to R-07 and R-11 (table above) without contradiction; section 9.1 has every A-7 case (E01 to E09) plus E10 to E13, recomputed with 0 mismatches. |
| AC-05 | PASS | Section 6.1 decides the `U0` source per provider, the local series; the series, period instances, day-start rules and tracked consumption are in 6.2 to 6.5; retention and the security-lifecycle precondition are in 6.6. |
| AC-06 | PASS | Section 7: inputs (7.1, 7.6), formula and aggregation (7.2), minimum samples (7.2), invalidation (7.3) and label (7.5); cases S01 to S09. |
| AC-07 to AC-10 | NOT_RUN | Gate A, Phase B, the follow-up proposals and the whole-item AC-10 check are outside T-11. |

These verdicts say that research.md meets the Phase A documentation criteria. They do not
approve the design; that is the owner's Gate A review. The security-lifecycle review of the
series and the budget configuration remains an implementation precondition, NOT_RUN.

### Privacy scan and publication checks (Step 4)

- **Scope:** all 18 files changed by the 19 AIU-034 commits from the first Phase A commit
  `6ee7f42` to `HEAD`, plus the T-11 working tree. The only other commit in that range,
  `e5013fa`, is a product fix outside Phase A. Whole files were scanned, not only added lines,
  by `scan_phase_a.py` (SHA-256 `530DC0644715879037B1CB64DDEC6D2DFA9F370F315B69518E63F924E9D8E30E`).
- **Patterns and results:** e-mail, bearer, JWT, `sk-`, GitHub, AWS and Slack tokens and
  secret assignments: 0. Currency amounts, 10 lines, and credit amounts, 5 lines: all are the
  spec's generic examples (USD 500, USD 300, 17,000 credits) or synthetic worked examples. UUID,
  1: `docs/providers/claude.md` line 41, the public upstream OAuth client ID recorded in
  AIU-007, not a credential. Owner identity strings, 1: `docs/providers/codex.md` line 53, a
  local clone path that contains the Windows user name. It dates from 2026-09-14 (AIU-003), was
  not added in Phase A, and is reported to the owner, not changed here.
- **Manual review of the T-11 changes:** synthetic values only; no account or organization
  names, balances, captures, payloads or credentials.
- **Document checks on the final tree before the commit:** `dotnet run --project
  tools/AiUsage.ProjectValidation --no-restore -- --root . --json` returned
  `{"valid":true,"diagnostics":[]}`, and `git diff --check` reported no errors.

Gate A: NOT_RUN. The Phase A outputs are complete and await the owner's review. Phase B has not
started.

## T-10 independent detail review - 2026-09-29

Reviewer: primary [astra] session, independent of the T-07 to T-09 author; no subagent.
Frozen review base: `4c4318fe65b48f38911b395a3172fbdf0788491f`. Initial `git pull` on
`main` reported already up to date; the working tree was clean. T-10 was set to in-progress
before review. Scope: research sections 3, 5 to 9 and 11, the relevant section 4 notes,
spec R-03 to R-07 and R-11, both 2026-09-29 owner amendments/clarifications, and D-184.
This is a document review, not new provider evidence or approval of the proposed design.

### Independent recomputation (Step 1)

PASS: a newly authored PowerShell script in the session temporary directory recomputed
all 15 E01 to E09 rows, including every sub-case. It used only transcribed row inputs
(including referenced dates, the E07b weekday override and E03's C), section 8 formulas,
and Windows `GMT Standard Time` rules for Europe/London. It enumerated local dates,
intersected each day with the period in UTC ticks, computed decimal day weights, W, Wr,
E, N, T, B, deviation, used/left today and state, then applied section 9's table-only
half-up rounding. Expected cells were read only after computation. No previous author's
scratch script was read or reused. The script is outside the repository and is not committed.

Script: `recompute.ps1`; SHA-256
`CABE1163A457E9A5F9731E97F965B3BB476F406B99F1FCC7AD11365B026B53B2`.
Execution exited 0: **139 comparisons, zero mismatches**. These comprise seven numerical
cells and the state for every row (120), plus 19 checks of numerical notes: day weights,
session estimates, percentage-left notes, today's shares, the E04b counterfactual and E09
remainder. Cap binding was also checked against each row's provider limit/cap inputs.
Decimal intermediates were retained to runtime precision until final rounding; no rounded
W/Wr cell was used as an input. This verifies the published two-decimal results, not an
implementation's future exact-rational arithmetic or display rounding.

| Case | W / Wr | N / B | Deviation | Used / left today | State |
| --- | --- | --- | --- | --- | --- |
| E01 | 22 / 2 | 4500 / 1363.64 | +6836.36 | 800 / 3700 | OK |
| E02 | 22 / 13 | 753.85 / 772.73 | +77.27 | 450 / 303.85 | OK |
| E03 | 5 / 2.63 | 23.62 / 20 | +20.50 | 9 / 14.62 | OK |
| E04a | 5 / 3.63 | none / 20 | -3.50 | 6 / none | Neutral |
| E04b | 5 / 3.63 | 19.03 / 20 | +16.50 | 0 / 19.03 | OK |
| E05a | 22 / 2 | 2000 / 1136.36 | +2063.64 | 800 / 1200 | OK |
| E05b | 22 / 2 | 250 / 977.27 | -1277.27 | 800 / -550 | Over cap by 300 |
| E05c | 22 / 2 | 0 / 909.09 | -2709.09 | 800 / -800 | Over cap by 1800 |
| E06a | 5 / 0.63 | 19.20 / 20 | +5.00 | 7 / 5.00 | OK |
| E06b | 5 / 5 | 20 / 20 | +3.50 | 4 / 3.50 | OK |
| E07a | 4.96 / 2.42 | 19.86 / 20.17 | +11.43 | 8 / 11.86 | OK |
| E07b | 6.98 / 6.98 | 14.33 / 14.33 | +3.88 | 3 / 3.88 | OK |
| E08a | 20 / 20 | 15 / 15 | +3.00 | 12 / 3.00 | Attention |
| E08b | 22.96 / 22.96 | 13.07 / 13.07 | +7.52 | 5 / 7.52 | OK |
| E09 | 5 / 0 | none / 20 | +28.00 | 2 / none | Neutral |

The script interprets the explicit neutral rule in section 8.5 before budget-only states;
E09's deviation and used-today cells are calculated audit values, while the prescribed UI
shows only the remainder. Passing arithmetic does not resolve the architectural findings.

### Coverage, vocabulary and requirements (Steps 2 and 3)

PASS, family coverage: independent set comparison found 25 section 3 family IDs and 25
mapped IDs in section 5.5, zero missing: Claude 8, Codex 7, Copilot 5, Antigravity 5.
UI-only families explicitly marked not represented count as mappings, not implemented
support or owner approval of PD-034-03. No missing section 5.5 row required correction.

Mechanical correction only: section 6.1's Codex budgetable-family list omitted CX-P even
though section 5.5 explicitly covers primary windows whose returned duration is at least
one day. Added CX-P to that list; no rule, eligibility threshold or provider claim changed.
No arithmetic or cross-reference correction was needed. All other findings below are open;
research's design is unchanged.

Vocabulary review covered every section 5.2 field, the money triple, cap/effective-limit
separation and source metadata through sections 6 to 9. Native units, explicit unknown/null/
unlimited states, opaque keys, independent flags, money scaling and cap provenance are
preserved in the ordinary cases. Consistency is FAIL for the specific series, period and
mixed-unit cases F-01, F-02, F-04, F-05, F-07 to F-09; this is not blanket model acceptance.

| Review focus | Verdict and evidence |
| --- | --- |
| 1. Absent versus zero/unlimited | PASS for absence semantics: sections 5.1, 5.2 and 5.4 keep all three distinct; no E-row fills unknown with zero. The separate known-zero budget exception is F-11. |
| 2. Minor units and exponents | PASS: section 5.2 retains signed minor units, exponent and currency, exact upward rescaling and overflow-to-unknown; E01/E05 use minor USD with exponent 2. No major-unit float or cross-currency conversion is introduced. |
| 3. Local day versus reset and DST | PASS for explicit weights and E06/E07 arithmetic (including the 25-hour day); FAIL for reset identity and assumed-period rollover, F-04/F-05. Full 23-hour days also weigh 1 by section 8.3; no new live check is claimed. |
| 4. One account versus all plans | PASS for evidence provenance: section 3 keeps Claude Pro, Codex Pro and supplied-image Google AI Plus observations separate from other plans. Mapping does not create live evidence. The distinct source-clock overgeneralization is F-08. |
| 5. Cap at/below usage | PASS for arithmetic: E05b/E05c prove positive/zero norm with over-cap state, never negative N. At exact equality section 8.8 says at limit, not over cap by zero; a known-zero provider limit still needs F-11 resolved. |
| 6. Reset/scoped estimator samples | PASS for stated exclusions: sections 7.1 to 7.4 reject cross-reset spans and CL-M/main-to-scoped pairing, with independent CX-A pairs only. FAIL for reliable reconstruction and the zero-C case, F-01/F-03/F-04. |

Word-by-word requirement check (every clause, including exceptions):

| Requirement | Result against research |
| --- | --- |
| R-03 | Sections 5.4/8.7 implement local caps, min(provider, cap), either known value, cap provenance and no percentage cap. Unknown-unit pools and UI-only pools remain explicitly restricted/pending, not proof of the word "always"; PD-034-01/03 remain owner decisions. Zero-limit handling needs F-11. |
| R-04 | PASS for one global weekday set, Monday to Friday by default, section 8.2. E07b explicitly overrides the default for a synthetic case. |
| R-05 | W/Wr/E, adaptive max, baseline/deviation signs, ordinary U-U0, cap recomputation and fractional days match the text and all rows. FAIL for reconstructing fixed day-start data (F-01), period-relative U (F-05), and the known-zero exception (F-11). Wr=0 explicitly hides the norm and displays only remainder. Section 6.2's first-observation "since" label is an explicit approximation, not actual midnight measurement. |
| R-06 | Provider resets, derived starts, assumed-month labelling and expiry separation are described. FAIL for inconsistent missing-period fallback (F-07) and the generalized Copilot start (F-08); assumed-cycle consumption also needs F-05. |
| R-07 | Local paired formula, three-instance minimum, median/MAD, labels and hidden-until-ready rule are stated. FAIL for a ready C=0 (F-03), reproducibility (F-01), and N/C versus T/C on partial days (F-06). |
| R-11 | One-day-or-longer eligibility, five-hour exclusion, work-day weighting, day-off usage reducing the next norm, and deferred holidays are explicit. Day-off neutrality has an unresolved exception, F-10. |
| 2026-09-29 amendments / D-184 | Sections 6/7 expressly require one local series, retain history, defer its extension to AIU-029 and keep security-lifecycle review as an implementation precondition. No section 8/9 calculation currently calls for provider history. FAIL for the surviving PD-034-03 dependency on LC-17, F-12. Google AI Plus remains a personal subscription with unknown credit fields; no API-credit billing requirement or new lookup is inferred. |

This table is T-10 review evidence only. T-11 still owns the consolidated AC-01 to AC-06
verdicts and Gate A package. Product tests, browser use, web research, live checks, provider
requests, sign-in and credential access: NOT_RUN. Security-lifecycle implementation review:
NOT_RUN, still a precondition. Gate A and Phase B: NOT_RUN.

### Open architectural findings (Step 4)

All counterexamples below are synthetic. Each finding is open and must be resolved or
turned into an owner decision by T-11; none authorizes a design change in T-10.

T-11 resolved all twelve on 2026-09-29 by design changes; none became a new decision. Each
finding's status line records this, and its resolution follows the status line.

#### F-01 - Run compression cannot reproduce day-start and estimator validity

- **Status:** RESOLVED by T-11; material to AC-05/AC-06 and D-184.
- **Resolution (T-11):** changed the design in research sections 6.2 to 6.4, 7.3 and 7.6. A run
  now ends at every gap (more than 15 minutes between valid readings), at a new period instance
  and at a plan or source change, so a run means continuous observation and every gap is stored.
  `U0` uses explicit midnight rules: covered, carried, bracketed (equal values around a gap) and
  first of day. Estimator samples read both windows from the runs covering the span's ends; gaps
  inside a span no longer matter because both ends must share one period instance. The
  invalidation "when the pool stops being returned" was dropped: it cannot be rebuilt from
  per-limit runs, and the limit key is the identity. Cases P01 to P04, P11, S05 and S07.
- **Location:** research sections 6.2 to 6.4, 7.3 and 7.6.
- **Problem:** first-seen/last-confirmed endpoints alone do not preserve the confirmations,
  gaps and invalidation events needed by the consumers of the single local series.
- **Evidence:** an unchanged value confirmed at 23:55 and 00:05 has one run with last
  confirmed 00:05. Section 6.3 can no longer find a last-confirmed time in the 15 minutes
  before midnight, and there is no run first seen after midnight either. A failure between
  two equal successful values disappears when the same run's last-confirmed time advances.
  Plan changes or pool disappearance do not start a run under the listed triggers, although
  section 7.3 must invalidate samples on those events. Section 7.6 therefore cannot always
  reconstruct the claimed valid paired span or history gaps from stored fields.
- **Suggested resolution:** specify sufficient local confirmation/validity and invalidation
  events or mandatory run boundaries, including midnight carry and gaps. Define how paired
  timestamps are recovered. Prove day-start stability and sample rejection after a reread
  of the series, including equal values before/after a failure or plan change.

#### F-02 - Balance-only consumption has no stored balance input

- **Status:** RESOLVED by T-11; material to AC-03/AC-05 and PD-034-01.
- **Resolution (T-11):** changed the design in sections 5.5, 6.2 and 6.5. The series stores a
  balance pool's signed remaining balance. Tracked consumption (6.5) defines the first baseline
  (the value at `S`, the gap rule, or zero on first sight), ignores top-ups and restarts with
  R-06's assumed month. The figure stays a labelled estimate and no allotment is invented.
  PD-034-01 stays an owner decision; its impact now points to section 6.5.
- **Location:** research sections 5.3, 5.5 CX-B, 6.3 and 11 PD-034-01(b).
- **Problem:** the series stores used value, but CX-B has only remaining balance. The
  recommended local accumulation cannot be reconstructed from the proposed fields.
- **Evidence:** two local balances of 100 and 90 imply an observed decrease of 10. Both
  CX-B used values are unknown, so storing the specified used-value field preserves neither
  input. Section 6.3 nevertheless says balance decreases derive from this same series.
- **Suggested resolution:** if (b) is accepted, include signed remaining balances and their
  validity/period provenance in the single series, with a defined first baseline and reset
  policy. Keep usage explicitly estimated and do not invent a provider allotment.

#### F-03 - Zero weekly delta can make a zero estimator denominator ready

- **Status:** RESOLVED by T-11; material to AC-06/R-07.
- **Resolution (T-11):** changed section 7.2. Readiness now also needs a median above 0 and
  weekly changes adding up to at least 5 percentage points; a zero weekly change remains a valid
  sample. Three zero samples and too little weekly movement both give "not ready" (S01, S03).
- **Location:** research sections 7.2, 7.3 and 7.5.
- **Problem:** confidence accepts C=0, then both session figures divide by it.
- **Evidence:** three otherwise valid distinct instances with delta-s=10 and delta-w=0
  each produce c=0; none meets a listed exclusion. Median=0 and MAD=0 satisfy the stated
  MAD <= 25% of median threshold. Remaining weekly/C and today's norm/C are undefined.
  Integer-rounded weekly values can produce these inputs without a negative delta.
- **Suggested resolution:** define a strictly positive, meaningful C requirement and a
  not-ready result for zero/insufficient weekly movement; retain the estimate label and
  include a three-zero-sample acceptance case.

#### F-04 - Changed reset timestamps are not sufficient proof of replenishment

- **Status:** RESOLVED by T-11; material to AC-04/AC-05/AC-06.
- **Resolution (T-11):** changed sections 6.3, 6.4 and 8.6. A new period instance needs a
  rollover (the previous reset has passed and the new reset is more than 60 seconds later) or
  an early replenishment (a decrease beyond rounding with a reset moved by more than 60
  seconds). Jitter, moving bounds and adjustments change only the displayed reset, and the same
  rule serves `U0` and the estimator. The authoritative boundary rule is section 6.4, rule 1:
  `U0 = 0` when the counter is known to have restarted today, including a late first post-reset
  reading; otherwise the midnight rules apply within the new instance. It replaces the T-09
  Step 1 phrase. Cases P05 to P09 and E06b.
- **Location:** research M-10, sections 6.2 rule 3, 7.3 and 8.6; tasks T-09 Step 1.
- **Problem:** section 8.6 treats any different reset instant as a new period and sets U0=0,
  while the estimator explicitly tolerates 60 seconds of relative-reset jitter. Section 3
  also records a moving Antigravity reset. A boundary update need not mean the counter reset.
- **Evidence:** a same-period reading changing reset from 15:00:00 to 15:00:01 and usage
  from 40 to 41 resets U0 to zero under 8.6, inflating used today to 41 and changing a norm
  that R-05 fixes for the day. Section 6.3's exact period-end match also loses the carried
  baseline. Separately, T-09 Step 1 calls for the post-reset reading as U0, whereas sections
  6.2/8.6 and E06b prescribe zero; the intended boundary rule needs one authoritative form.
- **Suggested resolution:** distinguish reset identity/replenishment from timestamp jitter,
  moving bounds and corrections, and use the same identity rule for U0 and estimator spans.
  Resolve and document the zero-versus-first-reading choice, including late post-reset reads.

#### F-05 - Assumed calendar reset has no period-relative consumption rule

- **Status:** RESOLVED by T-11; material to AC-04/AC-05/R-05/R-06.
- **Resolution (T-11):** changed sections 6.5, 8.3 (rule 4), 8.6 and 8.8. Under an assumed
  period, `U` is the consumption tracked from the series since `S`, not the provider's cumulative
  counter. Decreases count nothing and are shown as facts; a missing boundary reading follows
  explicit rules; the provider's cumulative value and limit stay separate facts and can still set
  the limit state. E12a reproduces this finding's counter across `S`, and E12b shows a provider
  reset in the middle of the period.
- **Location:** research sections 5.5 CL-X/CL-D, 6.2, 8.3 and 8.6.
- **Problem:** a local assumed month boundary is not necessarily the provider counter's
  boundary. The rules change S/R without defining how provider cumulative U becomes used
  within the assumed period; treating the boundary as replenishment incorrectly sets U0=0.
- **Evidence:** an unreset counter of 100 just before local month start and 120 afterwards
  represents a locally observed increase of 20. With new U0=0, used today becomes 120;
  retaining U0=100 instead still deducts the prior period's 100 from the new month's cap.
  The later "observed reset" rule only handles decreases, not this unchanged/increasing
  counter at the assumed boundary. The matrix explicitly leaves the true clock unknown.
- **Suggested resolution:** define period-relative usage/offsets from the local series for
  assumed periods, with observed provider resets, missing boundary readings and corrections
  handled explicitly; or keep the affected figures unavailable pending an owner decision.
  Preserve provider cumulative facts separately from any local estimate.

#### F-06 - Today's session figure disagrees on partial days

- **Status:** RESOLVED by T-11; material to AC-04/AC-06/R-07.
- **Resolution (T-11):** changed section 7.5 to `T / C`, as in section 8.4, and stated that
  R-07's "today's weekly norm" is read as today's share `T`. Partial-day result: S09, where E06b
  with `C` = 12 gives "< 1 session".
- **Location:** research sections 7.5 and 8.4, E06b.
- **Problem:** 7.5 uses N/C; 8.4 uses T/C, where T=N*f(today). These differ on partial days.
- **Evidence:** E06b has N=20 and T=7.5. With a synthetic ready C=12, 7.5 displays about
  1 session, while 8.4 displays less than 1. The E03 check cannot expose this because its
  today is a full work day. R-07 says today's weekly norm, while R-05 permits defined
  proportional partial days; the chosen interpretation must be consistent.
- **Suggested resolution:** select one daily-session input, align both sections and the
  labels, and add a partial-day session result to the examples.

#### F-07 - Missing-duration windows bypass the specified period fallback

- **Status:** RESOLVED by T-11; material to AC-03/AC-04/R-06.
- **Resolution (T-11):** changed section 8.3 ("Period from supplied facts"). Supplied provider
  facts are always kept and only missing ones are filled: reset and duration known gives a
  derived start; reset known and a family documented as monthly gives a start one month before
  the reset, marked assumed; reset known without a duration or period type gives no budget and
  "period unknown", because the calendar month would replace a supplied reset that R-06 keeps;
  no reset gives R-06's calendar-month fallback. This is the per-family confirmation R-06 asks
  Phase A for, not a silent narrowing, and it is listed for the owner at Gate A. Case E13.
- **Location:** research M-05, sections 5.5, 8.1 and 8.3; spec R-06.
- **Problem:** R-06 says a missing provider period defaults to the calendar month, but M-05
  says an unknown-duration window gets no budget; 8.3 limits fallback to CL-X/CL-D and
  conditional CX-B, while CX-I receives a different assumed-start rule.
- **Evidence:** a Codex percentage window without limit_window_seconds cannot obtain S in
  5.5 and is period unknown in 8.1, even if its current percentage is known. The case with
  a known reset but unknown start/duration also lacks a common rule; substituting a new
  reset would conflict with R-06's instruction to retain a supplied provider reset.
- **Suggested resolution:** explicitly cover missing start, reset and duration separately
  for each family/plan, preserving known provider facts. Reconcile the exclusions with R-06
  or raise a precise amendment decision rather than silently narrowing its fallback.

#### F-08 - Copilot calendar start is generalized beyond the cited evidence

- **Status:** RESOLVED by T-11; material to AC-03/AC-04/R-06.
- **Resolution (T-11):** changed M-04 and sections 5.2, 5.5 and 8.3 (rule 2). GH-C, GH-I and
  GH-P get a start one calendar month before the provider reset in UTC, marked assumed, instead
  of a derived first-of-month start. A documented clock applies only to an identified pool, and
  neither the plan label nor the Free evidence identifies one. The E08a and E08b figures are
  unchanged; their notes now say that the start is assumed.
- **Location:** research section 3 Copilot field table/closing paragraph, M-04,
  section 5.5 GH-C/GH-I/GH-P and section 8.3.
- **Problem:** all three request families receive a provider-derived first-of-month UTC
  start based on G5, although the matrix limits that clock evidence to identified legacy
  premium requests (and separately documented AI-credit pools).
- **Evidence:** section 3 says month start follows documented semantics only after
  identifying the applicable pool and that neither source proves all internal pools share
  that clock. Section 5.5 instead applies the previous-month UTC start to GH-C, GH-I and
  GH-P together, without that condition. Source-derived certainty is overstated.
- **Suggested resolution:** gate derived calendar bounds on the established pool semantics;
  preserve unresolved starts or apply an explicitly assumed fallback otherwise. Do not use
  a plan label or personal Free evidence to identify paid-plan billing generation.

#### F-09 - CX-I mixes unknown-unit amounts with a percentage-window contract

- **Status:** RESOLVED by T-11; material to AC-03/AC-04 and review focus 1/2.
- **Resolution (T-11):** changed M-07 and sections 5.2, 5.3 and 5.5. CX-I is a percent-window
  whose used and remaining are the provider percentages, with `L = 100 %`. Its raw strings are a
  secondary amount of unknown unit on the window's existing `Amount` member, shown as a fact and
  never budgeted, tracked or capped. Without percentages there is no budget, and the raw amounts
  never stand in for them.
- **Location:** research sections 5.2, 5.3 and 5.5 CX-I; sections 6.3 and 8.1.
- **Problem:** the row calls CX-I a percent-window but maps raw used/limit/remaining amounts
  beside percentages. Section 5.2 says a percent-window has no absolute limit and its used
  and remaining are percentages; its type extension does not define a separate unknown-unit
  amount channel. Missing percentages therefore have no unambiguous budgetable projection.
- **Evidence:** synthetic used="40", limit="200", used_percent=20 yields two different
  used quantities. Pairing raw 40 with L=100 violates 8.1; pairing raw 200 with percentage
  used violates the percent-window vocabulary. Only the percentage channel is currently
  interpretable, and absent percentage must not be silently replaced by an amount.
- **Suggested resolution:** define the normalized percentage fields and L=100 explicitly,
  retaining opaque/unknown-unit amounts separately if required, or specify another type
  arrangement. Keep the raw amount out of budget and local-series arithmetic until its unit
  is established; define behavior when only raw amounts are present.

#### F-10 - Day-off state exception is not reconciled with R-11

- **Status:** RESOLVED by T-11; material to AC-04/R-11.
- **Resolution (T-11):** changed sections 8.5 and 8.8. A limit has a limit state (over, at
  limit, within), a fact on every day, and a budget state (not ready, neutral, today used,
  attention, OK). R-11's neutral is the budget state of a day off; an exhausted or over-cap limit
  shows its limit state on a day off too, as R-10 and R-15 require. E10a and E10b add both
  day-off cases; R-11's text needs no change.
- **Location:** research sections 8.5 and 8.8; spec R-10/R-11.
- **Problem:** R-11 requires a neutral state on a day off, but 8.5/8.8 explicitly let at-limit
  and over-cap states override neutral. The safety intent of R-10 is understandable, but the
  two requirements need a stated distinction between budget state and binding-limit state.
- **Evidence:** a day-off case with L=100 and U=100 is at limit under the research and
  neutral under the literal R-11 wording. E04a and E09 are below L and cannot resolve this.
- **Suggested resolution:** distinguish the factual exhausted/binding state from the neutral
  daily-budget state, or record an owner decision approving the exception; add the exhausted
  and over-cap day-off cases. Do not silently change R-11 in T-10.

#### F-11 - Known-zero limit exception conflicts with R-05

- **Status:** RESOLVED by T-11; material to AC-04/R-03/R-05.
- **Resolution (T-11):** changed M-01 and sections 5.2, 5.4, 8.1 and 8.8. An explicit unlimited
  flag takes precedence over any amount, including 0 (E11c). A known provider 0 is a zero
  entitlement, and a personal cap of 0 is allowed. For `L = 0`, R-05's formulas give
  `N = B = 0`, so no budget is offered: this presents R-05's result and is not an exception to
  it. The state reads "not included" or "capped at 0" while unused, which does not bind the
  account, and over by `U` with use, including provider overage (E11a, E11b, E11d).
- **Location:** research M-01, sections 5.4, 8.1 and 8.8; spec R-05.
- **Problem:** R-05 applies to a known L/S/R and defines a zero-clamped norm. Research
  instead makes a provider limit of zero "not included, no budget", without explaining
  whether known usage/overage and a zero personal cap follow a different state policy.
- **Evidence:** with provider L=0, known period, U0=0, U=5 and Wr>0, R-05 yields N=B=0,
  used today=5 and left today=-5; section 8.8 also implies over cap by 5. Section 5.4's
  known-zero row and 8.1 suppress the budget. This is a design exception, not unknown data.
- **Suggested resolution:** define zero entitlement, explicit unlimited with an amount,
  and zero personal cap separately, including precedence of factual overage and budget
  availability. Reconcile the no-budget exception with R-05 or seek an owner amendment.

#### F-12 - UI-only-pool decision still points to provider history as a way forward

- **Status:** RESOLVED by T-11; material to D-184 and the 2026-09-29 local-only amendment.
- **Resolution (T-11):** changed section 11 (PD-034-03) and section 10. The impact now names
  only a current quota source, for example LC-16 finding the pool in the existing connection's
  response, and states that a provider history report cannot close the gap after D-184. A
  section 10 note keeps LC-11 and LC-17 as historical, unexecuted checks that cannot supply data.
  History display stays derived only from the local series.
- **Location:** research section 11 PD-034-03 Impact, with G-GH-2 and LC-17 as context.
- **Problem:** the pending decision says the main Copilot credit pool stays invisible until
  "LC-16/17 or a later transport" closes the gap. LC-17 is explicitly an AI Usage provider
  history report check, which D-184 no longer allows as a data source.
- **Evidence:** the currently operative recommendation still offers LC-17 as a route to
  showing that pool, while sections 6/7 correctly prohibit provider-history-derived figures.
  A history report also does not establish a current allotment (G-GH-2). Historical LC
  records can remain, but cannot be a dependency for future budget or history display.
- **Suggested resolution:** remove provider-history retrieval from the decision's prospective
  dependencies; identify a current quota source if one is ever established, or retain the
  explicit unknown. Keep any history display derived only from the one local series.

### Publication checks (Steps 5 and 6)

Document validator PASS: the required `dotnet run --project
tools/AiUsage.ProjectValidation --no-restore -- --root . --json` command returned
`{"valid":true,"diagnostics":[]}`. Diff check PASS: `git diff --check` returned no errors.
Added-line secret/personal-data pattern scan PASS: zero email, UUID, bearer/JWT, API-key,
secret-assignment or local-identity matches. Primary content review PASS: all new numerical
examples are synthetic; no account names, actual balances, credentials, payloads or captures
were added. Integrated diff/scope and reference review PASS for delivery of this review:
only research.md, tasks.md and verification.md change, with one mechanical research edit;
T-11 remains pending. Final task/handoff bookkeeping is included in pre-commit validation.
Open findings:
**12 (F-01 to F-12)**. These findings block treating the design as accepted for implementation;
they do not block committing this requested review record. No design resolution, T-11 work,
Gate A review or Phase B work is included.

## Owner direction D-184, local-only history - 2026-09-29

Base e5013fa. The owner directed that usage history comes only from local tracking, never
from provider-supplied history, and that history is kept rather than removed. Recorded as
D-184 in the decision register, an owner amendment in spec.md, direction notes on AIU-011
and AIU-029 in the backlog, and a new T-10 Step 2 check. Research sections 6.3, 6.4 and 7.6
replace the two-day day-start records and separate estimator records with one local reading
series (runs of unchanged values, gaps kept, at least 35 days). Section 5 references were
aligned. No rule or figure of sections 8 and 9 changes: `U0` rules 1 to 4 are unchanged in
meaning. No product code, transport or stored format changed; removing AIU-011 retrieval is
left to an unselected follow-up item. Validator `--json` returned `valid: true` with no
diagnostics; `git diff --check` passed; added-line secret scan found zero matches.
## T-09 budget rules and worked examples - 2026-09-29

Base e0ef2e6, same [opus] session; no account, provider request or credential access.

- Research section 8: eligibility, local day and work days, day weights for partial and
  23/25-hour days, calendar, rolling and assumed periods, figures, day off and `Wr = 0`,
  resets during the day, including an observed reset under an assumed period, cap changes,
  state precedence and display rounding.
- Research section 9: fifteen rows covering every A-7 case with 2026 dates in Europe/London:
  a USD 300 cap within USD 500 in minor units, 17,000 credits, a weekly window with work days,
  day-off usage, cap changes including below `U0` and between `U0` and `U`, a reset during the
  day, a weekly window across the DST change, a 25-hour day, months with 20 and 23 work days,
  and no remaining work days.
- Step 3 checks: every row was recomputed by hand for its defining terms and by an
  uncommitted scratch script in the session's temporary directory; all derived values
  matched. No rule contradicts another: the day-off rule removes the norm only on the day off,
  and usage flows into the next `U0`. Review focus 3: a reset in the middle of the day and DST
  days have defined behavior. Review focus 5: `N` is clamped at 0 and the state is over cap.
  Validator `--json` returned `valid: true` with no diagnostics; `git diff --check` passed;
  added-line secret scan found zero matches.
## T-08 start-of-day source and five-hour estimator - 2026-09-29

Base c8fa7ca, same [opus] session; no account, provider request or credential access.

- Research sections 6 and 7. `U0` is a local day-start reading for all four providers:
  history reports measure a different counter on an unestablished provider day and are
  unavailable for work and Copilot routes. Minimal record, two-day retention, relation to
  AIU-029 and storage are specified. The security-lifecycle review is recorded as an
  implementation precondition, not performed.
- Estimator: pairs by duration within one pool (Claude CL-S/CL-W, Codex per group,
  Antigravity per model group); per-instance span sample, median of 10 within 28 days,
  minimum 3, MAD at most 25 % of the median, explicit exclusions for either reset, small or
  negative change, stale readings, saturation and source switch. Review focus 6: straddling
  pairs are rejected and model-scoped weekly limits get no session figure (section 7.4).
- Step 4 checks: every provider has a decided `U0` source; the estimator names inputs,
  formula, minimum samples, invalidation and label. Validator `--json` returned
  `valid: true` with no diagnostics; `git diff --check` passed; added-line secret scan found
  zero matches.
## T-07 normalized limit model - 2026-09-29

Base 036bf81; `git pull` was up to date and main was clean. [opus] session; no account,
browser, provider request or credential access.

- Step 1: model notes M-01 to M-10 added to research section 4.
- Steps 2 to 6: research section 5 (vocabulary, Core type extension, personal cap and effective
  limit, provider mapping, source independence, stored-format impact) and PD-034-01 to
  PD-034-03 in section 11.
- Step 7 checks, primary review: every section 3 family (25 families, CL-S to AG-T) has a
  section 5.5 row, "not represented" rows included. Money is only minor units, exponent and
  currency, with exact upward rescaling within one currency. No field sums or converts across
  units. Caps, observations and budget inputs are keyed by account target ID and limit key,
  never by snapshot source. Validator `--json` returned `valid: true` with no diagnostics;
  `git diff --check` passed; added-line secret scan found zero matches.

## T-06 closure - 2026-09-29

Base e4c4535; main was clean at closure start. Earlier continuation records below are
historical checkpoints, superseded by this closure for outstanding task state.

- LC-01: retained earlier PASS; no repeat.
- LC-07: retained PASS for agent-observed personal Pro credit UI only. Balance is not a
  recurring allowance; disabled reload is not a spending cap. Missing periods/expiry and
  transport remain unknown. No Claude/Codex percentage regression was performed.
- LC-22 quota structure: PASS for inspection of an owner-supplied image only, not an
  agent-operated live session. Google AI Plus label; Gemini Flash/Pro group and shared
  Claude Opus/Sonnet/GPT-OSS group; weekly remaining percentages and within-group sharing.
  No absolute cap, period start, reset/expiry or five-hour row visible in the supplied image.
  Capture time and completeness of the page are unknown. No amount-unit mapping to
  `remainingAmount` or inference about Free, Pro or Ultra follows.
- LC-22 Google One activity: NOT_RUN. The owner reports the proposed section was not found
  and ended the lookup. The agent's earlier navigation guidance was not verified for this
  tier. Credit unit/scope/balance/allowance/period/reset/expiry stay unknown, not absent or zero.
- LC-03/04/08/09/11 and LC-12 through LC-17: NOT_RUN,
  "postponed by owner: work account or manual lookup". LC-02/06 are nonmatching plans;
  LC-05/10/18/19/20/21 remain NOT_RUN, not authorized.

Owner clarification is recorded in spec.md: ordinary personal Google AI Plus, no work/API
subscription; no Antigravity API-credit billing implementation. Google One product credits
are distinct and remain unverified. No CLI access, account operation or browser action was
performed during this closure. The supplied image is not copied or staged; only sanitized
structure/semantics are recorded. The Antigravity audit date is explicitly scoped to recording
owner-supplied image evidence, with capture time unknown; no transport live date advances.

Primary acceptance review: every G-CL, G-CX, G-GH and G-AG gap retains either source/UI
evidence or an explicit unknown with the LC that could close it if separately authorized.
AC-02 PASS for that documentation criterion, not all live checks. T-06 acceptance is met
with the owner's ended lookup and explicit limitations; T-07 [opus] is the exact next task
and has not started. No product code or security/lifecycle behavior changed; independent
review is not required for this documentation-only closure under CONTRIBUTING.md.

Closure checks PASS: secret/personal-data pattern scan of added lines found zero matches;
primary content review found no identities, actual balances, percentages, personal dates or
captures in the five changed Markdown files. Primary diff/acceptance review found no open
material findings; evidence provenance, links, scope clarification and T-07 handoff agree.
Document validator `--json` returned `valid: true` and no diagnostics; `git diff --check`
passed. Product tests and new transport/live-account probes NOT_RUN (documentation only).

## T-06 personal Pro continuation - 2026-09-28

Base 2c16f71; initial git pull was already up to date and main was clean. The owner renewed
read-only personal-account authorization, Chrome only, with owner-led authentication.
The remaining scope excludes completed LC-01 and Claude/Codex percentage regressions.

LC-07 PASS for scoped UI observation. Pro and initial credit labels were owner-reported,
then independently confirmed by the agent: Billing showed ChatGPT Pro; Usage Overview
showed credits remaining / Current balance, Work and Codex continuation scope, and disabled
Automatic reload. No separate allowance/cap, credit period, reset/expiry or numeric spending
cap was displayed in the observed credit section. Subscription renewal and reset-inventory
expiry remain separate from credit expiry. No personal amounts/dates or identities retained.
LC-06 NOT_RUN because the observed plan is Pro. No transport conclusions follow.

The browser-tab connector returned `Browser is not available: chrome`. The dedicated native
Computer Use skill successfully read the existing Chrome window. Accessibility text omitted
the main content with `child_limit`; transient visual observations supplied the evidence,
without saving captures to files. Only read-only Account/Billing navigation occurred; no
authentication, account selection, settings change, purchase, redemption, CLI access,
AI Usage connection or hidden browser/network inspection was performed.

LC-22 remains pending the owner-opened Google AI Plus surface. Work-account and Copilot
checks remain NOT_RUN: "postponed by owner: work account or manual lookup". Unlisted checks
remain unauthorized. T-06 is in progress; T-07 has not started.

Checkpoint verification PASS: added-line secret/personal-data pattern scan returned zero
matches; primary diff/acceptance review found only sanitized structure and semantics in
four Markdown files, with no captures or account values. Validator `--json` returned
`valid: true` with no diagnostics; `git diff --check` passed. Links and evidence separation
were reviewed. Product tests NOT_RUN (documentation only). LC-22 remains outstanding.

Phase A source research has started. The owner approved the specification on 2026-09-26.

## Gates

| Gate | Status | Record |
| --- | --- | --- |
| Specification review | PASS | Owner approved [spec.md](spec.md) in the session conversation on 2026-09-26. |
| Gate A | PASS | Owner accepted Phase A in the session conversation on 2026-09-29, with PD-034-01 (b), PD-034-03 (a) and the R-05, R-06 and R-11 readings (D-185). |
| Gate B | NOT_RUN | T-14 independent review completed on 2026-09-29 with four open findings, F-13 to F-16. T-15 must resolve them and prepare the brief for owner review. |

## Results by acceptance criterion

| AC | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | T-11: section 3 matrix for every provider and plan in scope; coverage script 700 of 700 cells. |
| AC-02 | PASS | T-06 closure: research.md section 4 preserves each gap's evidence or explicit unknown and closing LC; deferred checks are not claimed as executed. Re-checked by T-11 after D-184. |
| AC-03 | PASS | T-11: section 5 model, 25 of 25 families mapped, stored-format impact in 5.7. |
| AC-04 | PASS | T-11: section 8 re-checked against R-03 to R-07 and R-11; section 9.1 recomputed, 0 mismatches. |
| AC-05 | PASS | T-11: `U0` source per provider (6.1), local series and retention (6.2 to 6.6), security-lifecycle precondition noted. |
| AC-06 | PASS | T-11: section 7 inputs, formula, minimum samples, invalidation and label; cases S01 to S09. |
| AC-07 | PASS | Gate A owner review recorded on 2026-09-29, before any Phase B work (D-185). |
| AC-08 | NOT_RUN | |
| AC-09 | NOT_RUN | |
| AC-10 | NOT_RUN | |

## T-01 - 2026-09-26

PASS: current-source inventory and Core member coverage in research.md section 2, reviewed against baseline 27564d8. All four parsers, clients, state/cache formats and Windows LiveMapping inspected. Document validator command from tasks.md printed valid=true with no diagnostics; git diff --check exited 0. These are document/source checks only; product tests and live requests NOT_RUN. No product or stored-format change.

T-01 bookkeeping correction: the initial draft validated, but marking done with prose in the evidence field produced DONE_WITHOUT_EVIDENCE. Commit 6ee7f42 was mistakenly pushed before resolving that failure. The follow-up uses a repository-relative evidence artifact path; final validation is recorded only after rerun. No source finding changed.

## T-02 - 2026-09-26

PASS: research.md Claude plan/field matrix, G-CL-1 through G-CL-5, C1-C11 and provider append reviewed together. Pinned OMP and latest v18.3.2 inspected; no Team/Enterprise live claims, null limits stay unknown, current consumption and legacy Enterprise distinguished. Validator --json valid=true with no diagnostics and git diff --check exit 0. Live LC-01 through LC-05 NOT_RUN; no account requests. T-01 corrected evidence reference also passed validation before commit 4ed5ee4.

## T-03 - 2026-09-26

PASS: Codex matrix, G-CX-1 through G-CX-4, O1-O8 and provider append reviewed. Official pinned spend-control nested type reveals unparsed amounts/resets; units and plan response presence remain unknown. Balance never used as allotment; actual duration is response-driven; historical HTTP 400/403 causes not inferred. Validator --json valid=true and diff check exit 0. LC-06 through LC-11 NOT_RUN; no new account request.

## T-04 - 2026-09-26

PASS: Copilot matrix, G-GH-1 through G-GH-4, G1-G7 and provider record reviewed. Explicit unlimited remains distinct from unknown; current credit billing differs from legacy annual premium requests; Free live evidence is not generalized. Parser date normalization distinguished from documented UTC monthly reset. Validator --json valid=true and diff check exit 0. LC-12 through LC-17 NOT_RUN; no authenticated request.

## T-05 - 2026-09-26

PASS: Antigravity plan/field matrix, G-AG-1 through G-AG-4, A1-A6 and provider append reviewed. remainingAmount remains unit unknown; source credit UI distinguished from reusable transport and monthly Flow credits. No Free-to-paid generalization. Validator --json valid=true and diff check exit 0. LC-18 through LC-21 NOT_RUN; no CLI credential read, provisioning or provider request.

## T-06 preparation - 2026-09-26

Steps 1 and 2 completed: section 10 consolidates LC-01 through LC-21 with account, surface, evidence target and risk. The owner received per-ID authorization questions grouped by provider; each check requires its own response. Authorization is pending, and every live verdict remains NOT_RUN. No account access, sign-in or authenticated request occurred. T-06 remains in progress; T-07 has not been handed off.

Preparation checks PASS: the Phase A diff contains only eight Markdown files; added-line credential/identity pattern scan returned zero matches, and primary content review found no personal data, raw payloads or captures. Validator --json valid=true and git diff --check exit 0. These checks establish document consistency only and do not establish live-provider success. Repeat the privacy and document checks after any live-outcome edits.

## T-06 interruption checkpoint - 2026-09-26

Environment: Windows, default HTTPS browser Microsoft Edge; base f42247a. The initial
`git pull` reported already up to date and the working tree was clean. The owner's
session-scoped authorization covers only personal Claude Pro/Max after UI plan selection,
personal ChatGPT Plus/Pro after UI plan selection, and new LC-22 for Google AI Plus.

- BLOCKED: selecting the personal Claude and ChatGPT plan and observing LC-22. Initial
  in-app-browser navigation reached signed-out/public pages; no plan or usage was observed.
  At the owner's correction, in-app-browser use stopped. Native Computer Use opened the
  default Edge browser, then stopped this turn because it could not confidently determine
  the current browser URL to enforce policy. No browser input followed that stop.
- NOT_RUN: LC-01/02 and LC-06/07, because plan selection did not complete. Neither alternative
  is reported as the account's plan. LC-22 is separately BLOCKED before account observation.
- NOT_RUN: LC-03/04/08/09/11 and LC-12 through LC-17, reason
  "postponed by owner: work account or manual lookup".
- NOT_RUN: LC-05/10/18/19/20/21, not authorized. No AI Usage connection was accessed.
- No authenticated usage observation, sign-in, credential entry, account selection, terms
  acceptance, settings change or purchase occurred. No developer tools, network traffic,
  cookies, local storage or source CLI credentials were read.

The AI Plus matrix is separate and unknown/none. Existing provider matrices retain their
evidence levels, and all transport gaps remain open. Provider-record `live_verified_at`
values are unchanged because there are no UI-observed rows to date. T-06 remains in progress;
T-07 [opus] is the subsequent task, not ready for handoff on this checkpoint.

Checkpoint verification PASS: added-line secret/identity pattern scan found zero matches;
primary diff review found no account identities, balances, spend values, raw payloads or
screenshots. Only research.md, tasks.md and verification.md changed. Document validation
(`dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json`,
using the existing user-local SDK) returned `valid: true` with no diagnostics;
`git diff --check` passed. Links and the retained T-07 amendment were reviewed. These are
document checks only. Live quota checks and product tests did not pass or run at this checkpoint.

## T-06 Chrome continuation - 2026-09-26

Environment: Windows, owner-selected Google Chrome, existing signed-in personal session;
base 355599e. The owner corrected the browser selection to Chrome in this same session.

- LC-01 PASS, UI observation only: `https://claude.ai/settings/usage` opened the personal
  Usage page, whose plan label was Pro. Session and weekly percentage-used rows were present;
  reset forms were time-of-day and weekday/time respectively. No model-scoped weekly row was
  displayed. A usage-credit balance and monthly spending section were present, with a dollar
  symbol and an explicit no-spend-limit label. No actual values were retained.
- A separate cloud-session included-credit row displayed a balance and expiry with time,
  GMT offset, month and day. Research now distinguishes this CL-C family from general
  usage credits and monetary spending. Recurring allotment, exact grant start, ISO currency,
  wire exponent and transport mapping remain unknown. Product breakdowns are not new limits.
- LC-02 NOT_RUN: the observed personal plan is Pro. No Max or work-plan inference is made.
- LC-06/07 remain NOT_RUN: ChatGPT plan selection is BLOCKED. LC-22 remains BLOCKED.
  After the Claude observation, the native control tool stopped this turn on the new-tab
  action because it could not confidently determine Chrome's current URL to enforce policy.
  No further browser input followed that stop. No Codex or Google account observation occurred.
- Other postponed/not-authorized verdicts remain unchanged. No sign-in, account selection,
  state-changing provider action, hidden storage/network inspection or credential access.

The Claude audit date is advanced for its explicitly scoped UI observations only. Its
wire-field matrix retains source/none levels; the separate UI matrix records live cells.
The Codex and Antigravity provider dates remain unchanged. T-06 is still in progress.

Continuation checks PASS: added-line secret/identity and monetary-value pattern scan returned
zero matches; primary content review confirmed only sanitized UI structure, units and reset
forms in the four changed Markdown files. No screenshots/captures or account values are
staged. Validator `--json` returned `valid: true` with no diagnostics, and `git diff --check`
passed. UI/source separation and the new provider-record link were reviewed. Product tests
NOT_RUN because this checkpoint changes documentation only.

## T-06 Chrome retry - 2026-09-26

Base 62b5e2f, same Windows/Chrome session; owner explicitly requested another attempt.
Navigation to `https://chatgpt.com/codex/settings/usage` was submitted through the existing
Chrome tab. Window inventory subsequently reported ChatGPT. Reading the page timed out
with `computer-use request timed out: get_window_state`, including one text-capture retry
after window reselection and a screenshot-only capture. No ChatGPT plan or quota content
was observed, so LC-06/07 stay NOT_RUN with plan selection BLOCKED. LC-22 stays BLOCKED;
LC-01 remains the already completed UI observation. No new provider live date or matrix
evidence is added. No authentication or provider-state mutation occurred. Any transient
new-tab screen observation was not saved to the repository.

Retry checkpoint checks PASS: validator `--json` returned `valid: true` without diagnostics;
`git diff --check` passed; added-line secret/personal-data scan returned zero matches.
Primary review confirmed the two changed Markdown files contain only the blocker and
handoff update, without private values or captures. Product tests NOT_RUN (documents only).
