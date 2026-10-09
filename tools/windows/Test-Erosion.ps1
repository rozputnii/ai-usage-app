<#
.SYNOPSIS
Reports possible test erosion between a base ref and HEAD. Informational only: always exits 0.

.DESCRIPTION
Looks at changed C# files under tests/ and lists removed [Fact]/[Theory] methods, newly added
Skip = or Explicit = true, files whose Assert. count dropped, and changed files under fixture or
expected-data folders. Each listed item needs a justification in the change description.

.EXAMPLE
powershell -NoProfile -File tools/windows/Test-Erosion.ps1
powershell -NoProfile -File tools/windows/Test-Erosion.ps1 -Base HEAD~1
#>
[CmdletBinding()]
param([string] $Base = 'origin/main')
Set-StrictMode -Version Latest

function Get-TestMethods([string[]] $Lines) {
    # Qualified Class.Method names of methods carrying [Fact] or [Theory]. File-scoped namespaces
    # are enforced, so test classes start at column 0 and nested helper types are ignored.
    $class = ''; $pending = $false
    foreach ($line in $Lines) {
        if ($line -match '^(?:(?:public|internal|sealed|static|abstract|partial)\s+)*(?:class|record)\s+(\w+)') { $class = $Matches[1] }
        if ($line -match '\[\s*(?:Fact|Theory)\b') { $pending = $true }
        if ($pending -and $line -match '^\s*(?:public|internal|private|protected)\b[^=]*?\b(\w+)\s*\(') {
            "$class.$($Matches[1])"; $pending = $false
        }
    }
}

function Get-Content-At([string] $Commit, [string] $Path) {
    if (!$Path) { return @() }
    $text = git -C $root show "${Commit}:$Path" 2>$null
    if ($LASTEXITCODE -ne 0) { return @() }
    @($text)
}

try {
    $root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
    $mergeBase = git -C $root merge-base $Base HEAD
    if ($LASTEXITCODE -ne 0 -or !$mergeBase) { throw "Cannot find a merge base for '$Base' and HEAD." }
    $range = "$mergeBase..HEAD"

    $changes = foreach ($entry in git -C $root diff --name-status -M $range -- tests) {
        $parts = $entry -split "`t"
        $status = $parts[0].Substring(0, 1)
        [pscustomobject]@{
            Status = $status
            Old = if ($status -eq 'A') { $null } else { $parts[1] }
            New = if ($status -eq 'D') { $null } else { $parts[-1] }
        }
    }
    $changes = @($changes)
    $csharp = @($changes | Where-Object { ($_.New, $_.Old | Where-Object { $_ }) -like '*.cs' })

    $deleted = @(); $asserts = @(); $baseTests = @{}; $headTests = @{}
    foreach ($change in $csharp) {
        $before = Get-Content-At $mergeBase $change.Old
        $after = Get-Content-At 'HEAD' $change.New
        foreach ($name in Get-TestMethods $before) { $baseTests[$name] = $change.Old }
        foreach ($name in Get-TestMethods $after) { $headTests[$name] = $change.New }
        $countBefore = ([regex]::Matches(($before -join "`n"), '\bAssert\.')).Count
        $countAfter = ([regex]::Matches(($after -join "`n"), '\bAssert\.')).Count
        if ($countAfter -lt $countBefore) { $asserts += "  $(if ($change.New) { $change.New } else { $change.Old }): $countBefore -> $countAfter" }
    }
    # Compare across all changed files so a test moved between files of one class is not reported.
    foreach ($name in $baseTests.Keys | Sort-Object) {
        if (!$headTests.ContainsKey($name)) { $deleted += "  $name ($($baseTests[$name]))" }
    }

    $skips = @(); $file = ''
    foreach ($line in git -C $root diff -U0 -M $range -- 'tests/*.cs') {
        if ($line -match '^\+\+\+ (?:b/)?(.*)$') { $file = $Matches[1]; continue }
        if ($line -match '^\+' -and $line -match '\bSkip\s*=|\bExplicit\s*=\s*true|\bAssert\.Skip') { $skips += "  ${file}: $($line.Substring(1).Trim())" }
    }

    $fixtures = @($changes | Where-Object { ($_.New, $_.Old | Where-Object { $_ }) -match '(?i)/(fixtures?|expected|golden|snapshots?|testdata)/' } |
        ForEach-Object { "  $($_.Status) $(if ($_.New) { $_.New } else { $_.Old })" })

    Write-Output "Test-erosion report for $Base...HEAD ($($csharp.Count) changed C# test files)"
    $sections = [ordered]@{
        'Deleted [Fact]/[Theory] methods' = $deleted
        'Newly added Skip = / Explicit = true' = $skips
        'Files with fewer Assert. calls' = $asserts
        'Changed fixture or expected-data files' = $fixtures
    }
    $found = 0
    foreach ($title in $sections.Keys) {
        $items = @($sections[$title])
        if ($items.Count -eq 0) { continue }
        $found += $items.Count
        Write-Output ''; Write-Output "${title}:"; $items | ForEach-Object { Write-Output $_ }
    }
    Write-Output ''
    if ($found -eq 0) { Write-Output 'No test erosion signals found.' }
    else { Write-Output "Justify each of the $found item(s) above in the change description: why is the removed, skipped or weakened check no longer needed?" }
}
catch { Write-Warning "Test-erosion check could not run: $($_.Exception.Message)" }
exit 0
