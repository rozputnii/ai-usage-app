# Regression check for tools/windows/Set-ItemNumbers.ps1 (R13). Builds a throwaway Git repository under
# %TEMP%, runs the script without fetching or the final validation, and checks the numbering.
# Windows PowerShell 5.1.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$script:count = 0
function Equal($actual, $expected) { if ($actual -cne $expected) { throw "Expected '$expected', got '$actual'." }; $script:count++ }
$tool = Join-Path $PSScriptRoot '../../tools/windows/Set-ItemNumbers.ps1'
$repos = @()
function New-Repo {
    $script:repo = Join-Path ([IO.Path]::GetTempPath()) ('aiu-numbers-' + [guid]::NewGuid().ToString('N'))
    $script:repos += $script:repo
    New-Item -ItemType Directory -Path $script:repo | Out-Null
    Push-Location $script:repo
    git init -q -b main
    git config user.email test@example.invalid
    git config user.name test
    Pop-Location
}
function Commit-Base {
    Push-Location $repo
    git add -A; git commit -q -m base
    git update-ref refs/remotes/origin/main HEAD
    Pop-Location
}
function Put([string] $file, [string] $text, [switch] $Bom) {
    $path = Join-Path $repo $file
    New-Item -ItemType Directory -Force -Path (Split-Path $path) | Out-Null
    [IO.File]::WriteAllText($path, $text, [Text.UTF8Encoding]::new([bool]$Bom))
}
function Read([string] $file) { [IO.File]::ReadAllText((Join-Path $repo $file)) }
try {
    New-Repo
    # The highest item number, 012, is only a spec folder; T-NEW-1 is the same placeholder as T-NEW.
    # The highest decision number, 207, is only in superseded.md.
    Put 'docs/backlog.md' "## T-007 - Old`n- status: done`n`n## T-NEW - New item`n- depends_on: [T-007]`n- spec: ``docs/specs/T-NEW-new-item/spec.md```n`n## T-NEW-2 - Second`n- depends_on: [T-NEW-1]`n`n| T-009 | Indexed | done | G-001 | x |`n"
    Put 'docs/specs/T-012-numbered/spec.md' "---`nid: T-012`n---`n"
    Put 'docs/decisions/accepted.md' ("### R-205 - Old`r`nAmended by R-NEW (2026-10-09): x.`r`n`r`n### R-NEW - New`r`nUse ``R-NEW`` placeholders.`r`n`r`n``````text`r`n### R-NEW - fenced`r`n```````r`n" +
        "### R-NEW-9 - Ninth`r`n### R-NEW-10 - Tenth`r`n### R-NEW-2 - Second`r`n")
    Put 'docs/decisions/superseded.md' "### R-207 - Older`n"
    Put 'docs/specs/T-NEW-new-item/spec.md' "---`nid: T-NEW`n---`n# New`nSee T-NEW.3 and ``T-NEW``.`n"
    Put 'docs/specs/T-NEW-new-item/notes.md' "Second item: T-NEW-2.`n" -Bom
    Put 'CONTRIBUTING.md' "Use ``T-NEW`` and ``R-NEW`` until the merge.`n"
    Put '.github/copilot-instructions.md' "New rule from T-NEW.`n"
    Put '.claude/agents/example.md' "See R-NEW-2.`n"
    Commit-Base
    & $tool -Root $repo -SkipFinalCheck -NoFetch | Out-Null
    $backlog = Read 'docs/backlog.md'
    Equal ($backlog -match '(?m)^## T-013 - New item') $true
    Equal ($backlog -match '(?m)^## T-014 - Second') $true
    Equal ($backlog -match 'depends_on: \[T-013\]') $true
    # A spec folder path in a code span follows the folder rename.
    Equal ($backlog -match '`docs/specs/T-013-new-item/spec\.md`') $true
    Equal ((Read '.github/copilot-instructions.md') -match 'New rule from T-013\.') $true
    Equal ((Read '.claude/agents/example.md') -match 'See R-209\.') $true
    $notes = [IO.File]::ReadAllBytes((Join-Path $repo 'docs/specs/T-013-new-item/notes.md'))
    Equal ($notes[0] -eq 0xEF -and $notes[1] -eq 0xBB -and $notes[2] -eq 0xBF) $true
    Equal ((Read 'docs/specs/T-013-new-item/notes.md') -match 'Second item: T-014\.') $true
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

    # The highest item number is a Done index row; an untracked T-NEW folder whose slug starts with digits moves.
    New-Repo
    Put 'docs/backlog.md' "## T-NEW - Report`n- spec: [spec](specs/T-NEW-2024-report/spec.md)`n`n## Done index`n`n| T-020 | Done | done | G-001 | x |`n"
    Commit-Base
    Put 'docs/specs/T-NEW-2024-report/spec.md' "---`nid: T-NEW`n---`n"
    & $tool -Root $repo -SkipFinalCheck -NoFetch | Out-Null
    $backlog = Read 'docs/backlog.md'
    Equal ($backlog -match '(?m)^## T-021 - Report') $true
    Equal ($backlog -match '\(specs/T-021-2024-report/spec\.md\)') $true
    Equal (Test-Path (Join-Path $repo 'docs/specs/T-021-2024-report/spec.md')) $true
    Equal ((Read 'docs/specs/T-021-2024-report/spec.md') -match 'id: T-021') $true

    # Refuses a T-NEW folder whose placeholder no authored document names.
    New-Repo
    Put 'docs/backlog.md' "## T-NEW - Named`n"
    Put 'docs/specs/T-NEW-3-orphan/spec.md' "# Orphan`n"
    Commit-Base
    $refused = $false
    try { & $tool -Root $repo -SkipFinalCheck -NoFetch | Out-Null } catch { $refused = $_.Exception.Message -like '*no matching placeholder*' }
    Equal $refused $true
    Write-Output "PASS: $script:count numbering assertions"
} finally {
    while ($repos -contains (Get-Location).Path) { Pop-Location }
    foreach ($r in $repos) { Remove-Item -Recurse -Force $r }
}
