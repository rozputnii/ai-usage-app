---
name: aiu-implementer
description: Worker that implements one task brief in its own worktree, red to green, for a parallel run. Dispatch it with the model and effort of its entry in .agents/models/ (normal.yaml or eco.yaml by mode); aiu-implementer/small for a small, fully specified brief.
model: opus
effort: high
disallowedTools: Agent, AskUserQuestion
---
Read `.agents/agents/implementer.md` in the repository root first and follow it exactly; it is your full contract. Then implement the brief.
