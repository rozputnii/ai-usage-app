<#
.SYNOPSIS
Token and cost metrics for Claude Code sessions, from their local transcripts.

.DESCRIPTION
Reads <ProjectsRoot>/<project>/<session>.jsonl (the primary) and
<ProjectsRoot>/<project>/<session>/subagents/agent-*.jsonl (its subagents). One API request is
written on several lines with the same requestId; the last line per request counts. Lines are
scanned with regular expressions instead of ConvertFrom-Json, because Windows PowerShell 5.1
cannot parse very long lines. Only counters and agent metadata are read, never content.

Costs are API-price equivalents from experiments/eco's price table (the eco skill's
model-costs.md); they are a proxy for the subscription limit, not a bill.

.EXAMPLE
./Measure-Run.ps1 -SessionId 1b7b65ec-cd62-45c8-95a9-e6c282fee2e2 -OutFile run.json

.EXAMPLE
./Measure-Run.ps1 -Baseline -ProjectFilter 'C--Users-*-ai-usage-app*' -OutFile baseline.json
#>
param(
    [string[]] $SessionId,
    [switch] $Baseline,
    # Follows CLAUDE_CONFIG_DIR, which keeps each subscription account's sessions in its own folder.
    [string] $ProjectsRoot = (Join-Path $(if ($env:CLAUDE_CONFIG_DIR) { $env:CLAUDE_CONFIG_DIR } else { Join-Path $HOME '.claude' }) 'projects'),
    [string] $ProjectFilter = '*',
    [string] $OutFile
)
$ErrorActionPreference = 'Stop'

# USD per million tokens: input, output. Cache write 5m = 1.25x input, 1h = 2x input, read = 0.1x input.
$Prices = @{ opus = @(4.0, 20.0); sonnet = @(2.0, 10.0); haiku = @(0.10, 0.50) }
$HaikuLongPrompt = @(0.50, 2.50)   # Haiku above 100k prompt tokens

$UsageMarker = '"usage":{"input_tokens"'
function Get-Int([string] $text, [string] $key) {
    $m = [regex]::Match($text, '"' + $key + '":(\d+)')
    if ($m.Success) { [long]$m.Groups[1].Value } else { 0 }
}
function Get-Family([string] $model) {
    foreach ($f in 'opus', 'sonnet', 'haiku', 'fable') { if ($model -like "*$f*") { return $f } }
    return 'other'
}

function Read-Requests([string] $file) {
    $byRequest = @{}
    foreach ($line in [System.IO.File]::ReadLines($file)) {
        $u = $line.LastIndexOf($UsageMarker)
        if ($u -lt 0) { continue }
        $msg = $line.IndexOf('"message":{')
        $model = if ($msg -ge 0) { [regex]::Match($line.Substring($msg), '"model":"([^"]+)"').Groups[1].Value } else { '' }
        if (-not $model -or $model -like '<*') { continue }
        $id = [regex]::Match($line, '"requestId":"([^"]+)"', 'RightToLeft').Groups[1].Value
        if (-not $id) { $id = [regex]::Match($line, '"id":"(msg_[^"]+)"').Groups[1].Value }
        $usage = $line.Substring($u)
        $byRequest[$id] = [pscustomobject]@{
            Model     = $model
            Family    = Get-Family $model
            Input     = Get-Int $usage 'input_tokens'
            Write     = Get-Int $usage 'cache_creation_input_tokens'
            Write1h   = Get-Int $usage 'ephemeral_1h_input_tokens'
            Read      = Get-Int $usage 'cache_read_input_tokens'
            Output    = Get-Int $usage 'output_tokens'
            Thinking  = Get-Int $usage 'thinking_tokens'
            Timestamp = [regex]::Match($line, '"timestamp":"([^"]+)"', 'RightToLeft').Groups[1].Value
        }
    }
    @($byRequest.Values | Sort-Object Timestamp)
}

