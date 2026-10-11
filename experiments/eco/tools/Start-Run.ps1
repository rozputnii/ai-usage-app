<#
.SYNOPSIS
One unattended experiment run: an isolated clone at a base commit, one arm, one task brief.

.DESCRIPTION
1. Clones -Repo into <RunRoot>\<runId>, checks out -Base as branch "run", deletes every other
   ref and the remote, and prunes unreachable objects, so the run cannot push or see later
   commits.
2. Builds the prompt from arms/run-preamble.md and the brief and pipes it to `claude -p` with
   the arm's model, effort, prompt addition and agents, under bypassPermissions (owner decision
   2026-10-11: allowed only in such clones).
3. Writes <RunRoot>\results\<runId>\: prompt.md, claude.json (the CLI result), metrics.json
   (Measure-Run.ps1), commits.txt, diff.patch, run.json; appends a row to <RunRoot>\runs.csv.
   The arm is recorded only in run.json and runs.csv, never in the clone, so a judge given
   clone paths stays blind.

.EXAMPLE
./Start-Run.ps1 -Repo C:\src\ai-usage-app -Base 3564996 -Brief ..\tasks\replays.md#rp2 -Arm C

.EXAMPLE
./Start-Run.ps1 -Repo C:\src\ai-usage-app -Base 1a8526e -Brief ..\tasks\micro-exploration.md#probe -Arm A `
    -Model haiku -Effort low -AgentsFile ..\arms\trials-scouts.json -Task explore-X1 `
    -Vars @{ VARIANT = 'scout-lsp'; QUESTION = 'Which code decides where a dragged card is inserted?' }
#>
param(
    [Parameter(Mandatory)] [string] $Repo,
    [Parameter(Mandatory)] [string] $Base,
    [Parameter(Mandatory)] [string] $Brief,
    [Parameter(Mandatory)] [ValidateSet('A', 'B', 'C', 'D', 'E')] [string] $Arm,
    [string] $Task = [IO.Path]::GetFileNameWithoutExtension(($Brief -split '#')[0]),
    [string] $Model = 'opus',
    [string] $Effort,
    [string] $AppendPromptFile,
    [string] $AgentsFile,
    [string] $RunRoot = 'C:\aiu-exp',
    # A short label of the subscription account the run uses (for example main or second; never
    # an email). Runs compared with each other must use the same account (plan.md, Accounts).
    [string] $Account = $env:AIU_EXP_ACCOUNT,
    [hashtable] $Vars = @{},
    [string[]] $Appendix = @(),
    [int] $TimeoutMinutes = 360,
    [switch] $NoRun
)
$ErrorActionPreference = 'Stop'
if (-not $Account) { throw 'Set -Account or AIU_EXP_ACCOUNT to the account label (for example main or second).' }
if ($Account -match '@') { throw 'The account label must not be an email address.' }
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$arms = Join-Path (Split-Path -Parent $here) 'arms'

# Arm defaults (plan.md, Arms). E needs explicit -Effort, -AppendPromptFile and optional -AgentsFile.
switch ($Arm) {
    'A' { if (-not $Effort) { $Effort = 'high' } }
    'B' { if (-not $Effort) { $Effort = 'high' }; if (-not $AppendPromptFile) { $AppendPromptFile = Join-Path $arms 'B-eco-current.md' } }
    'C' { if (-not $Effort) { $Effort = 'medium' }; if (-not $AppendPromptFile) { $AppendPromptFile = Join-Path $arms 'C-eco-v2.md' } }
    'D' { if (-not $Effort) { $Effort = 'medium' }; if (-not $AppendPromptFile) { $AppendPromptFile = Join-Path $arms 'C-eco-v2.md' }; if (-not $AgentsFile) { $AgentsFile = Join-Path $arms 'D-agents.json' } }
    'E' { if (-not $Effort -or -not $AppendPromptFile) { throw 'Arm E needs -Effort and -AppendPromptFile (the frozen S3 winner).' } }
}

# No named parameters: git flags such as -d would bind to them. No 2>&1: in Windows PowerShell 5.1
# a redirected native stderr line throws under $ErrorActionPreference = 'Stop'.
function Invoke-Git {
    $dir, $rest = $args
    $ErrorActionPreference = 'Continue'
    $out = & git -C $dir @rest
    if ($LASTEXITCODE) { throw "git $($rest -join ' ') failed with exit code $LASTEXITCODE" }
    $out
}

$runId = 'r' + ([guid]::NewGuid().ToString('N').Substring(0, 6))
$clone = Join-Path $RunRoot $runId
$results = Join-Path $RunRoot "results\$runId"
New-Item -ItemType Directory -Force $results | Out-Null

# 1. Isolated clone.
& { $ErrorActionPreference = 'Continue'; & git clone --no-local --quiet $Repo $clone }
if ($LASTEXITCODE) { throw "git clone failed for $Repo" }
$baseSha = (Invoke-Git $clone rev-parse --verify "$Base^{commit}").Trim()
Invoke-Git $clone checkout --quiet -B run $baseSha | Out-Null
foreach ($ref in (Invoke-Git $clone for-each-ref --format='%(refname)')) {
    if ($ref -ne 'refs/heads/run') { Invoke-Git $clone update-ref -d $ref | Out-Null }
}
Invoke-Git $clone remote remove origin | Out-Null
Invoke-Git $clone reflog expire --expire=now --all | Out-Null
Invoke-Git $clone gc --prune=now --quiet | Out-Null
$visible = [int](Invoke-Git $clone rev-list --all --count)
$expected = [int](Invoke-Git $clone rev-list --count run)
if ($visible -ne $expected) { throw "Isolation failed: $visible commits visible, $expected expected." }

# 2. Prompt.
$briefPath, $anchor = $Brief -split '#', 2
$briefText = [IO.File]::ReadAllText((Resolve-Path $briefPath))
if ($anchor) {
    # A brief file may hold several briefs as "## <title> {#anchor}" sections; take the named one.
    $m = [regex]::Match($briefText, "(?ms)^## [^\n]*\{#$([regex]::Escape($anchor))\}.*?(?=^## |\z)")
    if (-not $m.Success) { throw "No section {#$anchor} in $briefPath" }
    $briefText = $m.Value
    # Operator notes (bases, defect keys, held-out tests) must not reach the run: when a section
    # has a blockquote, the blockquote alone is the brief.
    $quoted = @($briefText -split "`r?`n" | Where-Object { $_ -match '^>( |$)' } | ForEach-Object { $_ -replace '^> ?', '' })
    if ($quoted.Count) { $briefText = $quoted -join "`n" }
}
# Probe briefs carry <NAME> placeholders; -Vars @{ NAME = 'value' } fills them.
foreach ($k in $Vars.Keys) { $briefText = $briefText.Replace("<$k>", [string]$Vars[$k]) }
if ($briefText -cmatch '<[A-Z_]+>') { throw "Unfilled placeholder $($Matches[0]) in the brief; pass -Vars." }
# Appendices (for example a replay's original specification) follow the brief verbatim.
foreach ($a in $Appendix) { $briefText += "`n`n" + [IO.File]::ReadAllText((Resolve-Path $a)) }
$prompt = [IO.File]::ReadAllText((Join-Path $arms 'run-preamble.md')) + "`n`n# Task`n`n" + $briefText
$promptFile = Join-Path $results 'prompt.md'
[IO.File]::WriteAllText($promptFile, $prompt, (New-Object Text.UTF8Encoding $false))

