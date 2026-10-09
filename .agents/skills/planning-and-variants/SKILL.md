---
name: planning-and-variants
description: Turn an owner's request into an agreed plan before implementation. Use when the owner asks to brainstorm or plan, when a request adds a feature or changes behavior, and always when a change visibly alters the UI (rendered design variants first).
---
# planning-and-variants

The repository's planning step (decision R-217). It replaces the superpowers brainstorming
skill and follows AGENTS.md and CONTRIBUTING.md, which win wherever this text seems to differ.

## 1. Understand

- Read the request, the affected code and tests, the relevant spec and decisions, and check
  whether the work already exists on `main` or in another worktree.
- Write back the intended outcome, constraints and success criteria in a few lines, separating
  what the owner said from your assumptions. Skip questions the request already answers.
- A bug report (wrong behaviour, a regression against approved UI) is a diagnosis request, not
  a design question: use the systematic-debugging skill and fix it; no variants, no design gate.

## 2. Classify

Say the classification out loud so the owner can override it, and take the heavier one when
unsure:

- **Spike:** a feasibility question. State the question and the cheapest probe, run it, and
  report a recommendation; anything built is throwaway.
- **Change:** pick the tier (T0-T3) from CONTRIBUTING, Risk tiers. The tier sets the records:
  T0 has no spec; T1 and T2 get a one-page spec in `docs/specs/<T-NEW>-<slug>/`; T2 with
  parallel workers also gets tasks.md (see the plan-execution skill); T3 adds a design note when
  there is a real choice.

Hidden complexity found later upgrades the tier; say so and continue.

## 3. Ask once

Collect every open question that the always-ask list in AGENTS.md covers and ask them in one
batch, each with options and your recommendation first. When the owner asked to brainstorm, also
offer 2-3 approaches for the main design choice, with trade-offs and your recommendation. Decide everything else yourself and
record the reason. Never ask the owner to approve a spec, a plan or an execution mode
separately: the owner's yes to the design covers the path to a verified merge.

## 4. Visible UI: rendered variants

When the change visibly alters the UI (layout, card structure, controls, copy placement):

- Show 2-3 short variants as rendered mockups, not prose: use the inline visual widget or a
  design tool when available, with states or animation when they matter. Mark your
  recommendation.
- Put the variant choice in the same single question as any other open decisions.
- Record the pick as one line in the spec ("Variant B, owner, date"); record a decision only if
  it binds future work.

## 5. Plan and go

- State one plan line: tier, intended result and acceptance checks.
- For T1-T3 write the spec (Problem, R-xx, AC-xx, Out of scope; a bug may use current,
  expected and unchanged behaviour) following docs/workflow/formats.md.
- Then implement without further approval gates. For multi-part work, use the plan-execution
  skill. Nothing from this skill is written to `docs/superpowers` or `.superpowers`.