function Measure-Requests($requests) {
    $r = [ordered]@{ calls = 0; costUsd = 0.0; cacheWriteUsd = 0.0; cacheReadUsd = 0.0; inputUsd = 0.0; outputUsd = 0.0
        unpricedCalls = 0; inputTokens = 0; cacheWriteTokens = 0; cacheReadTokens = 0; outputTokens = 0; thinkingTokens = 0
        maxContext = 0; firstPrompt = 0; start = $null; end = $null; models = @{} }
    foreach ($q in $requests) {
        $prompt = $q.Input + $q.Write + $q.Read
        if ($r.calls -eq 0) { $r.firstPrompt = $prompt; $r.start = $q.Timestamp }
        $r.end = $q.Timestamp
        $r.calls++
        $r.models[$q.Model] = 1 + [int]$r.models[$q.Model]
        $r.inputTokens += $q.Input; $r.cacheWriteTokens += $q.Write; $r.cacheReadTokens += $q.Read
        $r.outputTokens += $q.Output; $r.thinkingTokens += $q.Thinking
        if ($prompt -gt $r.maxContext) { $r.maxContext = $prompt }
        $p = $Prices[$q.Family]
        if (-not $p) { $r.unpricedCalls++; continue }
        if ($q.Family -eq 'haiku' -and $prompt -gt 100000) { $p = $HaikuLongPrompt }
        $in = $p[0] / 1e6
        $r.inputUsd += $q.Input * $in
        $r.cacheWriteUsd += (($q.Write - $q.Write1h) * 1.25 + $q.Write1h * 2) * $in
        $r.cacheReadUsd += $q.Read * 0.1 * $in
        $r.outputUsd += $q.Output * $p[1] / 1e6
    }
    $r.costUsd = $r.inputUsd + $r.cacheWriteUsd + $r.cacheReadUsd + $r.outputUsd
    $r
}

function Measure-Session([System.IO.FileInfo] $primary) {
    $id = $primary.BaseName
    $main = Measure-Requests (Read-Requests $primary.FullName)
    $subs = @()
    $subDir = Join-Path $primary.DirectoryName "$id\subagents"
    if (Test-Path $subDir) {
        foreach ($f in Get-ChildItem $subDir -Filter 'agent-*.jsonl') {
            $meta = Join-Path $subDir ($f.BaseName + '.meta.json')
            $type = if (Test-Path $meta) { (Get-Content $meta -Raw | ConvertFrom-Json).agentType } else { '?' }
            $m = Measure-Requests (Read-Requests $f.FullName)
            if ($m.calls -gt 0) { $m['agentType'] = $type; $subs += [pscustomobject]$m }
        }
    }
    $total = [ordered]@{}
    foreach ($k in 'costUsd', 'cacheWriteUsd', 'cacheReadUsd', 'inputUsd', 'outputUsd', 'calls', 'outputTokens', 'thinkingTokens', 'unpricedCalls') {
        $total[$k] = $main[$k] + (($subs | Measure-Object -Property $k -Sum).Sum)
    }
    $times = @($main.start, $main.end) + @($subs | ForEach-Object { $_.start; $_.end }) | Where-Object { $_ } | Sort-Object
    $total['wallMinutes'] = if ($times.Count -ge 2) { [math]::Round(([datetime]$times[-1] - [datetime]$times[0]).TotalMinutes, 1) } else { 0 }
    $total['subagents'] = $subs.Count
    $total['coldStartTokens'] = ($subs | Measure-Object -Property firstPrompt -Sum).Sum
    [pscustomobject]@{ sessionId = $id; total = [pscustomobject]$total; primary = [pscustomobject]$main; subagents = $subs }
}

function Get-Median($values) {
    $v = @($values | Sort-Object)
    if ($v.Count -eq 0) { return 0 }
    if ($v.Count % 2) { $v[[int][math]::Floor($v.Count / 2)] } else { ($v[$v.Count / 2 - 1] + $v[$v.Count / 2]) / 2 }
}

