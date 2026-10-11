# Micro-trial: review (S2)

Tests H5 and H6: which reviewer configuration finds the known defects of real diffs, with how
many false alarms, for how many tokens.

## Method

Each trial is a `Start-Run.ps1` run of the probe below, with a haiku low primary that only
delegates, base = the reviewed commit, and `-AgentsFile ..\arms\D-agents.json` (the lean
`reviewer`). Variants (`VARIANT`, `MODEL`, `EFFORT`):

| Variant | Agent | Model, effort |
| --- | --- | --- |
| R-sonnet | reviewer | sonnet, high |
| R-opus-med | reviewer | opus, medium |
| R-opus-high | reviewer | opus, high |
| R-repo | aiu-reviewer (repository agent, loads CLAUDE.md) | opus, high |

Two repeats per variant and diff. Score per run: recall = known defects reported (a finding
counts when it names the same problem and place), false alarms = Important or Critical findings
the operator cannot confirm, plus the subagent's cost, calls and cold start from `metrics.json`.

## Probe {#probe}

This is a measurement probe, not real work. Do exactly this:

1. Delegate one review to the agent `<VARIANT>` with model `<MODEL>` and effort `<EFFORT>`,
   with this brief and nothing else: "Review the change `<RANGE>` in this clone. Acceptance
   items: <ACCEPTANCE>. Rerun: <CHECKS>. Write the full review to
   `.ai-usage-local/review.md`."
2. Return the reviewer's short report verbatim in NOTES, followed by the full text of
   `.ai-usage-local/review.md`. Make no other changes and read nothing else yourself.

## Diffs and defect keys

### RV1 - peak-hours hint (`8eaf1f7`)

- RANGE `8eaf1f7^..8eaf1f7`; ACCEPTANCE "AC-01 to AC-04 in docs/specs/T-NEW-peak-hours-hint/spec.md";
  CHECKS "C5, C7, C2, C6 from docs/workflow/verification.md; C8 is out of scope".
- Key (the real review, fixed in `9e0b115`):
  - D1 (Important) the spec's verification record is missing.
  - D2 (Important) the title-bar passthrough regions are not refreshed when the hint appears or
    changes (`LedgerWindow.xaml.cs` property-changed handling has no `Peak` case).
  - D3 (Minor) the new AGENTS.md review rule is broader than the decision.
  - D4 (Minor) the tooltip writes the schedule as a separate literal instead of using the
    schedule constants (`PeakHours.cs`).
  - D5 (Minor) evidence details in peak-hours.md.
  - D6 (Minor) layout nits in the tray header (vertical alignment).

### RV2 - eco skill (`c583e27`)

- RANGE `c583e27^..c583e27`; ACCEPTANCE "the skill lowers token cost without lowering required
  quality and never changes what the rules require; R-222 and AGENTS.md agree with it";
  CHECKS "C2, C6".
- Key (fixed in `5815b68` and `5c12c61`):
  - E1 (Important) brief excerpts could replace reads a contract requires.
  - E2 (Important) a T0 diff is sent to a subagent review, contradicting CONTRIBUTING (the
    primary reviews T0).
  - E3 (Important) outside a parallel run the implementer's stop/merge step conflicts with its
    contract (step 4 and 5).
  - E4 (Minor) the escalation ladder has no last step and treats NEEDS_CONTEXT or an
    environmental BLOCKED as a model problem.
  - E5 (Minor) model-costs reading overstates opus medium against sonnet xhigh and omits TB4.
  - E6 (Minor) R-222 does not match the skill's escalation rules.

### RV3 - smoke harness (`02eddb6`)

- RANGE `02eddb6^..02eddb6`; ACCEPTANCE "the T-059 and T-060 specs' acceptance items for the
  smoke harness"; CHECKS "build tests/windows/AiUsage.Windows.Tests (Release); run
  tools/windows/Test-Erosion.ps1; desktop smokes are out of scope".
- Key (fixed in `42719a1`):
  - S1 (Minor) the rename-hover wait accepts any tooltip, so a leftover Refresh tooltip passes.
  - S2 (Minor) the squeeze waits for a height later squeezes already have.
  - S3 (Minor) the first-run test finds `SettingsButton` by an id that is not unique.

### RV4 - clean control (`804096a`)

- RANGE `804096a^..804096a`; ACCEPTANCE "the test-erosion sentinel flags a runtime
  `Assert.Skip`"; CHECKS "C6; run tools/windows/Test-Erosion.ps1".
- Key: no known defect. Any Important finding is a false alarm unless confirmed.
