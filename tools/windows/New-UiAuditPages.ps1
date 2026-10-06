param(
    [Parameter(Mandatory)][string]$InputDirectory,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [string]$CoverageDirectory = (Join-Path $PSScriptRoot '../../.ai-usage-local/ui-audit/coverage'),
    [string]$NativeBlocker = '',
    [string[]]$ScenarioPatterns = @('*'),
    [string]$CompositeDirectory = '',
    [string]$TransitionDirectory = ''
)
$ErrorActionPreference = 'Stop'
$inputRoot = [IO.Path]::GetFullPath($InputDirectory)
$outputRoot = [IO.Path]::GetFullPath($OutputDirectory)
$coverageRoot = [IO.Path]::GetFullPath($CoverageDirectory)
[IO.Directory]::CreateDirectory($outputRoot) | Out-Null
[IO.Directory]::CreateDirectory($coverageRoot) | Out-Null
$cases = @([IO.Directory]::EnumerateFiles($inputRoot, '*.expectation.json') | Sort-Object | ForEach-Object {
    Get-Content -LiteralPath $_ -Raw | ConvertFrom-Json -AsHashtable -DateKind String
})
if ($cases.Count -eq 0) { throw 'Export the successfully tested synthetic corpus first.' }
$matrix = [Collections.Generic.List[object]]::new()

function Write-Page([string]$pageId, [object[]]$selected, [bool]$inventory) {
    $fixtures = @($selected | ForEach-Object {
        Get-Content -LiteralPath (Join-Path $inputRoot ($_.Id + '.json')) -Raw | ConvertFrom-Json -AsHashtable -DateKind String
    })
    $first = $fixtures[0]
    $page = @{
        SyntheticMarker = 'AI Usage synthetic audit v1'
        Now = $first.Now; ZoneId = $first.ZoneId
        Accounts = @($fixtures | ForEach-Object { $_.Accounts })
        Labels = @{}; Observations = @($fixtures | ForEach-Object { $_.Observations })
        Configuration = @{ WorkDays = $first.Configuration.WorkDays; Caps = @($fixtures | ForEach-Object { $_.Configuration.Caps }) }
        ExpectedStates = @{}
    }
    foreach ($fixture in $fixtures) {
        if ($fixture.Now -ne $page.Now -or $fixture.ZoneId -ne $page.ZoneId) { throw 'A page needs one controlled clock and timezone.' }
        foreach ($entry in $fixture.Labels.GetEnumerator()) { $page.Labels[$entry.Key] = $entry.Value }
        foreach ($entry in $fixture.ExpectedStates.GetEnumerator()) { $page.ExpectedStates[$entry.Key] = $entry.Value }
    }
    if ($pageId -eq 'overview') {
        foreach ($account in $page.Accounts) {
            $providerName = switch ($account.Provider) { 'claude' { 'Claude' } 'codex' { 'Codex' } 'copilot' { 'Copilot' } 'antigravity' { 'Antigravity' } }
            $page.Labels[$account.AccountId.Replace('-', '')] = $providerName + ' · SYNTHETIC'
        }
    }
    $page | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $outputRoot ($pageId + '.json')) -Encoding utf8
    $selected | ConvertTo-Json -Depth 100 -AsArray | Set-Content -LiteralPath (Join-Path $outputRoot ($pageId + '.expectations.json')) -Encoding utf8
    if (!$inventory) { return }
    for ($index = 0; $index -lt $selected.Count; $index++) {
        $case = $selected[$index]
        $fixture = $fixtures[$index]
        $matrix.Add([pscustomobject]@{
            ScenarioId = $case.Id; Provider = $case.Provider
            SyntheticInput = 'inputs/' + $case.Id + '.provider.json'
            ReplayInput = 'inputs/' + $case.Id + '.json'
            ControlledTime = $case.Now; Zone = $fixture.ZoneId
            MidnightBaseline = $case.Baseline; ExpectedUsed = $case.Used; ExpectedTodayEnd = $case.TodayEnd
            PersonalCap = $case.Cap; ExpectedState = $case.State; ExpectedLayout = $case.Layout; ExpectedHealth = $case.Health
            ExpectedAppearance = $case.Layout + '; ' + $case.State + '; values in native units; provider/assumed reset provenance retained'
            Interactions = 'Used/Left mouse clicks; card hover; ordinary scroll; history where available; cap editor where permitted'
            AutomatedTest = $case.Test + '(' + $case.Id + ')'
            Deterministic = $case.Deterministic
            NativeTest = 'AuditWindows.ReplayPagesRenderUsedAndLeft'
            PlannedScreenshot = 'gallery/' + $pageId + '-used.png; gallery/' + $pageId + '-left.png'
            RelevantCard = @($fixture.ExpectedStates.Keys) -join '; '
            NativeResult = $(if ($NativeBlocker) { 'BLOCKED' } else { 'NOT_RUN' })
            ScreenshotResult = 'NOT_CAPTURED'; VisualInspection = 'NOT_RUN'
            Reason = $NativeBlocker
        })
    }
}

