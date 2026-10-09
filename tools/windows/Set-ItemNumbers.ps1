# Assigns real numbers to T-NEW / R-NEW placeholders right before a merge into main (R-196, R13).
# Run it on the task branch after merging fresh origin/main; it fetches first and refuses to run if
# origin/main is not merged. It finds the highest T-nnn and R-nnn on the branch, numbers the placeholders
# in suffix order (T-NEW or T-NEW-1 first, then T-NEW-2, ...), replaces them outside code spans and fences
# in the authored Markdown, renames docs/specs/T-NEW*-<slug> folders, and runs the document validation
# with --final. Review the diff, then commit and push at once.
[CmdletBinding()]
param([string] $Root = (Join-Path $PSScriptRoot '../..'), [switch] $SkipFinalCheck, [switch] $NoFetch)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$Root = (Resolve-Path $Root).Path
Push-Location $Root
try {
    if (!$NoFetch) { git fetch -q origin; if ($LASTEXITCODE -ne 0) { throw 'git fetch origin failed.' } }
    git merge-base --is-ancestor origin/main HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Merge fresh origin/main into this branch first.' }
    $files = @(git ls-files --cached --others --exclude-standard) | Where-Object {
        $_ -match '\.md$' -and ($_ -match '^(docs|\.agents)/' -or $_ -notmatch '/') -and $_ -notmatch '^docs/archive/' }
    $placeholder = '\b([TR])-NEW(?:-(\d+))?\b'
    function Get-Key($m) { '{0}-NEW-{1}' -f $m.Groups[1].Value, $(if ($m.Groups[2].Success) { [int]$m.Groups[2].Value } else { 1 }) }

    # Applies $action to every text segment outside fenced blocks and inline code; one routine for
    # finding and for replacing, so both always agree. Line endings, including CRLF, are kept.
    function Edit-OutsideCode([string] $text, [scriptblock] $action) {
        $fence = $false
        $lines = foreach ($line in ($text -split "`n")) {
            if ($line -match '^\s*```') { $fence = !$fence; $line; continue }
            if ($fence) { $line; continue }
            (($line -split '(`[^`]*`)') | ForEach-Object {
                if ($_.Length -ge 2 -and $_.StartsWith('`') -and $_.EndsWith('`')) { $_ } else { & $action $_ }
            }) -join ''
        }
        $lines -join "`n"
    }

    # Highest numbers in use: backlog headings and table rows, spec folders, decision headings.
    $maxT = 0; $maxR = 0
    $found = @{}
    foreach ($file in $files) {
        $text = [IO.File]::ReadAllText((Join-Path $Root $file))
        foreach ($m in [regex]::Matches($text, '(?m)^(?:## |\| *)T-(\d{3})\b')) { $maxT = [Math]::Max($maxT, [int]$m.Groups[1].Value) }
        foreach ($m in [regex]::Matches($text, '(?m)^### R-(\d{3}) - ')) { $maxR = [Math]::Max($maxR, [int]$m.Groups[1].Value) }
        $null = Edit-OutsideCode $text { param($segment) foreach ($m in [regex]::Matches($segment, $placeholder)) { $found[(Get-Key $m)] = $true }; $segment }
    }
    $folders = @(Get-ChildItem -Directory (Join-Path $Root 'docs/specs') -ErrorAction SilentlyContinue)
    foreach ($dir in $folders) { if ($dir.Name -match '^T-(\d{3})-') { $maxT = [Math]::Max($maxT, [int]$Matches[1]) } }

    $map = @{}
    foreach ($prefix in 'T', 'R') {
        $next = if ($prefix -eq 'T') { $maxT } else { $maxR }
        foreach ($key in @($found.Keys | Where-Object { $_.StartsWith("$prefix-") } | Sort-Object { [int]($_ -split '-')[-1] })) {
            $next++
            $map[$key] = '{0}-{1:000}' -f $prefix, $next
            Write-Output "$key -> $($map[$key])"
        }
    }
    if ($map.Count -eq 0) { Write-Output 'No placeholders found.' }
    $renames = foreach ($dir in $folders) {
        if ($dir.Name -match '^T-NEW(?:-(\d+))?-(.+)$') {
            $key = 'T-NEW-{0}' -f $(if ($Matches[1]) { [int]$Matches[1] } else { 1 })
            if (!$map.ContainsKey($key)) { throw "Folder $($dir.Name) has no matching placeholder in the authored documents." }
            [pscustomobject]@{ From = $dir.Name; To = "$($map[$key])-$($Matches[2])" }
        }
    }

    foreach ($file in $files) {
        $path = Join-Path $Root $file
        $original = [IO.File]::ReadAllText($path)
        $text = Edit-OutsideCode $original {
            param($segment)
            [regex]::Replace($segment, $placeholder, { param($m) $key = Get-Key $m; if ($map.ContainsKey($key)) { $map[$key] } else { $m.Value } })
        }
        if ($text -cne $original) { [IO.File]::WriteAllText($path, $text, [Text.UTF8Encoding]::new($false)); Write-Output "updated $file" }
    }
    foreach ($rename in $renames) {
        $from = "docs/specs/$($rename.From)"; $to = "docs/specs/$($rename.To)"
        if (@(git ls-files -- $from).Count) { git mv $from $to; if ($LASTEXITCODE -ne 0) { throw "git mv failed for $from." } }
        else { Move-Item -LiteralPath (Join-Path $Root $from) -Destination (Join-Path $Root $to) }
        Write-Output "renamed $from -> $to"
    }
    if (!$SkipFinalCheck) {
        dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json --final
        if ($LASTEXITCODE -ne 0) { throw 'Final document validation failed; inspect the diagnostics above.' }
    }
} finally { Pop-Location }
