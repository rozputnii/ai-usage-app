# Vendored skills

Three skills are copied from the superpowers plugin (owner decision OD-7, 2026-10-09) so that
they no longer change with plugin updates:

| Skill | Files |
| --- | --- |
| systematic-debugging | SKILL.md, root-cause-tracing.md, defense-in-depth.md, condition-based-waiting.md, condition-based-waiting-example.ts, find-polluter.sh |
| verification-before-completion | SKILL.md |
| receiving-code-review | SKILL.md |

- Source: <https://github.com/obra/superpowers>, tag `v6.4.2`, commit
  `8ca22dba9a94f28898bbce59f2537ff4d87c747d`. Each copied file's Git blob hash was checked
  against that commit on 2026-10-09 and matched before the local change below (the upstream blob
  of `systematic-debugging/SKILL.md` is `095d194`).
- Licence: MIT, Copyright (c) 2025 Jesse Vincent; the full text is in
  [LICENSE-superpowers.txt](LICENSE-superpowers.txt).
- Local changes, the only ones: in `systematic-debugging/SKILL.md`, phase 4, the reference to
  `superpowers:test-driven-development` now points to the red-to-green rule in CONTRIBUTING, and
  the reference to `superpowers:verification-before-completion` now points to the vendored copy.
- The upstream skill-authoring files (`CREATION-LOG.md`, `test-*.md`) are not copied.
- Repository rules win where a vendored text differs (AGENTS.md, Skills and plugins). To update,
  copy the files from a newer pinned commit, review every line, and record the commit here.
