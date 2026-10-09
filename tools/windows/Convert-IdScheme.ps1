# One-time migration to the OD-31 ID scheme (owner decision, 2026-10-09). Re-run it after merging fresh
# main into the migration branch so that items main added meanwhile are converted too; delete it once the
# migration is on main. Git history keeps the old prefixes.
#   AIU-nnn -> T-nnn and AIU-NEW -> T-NEW (work items, spec folders)
#   T-xx -> T-nnn.k (steps inside an item): the item is the last AIU-nnn named earlier on the same line,
#          otherwise the item that owns the spec folder; elsewhere a T-xx without such a line context stays
#          Limitation: this is a heuristic. On a line that names another item before a step of the folder's own
#          item ("AIU-043 tests ... | T-01 |"), the step gets the wrong item; the first run produced 16 such
#          references, corrected by hand. After every re-run, review each converted step whose item differs from
#          the folder's item against the original text (git diff), and fix wrong owners by hand.
#   D-nnn -> R-nnn and D-NEW -> R-NEW (decisions)
#   Markdown heading anchors (#aiu-nnn, #d-nnn, #...t-xx...) follow the same rules.
# Markdown is converted in full; C#, PowerShell and YAML only in comments, XAML and manifests only in <!-- -->.
# The archive, the Stage 1 audit inputs, real paths (.ai-usage-local, artifacts, spikes) and branch names keep
# their text.
[CmdletBinding()]
param([switch] $WhatIf)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Push-Location $root
try {
    # The first run (559b6bf) converted the Stage 2 records in docs/workflow/claude-workflow-audit/ by hand where needed;
    # re-runs skip that folder, which describes the old scheme on purpose.
    $skip = '^(docs/archive/|docs/workflow/claude-workflow-audit/|tools/windows/Convert-IdScheme\.ps1$)'
    $files = @(git ls-files --cached --others --exclude-standard) | Where-Object { $_ -match '\.(md|cs|ps1|psm1|yml|xaml|appxmanifest)$' -and $_ -notmatch $skip }
    $notPath = '(?<!\.ai-usage-local[/\\])(?<!artifacts/)(?<!spikes/windows/)(?<!feature/)'

    function Convert-Line([string] $line, [string] $item) {
        # Steps first: each T-xx takes the last item named before it on the line, or the folder's item.
        $script:context = $item
        $line = [regex]::Replace($line, "$notPath\bAIU-?(\d{3})(?=\b|-D)|(?<![\w.])T-(\d{2})(?!\d)|(?<=#[^\s)]*)\bt-(\d{2})(?!\d)", {
            param($m)
            if ($m.Groups[1].Success) { $script:context = $m.Groups[1].Value; return $m.Value }
            if (!$script:context) { return $m.Value }
            if ($m.Groups[2].Success) { return "T-$($script:context).$([int]$m.Groups[2].Value)" }
            return "t-$($script:context)$([int]$m.Groups[3].Value)"
        })
        $line = [regex]::Replace($line, "$notPath\bAIU-(\d{3}|NEW)\b", 'T-$1')
        $line = [regex]::Replace($line, "$notPath\bD-(\d{3}|NEW)\b", 'R-$1')
        $line = [regex]::Replace($line, '(?<=#[^\s)]*)\baiu-(\d{3})\b', 't-$1')
        $line = [regex]::Replace($line, '(?<=#[^\s)]*)\bd-(\d{3})\b', 'r-$1')
        return $line
    }
    function Convert-Text([string] $text, [string] $item) {
        (($text -split "`n") | ForEach-Object { Convert-Line $_ $item }) -join "`n"
    }

    $changed = 0
    foreach ($file in $files) {
        $bytes = [IO.File]::ReadAllBytes((Join-Path $root $file))
        $bom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
        $offset = if ($bom) { 3 } else { 0 }
        $original = [Text.UTF8Encoding]::new($false).GetString($bytes, $offset, $bytes.Length - $offset)
        # A folder this branch already renamed still owns the bare steps that a merge from main brings into it.
        $item = if ($file -match '^docs/specs/(?:AIU|T)-(\d{3})-') { $Matches[1] } else { '' }
        if ($file -match '\.md$') { $text = Convert-Text $original $item }
        elseif ($file -match '\.(xaml|appxmanifest)$') {
            $text = [regex]::Replace($original, '(?s)<!--.*?-->', { param($m) Convert-Text $m.Value '' })
        } else {
            # Comments only: C# from //, PowerShell and YAML from a # that starts the line or follows whitespace.
            $marker = if ($file -match '\.cs$') { '//' } else { '(?:^|(?<=\s))#' }
            $text = [regex]::Replace($original, "(?m)($marker)(.*)$", { param($m) $m.Groups[1].Value + (Convert-Line $m.Groups[2].Value '') })
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