$overview = @('M-ordinary', 'P-codex-ordinary', 'C-ordinary', 'P-antigravity-ordinary') | ForEach-Object {
    $match = @($cases | Where-Object Id -eq $_)
    if ($match.Count -ne 1) { throw "Missing overview scenario: $_" }
    $match[0]
}
Write-Page 'overview' $overview $false
$pageNumber = 0
foreach ($group in ($cases | Where-Object { $scenario = $_.Id; @($ScenarioPatterns | Where-Object { $scenario -like $_ }).Count -gt 0 } | Group-Object Now,Zone)) {
    $groupCases = @($group.Group)
    for ($offset = 0; $offset -lt $groupCases.Count; $offset += 4) {
        $last = [Math]::Min($offset + 3, $groupCases.Count - 1)
        Write-Page ('page-{0:D3}' -f (++$pageNumber)) $groupCases[$offset..$last] $true
    }
}
$matrix | Export-Csv -LiteralPath (Join-Path $coverageRoot 'coverage.csv') -NoTypeInformation -Encoding utf8
$matrix | Select-Object ScenarioId, PlannedScreenshot, RelevantCard, ScreenshotResult, VisualInspection, NativeResult |
    Export-Csv -LiteralPath (Join-Path $coverageRoot 'screenshot-index.csv') -NoTypeInformation -Encoding utf8
if ($CompositeDirectory) {
    $compositeIndex = foreach ($fixturePath in (Get-ChildItem -LiteralPath $CompositeDirectory -Filter '*.json' | Where-Object Name -NotLike '*.expectations.json' | Sort-Object Name)) {
        $scenarioId = $fixturePath.BaseName
        $fixture = Get-Content -LiteralPath $fixturePath.FullName -Raw | ConvertFrom-Json -AsHashtable -DateKind String
        if ($fixture.SyntheticMarker -ne 'AI Usage synthetic audit v1') { throw 'Composite requires the synthetic marker.' }
        $pageId = 'page-{0:D3}' -f (++$pageNumber)
        Copy-Item -LiteralPath $fixturePath.FullName -Destination (Join-Path $outputRoot ($pageId + '.json'))
        Copy-Item -LiteralPath (Join-Path $CompositeDirectory ($scenarioId + '.expectations.json')) -Destination (Join-Path $outputRoot ($pageId + '.expectations.json'))
        [pscustomobject]@{ ScenarioId = $scenarioId; PlannedScreenshot = 'gallery/' + $pageId + '-used.png; gallery/' + $pageId + '-left.png'
            RelevantCard = @($fixture.ExpectedStates.Keys) -join '; '; NativeResult = 'NOT_RUN'; VisualInspection = 'NOT_RUN' }
    }
    $compositeIndex | Export-Csv -LiteralPath (Join-Path $coverageRoot 'additional-screenshot-index.csv') -NoTypeInformation -Encoding utf8
}
if ($TransitionDirectory) {
    $transitionRoot = Join-Path $outputRoot 'transitions'
    New-Item -ItemType Directory -Path $transitionRoot -Force | Out-Null
    foreach ($fixturePath in (Get-ChildItem -LiteralPath $TransitionDirectory -Filter '*.json' | Where-Object Name -NotLike '*.expectations.json')) {
        $fixture = Get-Content -LiteralPath $fixturePath.FullName -Raw | ConvertFrom-Json -AsHashtable -DateKind String
        if ($fixture.SyntheticMarker -ne 'AI Usage synthetic audit v1' -or !$fixture.NextAccounts -or !$fixture.NextNow) { throw 'Transition requires a tested synthetic replacement.' }
        Copy-Item -LiteralPath $fixturePath.FullName -Destination $transitionRoot
        Copy-Item -LiteralPath (Join-Path $TransitionDirectory ($fixturePath.BaseName + '.next.expectations.json')) -Destination $transitionRoot
    }
}
"Prepared $($matrix.Count) parser-tested scenarios on $pageNumber pages plus an overview. No screenshots or native passes are implied."
