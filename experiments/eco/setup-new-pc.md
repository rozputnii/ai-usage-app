# New PC setup and stage S1

The new PC has no Claude Code memory from the owner's current PC. Everything the experiment
needs is in this folder and in the repository rules; the host facts of the current PC are in
docs/workflow/host-environment.md and may not apply here.

## Prerequisites

1. Git, `gh` (signed in: `gh auth login`), Windows PowerShell 5.1.
2. The .NET SDK that `global.json` names; `dotnet tool install --global csharp-ls --version
   0.28.0` (the repository enables the `csharp-lsp` plugin; without it there is no LSP tool).
3. Claude Code CLI signed in to the owner's Max account. S0 used CLI 2.1.296; record the
   version you have. Set the user environment variable `DISABLE_AUTOUPDATER=1` for the whole
   experiment so the CLI version stays fixed.
4. graphify, only for the S2 exploration trial: `uv tool install graphifyy` (or
   `pipx install graphifyy`). Do not run `graphify install`: it registers a skill in the Claude
   configuration, which would change every arm. Use only the `graphify` command inside trial
   clones; its output never enters Git.
5. Short paths: clone the repository to a short folder (for example `C:\src\ai-usage-app`); run
   clones go under `C:\aiu-exp`. Deep paths break builds ("The filename or extension is too
   long").
6. The experiment branch in its own worktree:
   `git -C C:\src\ai-usage-app worktree add C:\src\aiu-eco exp/eco-roles`. Run the harness
   from `C:\src\aiu-eco\experiments\eco\tools` with `-Repo C:\src\ai-usage-app`, and keep that
   checkout's `main` fetched.
7. Desktop smokes (C8 and others) need an unlocked desktop. A locked desktop gives BLOCKED in
   every arm alike; note it.

Record in `results/S1.md`: OS build, CPU, RAM, CLI version, SDK version, whether `pwsh` exists,
which MCP servers and connectors the account brings (they change subagent cold starts), and
any first-run Defender delay. Host facts worth keeping for normal work go into
docs/workflow/host-environment.md as a separate, normal change.

## S1 procedure

1. **Self-test** (cents):
   - `Measure-Run.ps1 -SessionId <any recent session>` gives sane totals.
   - `Start-Run.ps1 ... -NoRun` on base `3564996`: the clone shows only `refs/heads/run`, no
     remote, and `git cat-file -e 49c7381` fails (later commits invisible).
   - The S0 smoke: a brief that asks for `git log -1` and one question to the `scout` agent,
     run in arms D and A with `-Model haiku -Effort low`. Expect exit 0, a run report, and a
     scout cold start of a few thousand tokens. A much larger one means the `--agents` fields
     were not applied: stop and investigate.
   - `Start-Judge.ps1` on those two runs: expect TIE.
2. **Calibration**: ask the owner to pause other Claude use, record the weekly `/usage`
   percent, run replay RP3 in arm A, record `/usage` again, and compute API-equivalent dollars
   per percent. Repeat once with RP3 in arm C.
3. **Pilot**: judge RP3 A against RP3 C with `Start-Judge.ps1 -DefectKey ..\tasks\replays.md`.
   Check that the judge's verdict, the metrics and the run reports are consistent.
4. Write `results/S1.md` (environment, self-test, calibration, pilot, problems found and fixed),
   commit on `exp/eco-roles`, push `origin/exp/eco-roles`, and update the README status and next
   action.

## Operator prompt

Paste into a new Claude Code session opened in `C:\src\aiu-eco` (the experiment worktree):

> Read `experiments/eco/README.md`, `plan.md` and `setup-new-pc.md`, and follow them as the
> operator of this experiment. The repository rules apply; the plan's owner decisions are
> recorded there and need no re-confirmation. Do the next action the README names. Run one
> experiment run at a time in the background and wait for its completion notice. Before each
> stage, check the remaining weekly usage with me against the stage's estimate. Commit results
> on `exp/eco-roles` and push `origin/exp/eco-roles` after each stage; nothing from this branch
> goes to `main`. Be candid (R-223): if the plan looks wrong in practice, stop and tell me with
> a better option.
