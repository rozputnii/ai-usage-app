# Assigns real numbers to T-NEW / R-NEW placeholders right before a merge into main (R-196, R13).
# Run it on the task branch after merging fresh origin/main. It finds the highest T-nnn and R-nnn on the
# branch, numbers the placeholders in suffix order (T-NEW or T-NEW-1 first, then T-NEW-2, ...), replaces
# them outside code spans and fences in the authored Markdown, renames docs/specs/T-NEW*-<slug> folders,
# and runs the document validation with --final. Review the diff, then commit and push at once.
[CmdletBinding()]
param([string] $Root = (Join-Path $PSScriptRoot '../..'), [switch] $SkipFinalCheck)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$Root = (Resolve-Path $Root).Path
Push-Location $Root
try {
    git merge-base --is-ancestor origin/main HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Merge fresh origin/main into this branch first.' }
    $files = @(git ls-files --cached --others --exclude-standard) | Where-Object {
        $_ -match '\.md$' -and ($_ -match '^(docs|\.agents)/' -or $_ -notmatch '/') -and $_ -notmatch '^docs/archive/' }

    # Highest numbers in use: backlog headings and Done index rows, spec folders, decision headings.
    $maxT = 0; $maxR = 0
    foreach ($file in $files) {
        $text = [IO.File]::ReadAllText((Join-Path $Root $file))
        foreach ($m in [regex]::Matches($text, '(?m)^(?:## |\| *)T-(\d{3})\b')) { $maxT = [Math]::Max($maxT, [int]$m.Groups[1].Value) }
        foreach ($m in [regex]::Matches($text, '(?m)^### R-(\d{3}) - ')) { $maxR = [Math]::Max($maxR, [int]$m.Groups[1].Value) }
    }
    foreach ($dir in @(Get-ChildItem -Directory (Join-Path $Root 'docs/specs') -ErrorAction SilentlyContinue)) {
        if ($dir.Name -match '^T-(\d{3})-') { $maxT = [Math]::Max($maxT, [int]$Matches[1]) }
    }

    # Placeholders outside code, numbered by suffix; T-NEW and T-NEW-1 are the same first placeholder.
    $outsideCode = { param([string] $text) [regex]::Replace([regex]::Replace($text, '(?ms)^```.*?^```[ \t]*$', ''), '`[^`\n]*`', '') }
    $found = @{}
    foreach ($file in $files) {
        foreach ($m in [regex]::Matches((& $outsideCode ([IO.File]::ReadAllText((Join-Path $Root $file)))), '\b([TR])-NEW(?:-(\d+))?\b')) {
            $key = '{0}-NEW-{1}' -f $m.Groups[1].Value, $(if ($m.Groups[2].Success) { [int]$m.Groups[2].Value } else { 1 })
            $found[$key] = $true
        }
    }
    $map = [ordered]@{}
    foreach ($prefix in 'T', 'R') {
        $next = if ($prefix -eq 'T') { $maxT } else { $maxR }
        foreach ($key in @($found.Keys | Where-Object { $_.StartsWith("$prefix-") } | Sort-Object { [int]($_ -split '-')[-1] })) {
            $next++
            $map[$key] = '{0}-{1:000}' -f $prefix, $next
        }
    }
    if ($map.Count -eq 0) { Write-Output 'No placeholders found.' }
    foreach ($key in $map.Keys) { Write-Output "$key -> $($map[$key])" }

    # Replace outside fences and inline code only, so that rule text such as `T-NEW` survives.
    $replace = {
        param([string] $segment)
        [regex]::Replace($segment, '\b([TR])-NEW(?:-(\d+))?\b', {
            param($m)
            $key = '{0}-NEW-{1}' -f $m.Groups[1].Value, $(if ($m.Groups[2].Success) { [int]$m.Groups[2].Value } else { 1 })
            $map[$key]
        })
    }
    foreach ($file in $files) {
        $path = Join-Path $Root $file
        $original = [IO.File]::ReadAllText($path)
        $fence = $false
        $lines = foreach ($line in ($original -split "`n")) {
            if ($line -match '^```') { $fence = !$fence; $line; continue }
            if ($fence) { $line; continue }
            $parts = $line -split '(`[^`]*`)'
            ($parts | ForEach-Object { if ($_.Length -ge 2 -and $_.StartsWith('`') -and $_.EndsWith('`')) { $_ } else { & $replace $_ } }) -join ''
        }
        $text = $lines -join "`n"
        if ($text -cne $original) { [IO.File]::WriteAllText($path, $text, [Text.UTF8Encoding]::new($false)); Write-Output "updated $file" }
    }
    foreach ($dir in @(Get-ChildItem -Directory (Join-Path $Root 'docs/specs') -ErrorAction SilentlyContinue)) {
        if ($dir.Name -match '^T-NEW(?:-(\d+))?-(.+)$') {
            $key = 'T-NEW-{0}' -f $(if ($Matches[1]) { [int]$Matches[1] } else { 1 })
            $target = "$($map[$key])-$($Matches[2])"
            git mv "docs/specs/$($dir.Name)" "docs/specs/$target"
            if ($LASTEXITCODE -ne 0) { throw "git mv failed for $($dir.Name)." }
            Write-Output "renamed docs/specs/$($dir.Name) -> docs/specs/$target"
        }
    }
    if (!$SkipFinalCheck) {
        dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json --final
        if ($LASTEXITCODE -ne 0) { throw 'Final document validation failed; inspect the diagnostics above.' }
    }
} finally { Pop-Location }