$sessionId = [guid]::NewGuid().ToString()
$cliArgs = @('-p', '--model', $Model, '--effort', $Effort, '--session-id', $sessionId,
    '--permission-mode', 'bypassPermissions', '--output-format', 'json')
if ($AppendPromptFile) { $cliArgs += @('--append-system-prompt-file', (Resolve-Path $AppendPromptFile).Path) }
if ($AgentsFile) { $cliArgs += @('--agents', (Resolve-Path $AgentsFile).Path) }

$run = [ordered]@{ runId = $runId; task = $Task; brief = $Brief
    vars = (($Vars.Keys | Sort-Object | ForEach-Object { "$_=$($Vars[$_])" }) -join '; '); account = $Account; arm = $Arm; model = $Model; effort = $Effort
    base = $baseSha; sessionId = $sessionId; clone = $clone; appendPrompt = $AppendPromptFile; agents = $AgentsFile
    cli = (& claude --version | Out-String).Trim(); started = (Get-Date).ToString('o') }
if ($NoRun) { $run | ConvertTo-Json; return }

# 3. Run.
$env:DISABLE_AUTOUPDATER = '1'
$claude = (Get-Command claude).Source
$watch = [Diagnostics.Stopwatch]::StartNew()
$p = Start-Process -FilePath $claude -ArgumentList $cliArgs -WorkingDirectory $clone -NoNewWindow -PassThru `
    -RedirectStandardInput $promptFile -RedirectStandardOutput (Join-Path $results 'claude.json') `
    -RedirectStandardError (Join-Path $results 'stderr.txt')
$null = $p.Handle   # Windows PowerShell 5.1 reports no ExitCode unless the handle is opened early.
$finished = $p.WaitForExit($TimeoutMinutes * 60000)
if (-not $finished) { Stop-Process -Id $p.Id -Force; $run.timeout = $true }
$watch.Stop()
$run.exitCode = if ($finished) { $p.ExitCode } else { -1 }
$run.wallMinutes = [math]::Round($watch.Elapsed.TotalMinutes, 1)

# 4. Evidence.
& (Join-Path $here 'Measure-Run.ps1') -SessionId $sessionId -OutFile (Join-Path $results 'metrics.json')
$utf8 = New-Object Text.UTF8Encoding $false
[IO.File]::WriteAllLines((Join-Path $results 'commits.txt'), [string[]]@(Invoke-Git $clone log --format='%h %s' "$baseSha..HEAD"), $utf8)
[IO.File]::WriteAllLines((Join-Path $results 'diff.patch'), [string[]]@(Invoke-Git $clone diff $baseSha HEAD), $utf8)
$run.uncommitted = @(Invoke-Git $clone status --porcelain).Count
$metrics = Get-Content (Join-Path $results 'metrics.json') -Raw | ConvertFrom-Json
$run.costUsd = [math]::Round($metrics.total.costUsd, 2)
$run.calls = $metrics.total.calls
$run.subagents = $metrics.total.subagents
$run.commits = @(Get-Content (Join-Path $results 'commits.txt') | Where-Object { $_ }).Count
$run | ConvertTo-Json | Set-Content (Join-Path $results 'run.json')
$csv = Join-Path $RunRoot 'runs.csv'
[pscustomobject]$run | Select-Object runId, started, task, vars, account, arm, model, effort, base, sessionId, exitCode, wallMinutes, costUsd, calls, subagents, commits, uncommitted |
    Export-Csv $csv -Append -NoTypeInformation
"$runId $Task arm=$Arm exit=$($run.exitCode) wall=$($run.wallMinutes)m cost=`$$($run.costUsd) calls=$($run.calls) subagents=$($run.subagents)"
