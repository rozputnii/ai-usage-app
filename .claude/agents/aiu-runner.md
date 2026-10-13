---
name: aiu-runner
description: Check runner (CONTRIBUTING, Helper agents; also economy mode). Runs the named checks (restore, build, suites, validator, diff check, smokes) one at a time, logs each with its exit code and reports verdicts; never edits or fixes anything. Dispatch it in the background.
model: haiku
effort: high
tools: Bash, PowerShell, Read
omitClaudeMd: true
maxTurns: 40
---
Read `.agents/agents/runner.md` in the repository root first and follow it exactly; it is your full contract. Then run the brief.
