<#
.SYNOPSIS
Blind pairwise judgment of two runs of the same task (judge.md).

.DESCRIPTION
Shows the two run clones to an opus high judge as X and Y in random order, with their final
run reports and the task brief. The mapping X/Y -> run IDs is written only to the local
judgment folder after the judge has finished. The judge may build and run checks in the
clones but must not change tracked files.

.EXAMPLE
./Start-Judge.ps1 -RunA r1a2b3c -RunB r4d5e6f -Brief ..\tasks\real-omp-review.md
#>
param(
    [Parameter(Mandatory)] [string] $RunA,
    [Parameter(Mandatory)] [string] $RunB,
    [Parameter(Mandatory)] [string] $Brief,
    [string] $DefectKey,
    [string] $RunRoot = 'C:\aiu-exp',
    [string] $Effort = 'high'
)
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$flip = (Get-Random -Maximum 2) -eq 1
$x, $y = if ($flip) { $RunB, $RunA } else { $RunA, $RunB }
function Get-Report([string] $run) {
    $raw = Get-Content (Join-Path $RunRoot "results\$run\claude.json") -Raw
    try { ($raw | ConvertFrom-Json).result } catch { $raw }
}
# PowerShell names are case-insensitive: $runA would overwrite the [string] $RunA parameter and
# leave .base and .account empty (every judge before S4b got a blank base; the guards never ran).
$infoA = Get-Content (Join-Path $RunRoot "results\$RunA\run.json") -Raw | ConvertFrom-Json
$infoB = Get-Content (Join-Path $RunRoot "results\$RunB\run.json") -Raw | ConvertFrom-Json
$base = $infoA.base
if ($infoB.base -ne $base) { throw "The runs start from different bases ($base, $($infoB.base)); they are not a pair." }
if ($infoA.account -ne $infoB.account) {
    Write-Warning "The runs used different accounts ($($infoA.account), $($infoB.account)); connectors and cache differ, so the pair's cost comparison is invalid (plan.md, Accounts)."
}
$id = 'j' + ([guid]::NewGuid().ToString('N').Substring(0, 6))
$dir = Join-Path $RunRoot "judgments\$id"
New-Item -ItemType Directory -Force $dir | Out-Null

# A "<file>#<anchor>" spec gives only that "## <title> {#anchor}" section, operator notes included
# (the judge needs them: defect key, held-out tests). The rest of the file names arms and other
# tasks, which would unblind the judge.
function Get-Section([string] $spec) {
    $path, $name = $spec -split '#', 2
    $text = [IO.File]::ReadAllText((Resolve-Path $path))
    if (-not $name) { return $text }
    $m = [regex]::Match($text, "(?ms)^## [^\n]*\{#$([regex]::Escape($name))\}.*?(?=^## |\z)")
    if (-not $m.Success) { throw "No section {#$name} in $path" }
    $m.Value
}
$anchor = ($Brief -split '#', 2)[1]
$briefText = Get-Section $Brief
$prompt = [IO.File]::ReadAllText((Join-Path (Split-Path -Parent $here) 'judge.md')) + @"


# Inputs

- Base commit: $base
- Solution X: clone ``$(Join-Path $RunRoot $x)``, branch ``run``
- Solution Y: clone ``$(Join-Path $RunRoot $y)``, branch ``run``
- Brief section: $(if ($anchor) { "{#$anchor}" } else { 'whole file' })

## Task brief

$briefText

## Run report X

$(Get-Report $x)

## Run report Y

$(Get-Report $y)
"@
if ($DefectKey) { $prompt += "`n`n## Defect key`n`n" + (Get-Section $DefectKey) }
$promptFile = Join-Path $dir 'prompt.md'
[IO.File]::WriteAllText($promptFile, $prompt, (New-Object Text.UTF8Encoding $false))

$sessionId = [guid]::NewGuid().ToString()
$env:DISABLE_AUTOUPDATER = '1'
$p = Start-Process -FilePath (Get-Command claude).Source -ArgumentList @('-p', '--model', 'opus', '--effort', $Effort,
    '--session-id', $sessionId, '--permission-mode', 'bypassPermissions', '--output-format', 'json') `
    -WorkingDirectory $dir -NoNewWindow -PassThru -RedirectStandardInput $promptFile `
    -RedirectStandardOutput (Join-Path $dir 'claude.json') -RedirectStandardError (Join-Path $dir 'stderr.txt')
$null = $p.Handle   # Windows PowerShell 5.1 reports no ExitCode unless the handle is opened early.
$p.WaitForExit()
foreach ($run in $x, $y) {
    $dirty = & git -C (Join-Path $RunRoot $run) status --porcelain --untracked-files=no
    if ($dirty) { Write-Warning "The judge changed tracked files in $run; the judgment is invalid." }
}
& (Join-Path $here 'Measure-Run.ps1') -SessionId $sessionId -OutFile (Join-Path $dir 'metrics.json')
[ordered]@{ judgment = $id; X = $x; Y = $y; sessionId = $sessionId; account = $env:AIU_EXP_ACCOUNT } | ConvertTo-Json | Set-Content (Join-Path $dir 'mapping.json')
"$id X=$x Y=$y -> $dir"