$projects = Get-ChildItem $ProjectsRoot -Directory -Filter $ProjectFilter
if ($Baseline) {
    $sessions = foreach ($p in $projects) { foreach ($f in Get-ChildItem $p.FullName -Filter '*.jsonl') { Measure-Session $f } }
    $sessions = @($sessions | Where-Object { $_.primary.calls -gt 0 })
    $subs = @($sessions | ForEach-Object { $_.subagents })
    $primaryCost = ($sessions | ForEach-Object { $_.primary.costUsd } | Measure-Object -Sum).Sum
    $subCost = ($subs | Measure-Object -Property costUsd -Sum).Sum
    function Share($items, $prop) { $t = ($items | Measure-Object -Property costUsd -Sum).Sum; if ($t) { [math]::Round(100 * ($items | Measure-Object -Property $prop -Sum).Sum / $t) } else { 0 } }
    $prims = @($sessions | ForEach-Object { $_.primary })
    $top3 = ($prims | Sort-Object costUsd -Descending | Select-Object -First 3 | Measure-Object -Property costUsd -Sum).Sum
    $result = [pscustomobject]@{
        sessions = $sessions.Count; subagents = $subs.Count
        primaryUsd = [math]::Round($primaryCost); subagentUsd = [math]::Round($subCost)
        primarySharePct = [ordered]@{ cacheWrite = Share $prims cacheWriteUsd; cacheRead = Share $prims cacheReadUsd; output = Share $prims outputUsd }
        subagentSharePct = [ordered]@{ cacheWrite = Share $subs cacheWriteUsd; cacheRead = Share $subs cacheReadUsd; output = Share $subs outputUsd }
        thinkingShareOfOutputPct = [ordered]@{
            primary = [math]::Round(100 * ($prims | Measure-Object thinkingTokens -Sum).Sum / [math]::Max(1, ($prims | Measure-Object outputTokens -Sum).Sum))
            subagents = [math]::Round(100 * ($subs | Measure-Object thinkingTokens -Sum).Sum / [math]::Max(1, ($subs | Measure-Object outputTokens -Sum).Sum)) }
        medianPrimaryUsd = [math]::Round((Get-Median ($prims | ForEach-Object costUsd)), 1)
        medianPrimaryCalls = Get-Median ($prims | ForEach-Object calls)
        top3PrimarySharePct = [math]::Round(100 * $top3 / [math]::Max(1, $primaryCost))
        top3 = @($prims | Sort-Object costUsd -Descending | Select-Object -First 3 | ForEach-Object { [pscustomobject]@{ usd = [math]::Round($_.costUsd); calls = $_.calls; maxContext = $_.maxContext } })
        byAgentType = @($subs | Group-Object agentType | Sort-Object Count -Descending | ForEach-Object {
            [pscustomobject]@{ agentType = $_.Name; count = $_.Count
                medianColdStart = Get-Median ($_.Group | ForEach-Object firstPrompt)
                meanUsd = [math]::Round(($_.Group | Measure-Object costUsd -Average).Average, 2)
                meanCalls = [math]::Round(($_.Group | Measure-Object calls -Average).Average) } })
    }
} else {
    if (-not $SessionId) { throw 'Pass -SessionId or -Baseline.' }
    $result = foreach ($id in $SessionId) {
        $file = $projects | ForEach-Object { Get-Item (Join-Path $_.FullName "$id.jsonl") -ErrorAction SilentlyContinue } | Select-Object -First 1
        if (-not $file) { throw "No transcript for session $id under $ProjectsRoot." }
        Measure-Session $file
    }
}
$json = $result | ConvertTo-Json -Depth 6
if ($OutFile) { [System.IO.File]::WriteAllText($OutFile, $json, (New-Object System.Text.UTF8Encoding $false)) } else { $json }
