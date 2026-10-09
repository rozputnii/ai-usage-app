# One-time migration to the OD-31 ID scheme (owner decision, 2026-10-09). Re-run it after merging fresh
# main into the migration branch so that items main added meanwhile are converted too; delete it once the
# migration is on main. Git history keeps the old prefixes.
#   AIU-nnn -> T-nnn and AIU-NEW -> T-NEW (work items, spec folders)
#   T-xx inside docs/specs/<item>/ -> T-nnn.k (steps inside an item)
#   D-nnn -> R-nnn and D-NEW -> R-NEW (decisions)
# Markdown is converted in full; C#, PowerShell and YAML only in comments. Historical audit inputs, the
# archive and paths into the ignored .ai-usage-local and artifacts roots keep their text.
[CmdletBinding()]
param([switch] $WhatIf)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Push-Location $root
try {
    $skip = '^(docs/archive/|docs/workflow/claude-workflow-audit/(report\.md|lanes/|stage-[12]-prompt\.md|prompt-audit\.md)|tools/windows/Convert-IdScheme\.ps1$)'
    $files = @(git ls-files --cached --others --exclude-standard) | Where-Object { $_ -match '\.(md|cs|ps1|psm1|yml)$' -and $_ -notmatch $skip }
    $notPath = '(?<!\.ai-usage-local/)(?<!artifacts/)'
    function Convert-Ids([string] $text, [string] $item) {
        $text = [regex]::Replace($text, "$notPath\bAIU-(\d{3}|NEW)\b", 'T-$1')
        $text = [regex]::Replace($text, "$notPath\bD-(\d{3}|NEW)\b", 'R-$1')
        if ($item) { $text = [regex]::Replace($text, '\bT-(\d{2})\b', { param($m) "T-$item.$([int]$m.Groups[1].Value)" }) }
        return $text
    }
    $changed = 0
    foreach ($file in $files) {
        $bytes = [IO.File]::ReadAllBytes((Join-Path $root $file))
        $bom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
        $original = [Text.UTF8Encoding]::new($false).GetString($bytes, $(if ($bom) { 3 } else { 0 }), $bytes.Length - $(if ($bom) { 3 } else { 0 }))
        $item = if ($file -match '^docs/specs/AIU-(\d{3})-') { $Matches[1] } else { '' }
        if ($file -match '\.md$') { $text = Convert-Ids $original $item }
        else {
            # Comments only: C# from //, PowerShell and YAML from a # that starts the line or follows whitespace.
            $marker = if ($file -match '\.cs$') { '//' } else { '(?:^|(?<=\s))#' }
            $text = [regex]::Replace($original, "(?m)($marker)(.*)$", { param($m) $m.Groups[1].Value + (Convert-Ids $m.Groups[2].Value '') })
        }
        if ($text -cne $original) {
            $changed++
            Write-Output "converted $file"
            if (!$WhatIf) {
                $out = [Text.UTF8Encoding]::new($bom).GetPreamble() + [Text.UTF8Encoding]::new($false).GetBytes($text)
                [IO.File]::WriteAllBytes((Join-Path $root $file), [byte[]]$out)
            }
        }
    }
    foreach ($folder in @(Get-ChildItem -Directory (Join-Path $root 'docs/specs') | Where-Object { $_.Name -match '^AIU-(\d{3}|NEW(-\d+)?)-' })) {
        $target = 'T-' + $folder.Name.Substring(4)
        Write-Output "rename docs/specs/$($folder.Name) -> docs/specs/$target"
        if (!$WhatIf) { git mv "docs/specs/$($folder.Name)" "docs/specs/$target"; if ($LASTEXITCODE -ne 0) { throw "git mv failed for $($folder.Name)." } }
    }
    Write-Output "$changed files converted."
} finally { Pop-Location }
