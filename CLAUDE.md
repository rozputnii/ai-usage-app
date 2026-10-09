@AGENTS.md

## Claude Code

- Skills: the repository skills listed in AGENTS.md live in `.agents/skills/<name>/SKILL.md`.
  Reading the matching file before work in its area is mandatory. Thin wrappers in
  `.claude/skills/` make the same skills invocable by name; the canonical text stays in
  `.agents/skills/`.
- Agents: `aiu-reviewer` and `aiu-implementer` in `.claude/agents/` load the contracts in
  `.agents/agents/`. When they are not loaded (for example, added during the session), use a
  general-purpose agent and tell it to read and follow the contract file.
- Project settings in `.claude/settings.json` hold deny rules and plugin switches only
  (CONTRIBUTING, Agent permissions).
