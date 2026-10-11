<#
.SYNOPSIS
Runs a batch of Start-Run.ps1 runs one at a time, in random order, under a dollar budget.

.DESCRIPTION
-Plan is a JSON array of runs; each item holds Start-Run.ps1 parameters (Base, Brief, Arm, and
optionally Task, Model, Effort, AgentsFile, AppendPromptFile, TimeoutMinutes, Vars). Paths are
relative to this folder. Before each run the batch stops when the API-equivalent spend of its
finished runs has reached -BudgetUsd; the runs not started are listed in the log so the next
batch can pick them up. The log is <RunRoot>\batches\<plan name>-<time>.log.

.EXAMPLE
./Start-Batch.ps1 -Plan C:\aiu-exp\plans\s2-runner.json -BudgetUsd 20
#>
param(
    [Parameter(Mandatory)] [string] $Plan,
    [double] $BudgetUsd = 20,
    [string] $Repo = 'D:\projects\ai-usage-app',
    [string] $RunRoot = 'C:\aiu-exp',
    [switch] $InOrder
)
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $here
# Windows PowerShell 5.1 emits a JSON array as one object; unroll it.
$items = @(Get-Content $Plan -Raw | ConvertFrom-Json | ForEach-Object { $_ })
if (-not $InOrder) { $items = @($items | Sort-Object { Get-Random }) }
New-Item -ItemType Directory -Force (Join-Path $RunRoot 'batches') | Out-Null
$log = Join-Path $RunRoot ("batches\{0}-{1}.log" -f [IO.Path]::GetFileNameWithoutExtension($Plan), (Get-Date -Format 'MMdd-HHmm'))
function Write-Log([string] $text) { Add-Content $log $text; $text }

$spent = 0.0
for ($i = 0; $i -lt $items.Count; $i++) {
    $item = $items[$i]
    if ($spent -ge $BudgetUsd) {
        Write-Log "BUDGET reached at `$$([math]::Round($spent, 2)); not started:"
        $items[$i..($items.Count - 1)] | ForEach-Object { Write-Log ('  ' + ($_ | ConvertTo-Json -Compress)) }
        break
    }
    $params = @{ Repo = $Repo; RunRoot = $RunRoot }
    foreach ($p in $item.PSObject.Properties) {
        if ($p.Name -eq 'Vars') {
            $vars = @{}; foreach ($v in $p.Value.PSObject.Properties) { $vars[$v.Name] = [string]$v.Value }
            $params.Vars = $vars
        } else { $params[$p.Name] = $p.Value }
    }
    try {
        $line = & (Join-Path $here 'Start-Run.ps1') @params | Select-Object -Last 1
        $cost = [double](Get-Content (Join-Path $RunRoot 'runs.csv') | ConvertFrom-Csv | Select-Object -Last 1).costUsd
        $spent += $cost
        Write-Log "$line | $($item | ConvertTo-Json -Compress)"
    } catch {
        Write-Log "FAILED $($_.Exception.Message) | $($item | ConvertTo-Json -Compress)"
    }
}
Write-Log "DONE spent=`$$([math]::Round($spent, 2)) runs=$i of $($items.Count)"
