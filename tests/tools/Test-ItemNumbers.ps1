# Regression check for tools/windows/Set-ItemNumbers.ps1 (R13). Builds a throwaway Git repository under
# %TEMP%, runs the script without fetching or the final validation, and checks the numbering.
# Windows PowerShell 5.1.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$script:count = 0
function Equal($actual, $expected) { if ($actual -cne $expected) { throw "Expected '$expected', got '$actual'." }; $script:count++ }
$tool = Join-Path $PSScriptRoot '../../tools/windows/Set-ItemNumbers.ps1'
$repo = Join-Path ([IO.Path]::GetTempPath()) ('aiu-numbers-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $repo | Out-Null
function Put([string] $file, [string] $text) {
    $path = Join-Path $repo $file
    New-Item -ItemType Directory -Force -Path (Split-Path $path) | Out-Null
    [IO.File]::WriteAllText($path, $text, [Text.UTF8Encoding]::new($false))
}
function Read([string] $file) { [IO.File]::ReadAllText((Join-Path $repo $file)) }
try {
    Push-Location $repo
    git init -q -b main
    git config user.email test@example.invalid
    git config user.name test
    # The highest item number, 012, is only a spec folder; T-NEW-1 is the same placeholder as T-NEW.
    Put 'docs/backlog.md' "## T-007 - Old`n- status: done`n`n## T-NEW - New item`n- depends_on: [T-007]`n`n## T-NEW-2 - Second`n- depends_on: [T-NEW-1]`n`n| T-009 | Indexed | done | G-001 | x |`n"
    Put 'docs/specs/T-012-numbered/spec.md' "---`nid: T-012`n---`n"
    Put 'docs/decisions/accepted.md' ("### R-207 - Old`r`nAmended by R-NEW (2026-10-09): x.`r`n`r`n### R-NEW - New`r`nUse ``R-NEW`` placeholders.`r`n`r`n``````text`r`n### R-NEW - fenced`r`n```````r`n" +
        "### R-NEW-9 - Ninth`r`n### R-NEW-10 - Tenth`r`n### R-NEW-2 - Second`r`n")
    Put 'docs/specs/T-NEW-new-item/spec.md' "---`nid: T-NEW`n---`n# New`nSee T-NEW.3 and ``T-NEW``.`n"
    Put 'CONTRIBUTING.md' "Use ``T-NEW`` and ``R-NEW`` until the merge.`n"
    git add -A; git commit -q -m base
    git update-ref refs/remotes/origin/main HEAD
    Pop-Location
    & $tool -Root $repo -SkipFinalCheck -NoFetch | Out-Null
    $backlog = Read 'docs/backlog.md'
    Equal ($backlog -match '(?m)^## T-013 - New item') $true
    Equal ($backlog -match '(?m)^## T-014 - Second') $true
    Equal ($backlog -match 'depends_on: \[T-013\]') $true
    $decisions = Read 'docs/decisions/accepted.md'
    Equal ($decisions -match '(?m)^### R-208 - New\r$') $true
    Equal ($decisions -match 'Amended by R-208') $true
    Equal ($decisions -match '(?m)^### R-209 - Second') $true
    Equal ($decisions -match '(?m)^### R-210 - Ninth') $true
    Equal ($decisions -match '(?m)^### R-211 - Tenth') $true
    Equal ($decisions -match 'Use `R-NEW` placeholders') $true
    Equal ($decisions -match '(?m)^### R-NEW - fenced') $true
    Equal (Test-Path (Join-Path $repo 'docs/specs/T-013-new-item/spec.md')) $true
    $spec = Read 'docs/specs/T-013-new-item/spec.md'
    Equal ($spec -match 'id: T-013') $true
    Equal ($spec -match 'See T-013\.3 and `T-NEW`') $true
    Equal ((Read 'CONTRIBUTING.md') -match '`T-NEW` and `R-NEW`') $true
    # Refuses to run before fresh main is merged.
    Push-Location $repo
    git add -A; git commit -q -m numbered
    git checkout -q -b moved
    git commit -q --allow-empty -m 'main moves on'
    git update-ref refs/remotes/origin/main HEAD
    git checkout -q main
    Pop-Location
    $refused = $false
    try { & $tool -Root $repo -SkipFinalCheck -NoFetch | Out-Null } catch { $refused = $_.Exception.Message -like '*Merge fresh origin/main*' }
    Equal $refused $true
    Write-Output "PASS: $script:count numbering assertions"
} finally {
    if ((Get-Location).Path -eq $repo) { Pop-Location }
    Remove-Item -Recurse -Force $repo
}
