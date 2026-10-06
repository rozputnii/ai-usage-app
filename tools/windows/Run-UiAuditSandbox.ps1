param(
    [string]$PageDirectory = '.ai-usage-local/ui-audit/pages',
    [string]$OutputDirectory = '.ai-usage-local/ui-audit/sandbox',
    [string[]]$TestMethod = @('*ReplayPagesRenderUsedAndLeft'),
    [string]$FirstPage = ''
)
$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$allowed = [IO.Path]::GetFullPath((Join-Path $repository '.ai-usage-local/ui-audit')) + [IO.Path]::DirectorySeparatorChar
$output = [IO.Path]::GetFullPath((Join-Path $repository $OutputDirectory))
if (!$output.StartsWith($allowed, [StringComparison]::OrdinalIgnoreCase)) { throw 'Output must be a new directory below .ai-usage-local/ui-audit.' }
for ($ancestor = $output; $ancestor; $ancestor = [IO.Path]::GetDirectoryName($ancestor)) {
    if ((Test-Path -LiteralPath $ancestor) -and ((Get-Item -LiteralPath $ancestor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Audit staging must not use redirected directories.' }
}
if ((Test-Path -LiteralPath $output) -and @(Get-ChildItem -LiteralPath $output -Force).Count) { throw 'Choose a fresh output directory; previous evidence is preserved.' }
if (!$TestMethod.Count -or @($TestMethod | Where-Object { $_ -notmatch '^[A-Za-z0-9.*]+$' }).Count -or ($FirstPage -and $FirstPage -notmatch '^(overview|page-\d{3})$')) { throw 'Invalid test/page filter.' }
$pages = [IO.Path]::GetFullPath((Join-Path $repository $PageDirectory))
if (!$pages.StartsWith($allowed, [StringComparison]::OrdinalIgnoreCase)) { throw 'Only isolated audit pages may be mapped into the guest.' }
for ($ancestor = $pages; $ancestor; $ancestor = [IO.Path]::GetDirectoryName($ancestor)) {
    if ((Test-Path -LiteralPath $ancestor) -and ((Get-Item -LiteralPath $ancestor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Audit pages must not use redirected directories.' }
}
if (!(Test-Path -LiteralPath (Join-Path $pages 'overview.expectations.json'))) { throw 'Generate the parser-tested replay pages first.' }
Get-Command wsb.exe -ErrorAction Stop | Out-Null
$inputPath = Join-Path $output 'input'
$evidencePath = Join-Path $output 'evidence'
New-Item -ItemType Directory -Path $inputPath,$evidencePath -Force | Out-Null
Push-Location $repository
try {
    & dotnet publish src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Release -r win-x64 -p:Platform=x64 -p:WindowsPackageType=None -p:SelfContained=true -p:WindowsAppSDKSelfContained=true -o (Join-Path $inputPath 'app') -v:minimal
    if ($LASTEXITCODE) { throw 'Unpackaged application publish failed.' }
    & dotnet publish tests/windows/AiUsage.Windows.Tests -c Release -r win-x64 --self-contained true -o (Join-Path $inputPath 'smoke') -v:minimal
    if ($LASTEXITCODE) { throw 'Native driver publish failed.' }
    Copy-Item -LiteralPath $pages -Destination (Join-Path $inputPath 'pages') -Recurse
    # Binary-safe: git writes the file itself; an empty diff still produces an empty file.
    $sourceDiff = Join-Path $inputPath 'source.diff'
    & git diff HEAD --binary --output=$sourceDiff
    if ($LASTEXITCODE -or !(Test-Path -LiteralPath $sourceDiff)) { throw 'Source diff capture failed.' }
    [ordered]@{
        Source = (& git rev-parse HEAD); Dirty = [bool](& git status --porcelain)
        ApplicationSha256 = (Get-FileHash -LiteralPath (Join-Path $inputPath 'app/AiUsage.dll')).Hash
        DriverSha256 = (Get-FileHash -LiteralPath (Join-Path $inputPath 'smoke/AiUsage.Windows.Tests.dll')).Hash
        SourceDiffSha256 = (Get-FileHash -LiteralPath $sourceDiff).Hash
        TestMethod = $TestMethod; FirstPage = $FirstPage
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $inputPath 'build.json')
} finally { Pop-Location }

# This script runs only in the disposable guest, with its own writable work directory.
@'
param([string]$FirstPage)
$ErrorActionPreference = 'Stop'
if ($env:USERNAME -ne 'WDAGUtilityAccount') { throw 'Synthetic audit requires Windows Sandbox.' }
$report = [ordered]@{ Synthetic = $true; StartedAt = [DateTimeOffset]::UtcNow.ToString('o'); Status = 'STARTED'; Network = 'Disabled' }
try {
    New-Item -ItemType Directory -Path C:/AIU-Work -Force | Out-Null
    Copy-Item -LiteralPath C:/AIU-Input/app,C:/AIU-Input/smoke,C:/AIU-Input/pages -Destination C:/AIU-Work -Recurse -Force
    Copy-Item -LiteralPath C:/AIU-Input/build.json -Destination C:/AIU-Evidence
    $env:AIU_SMOKE_EXE = 'C:/AIU-Work/app/AiUsage.exe'
    $env:AIU_AUDIT_PAGE_DIRECTORY = 'C:/AIU-Work/pages'
    $env:AIU_SMOKE_EVIDENCE_DIRECTORY = 'C:/AIU-Evidence/gallery'
    $env:AIU_AUDIT_FIRST_PAGE = $FirstPage
    # A fresh guest can have no foreground window after its login helper closes.
    # Open only its synthetic work folder before the read-only desktop prerequisite.
    Start-Process explorer.exe -ArgumentList 'C:\AIU-Work' -WindowStyle Hidden
    for ($probe = 0; $probe -lt 6; $probe++) {
        $probeXml = 'C:/AIU-Evidence/desktop-' + $probe + '.xml'
        $probeLog = 'C:/AIU-Evidence/desktop-' + $probe + '.log'
        & C:/AIU-Work/smoke/AiUsage.Windows.Tests.exe -class '*AuditDesktopPrerequisite' -parallel none -xml $probeXml *> $probeLog
        $report.DesktopExitCode = $LASTEXITCODE
        Copy-Item -LiteralPath $probeXml -Destination C:/AIU-Evidence/desktop.xml -Force
        Copy-Item -LiteralPath $probeLog -Destination C:/AIU-Evidence/desktop.log -Force
        if (!$LASTEXITCODE) { break }
        Start-Sleep -Seconds 1
    }
    if ($LASTEXITCODE) { $report.Status = 'BLOCKED'; return }
    $nativeArguments = @('-parallel', 'none', '-xml', 'C:/AIU-Evidence/native.xml')
    foreach ($method in @((Get-Content C:/AIU-Input/build.json -Raw | ConvertFrom-Json).TestMethod)) { $nativeArguments += @('-method', $method) }
    & C:/AIU-Work/smoke/AiUsage.Windows.Tests.exe @nativeArguments *> C:/AIU-Evidence/native.log
    $report.NativeExitCode = $LASTEXITCODE
    [xml]$nativeXml = Get-Content C:/AIU-Evidence/native.xml -Raw
    $assemblies = @($nativeXml.assemblies.assembly)
    $report.Executed = ($assemblies | Measure-Object -Property total -Sum).Sum
    $report.Passed = ($assemblies | Measure-Object -Property passed -Sum).Sum
    $report.Failed = ($assemblies | Measure-Object -Property failed -Sum).Sum
    $report.Skipped = ($assemblies | Measure-Object -Property skipped -Sum).Sum
    $report.NotRun = ($assemblies | ForEach-Object { [int]$_.'not-run' } | Measure-Object -Sum).Sum
    $report.Errors = ($assemblies | Measure-Object -Property errors -Sum).Sum
    $report.Status = if ($report.NativeExitCode -or $report.Executed -le 0 -or $report.Passed -ne $report.Executed -or
        $report.Failed -or $report.Skipped -or $report.NotRun -or $report.Errors) { 'FAIL' } else { 'PASS_REQUIRES_VISUAL_INSPECTION' }
} catch { $report.Status = 'FAIL'; $report.Error = $_.Exception.Message }
finally { $report | ConvertTo-Json | Set-Content C:/AIU-Evidence/run.json }
'@ | Set-Content -LiteralPath (Join-Path $inputPath 'Run.ps1')

$escapedInput = [Security.SecurityElement]::Escape($inputPath)
$escapedEvidence = [Security.SecurityElement]::Escape($evidencePath)
$configuration = @"
<Configuration>
  <Networking>Disable</Networking><ClipboardRedirection>Disable</ClipboardRedirection>
  <AudioInput>Disable</AudioInput><VideoInput>Disable</VideoInput><PrinterRedirection>Disable</PrinterRedirection>
  <MappedFolders>
    <MappedFolder><HostFolder>$escapedInput</HostFolder><SandboxFolder>C:\AIU-Input</SandboxFolder><ReadOnly>true</ReadOnly></MappedFolder>
    <MappedFolder><HostFolder>$escapedEvidence</HostFolder><SandboxFolder>C:\AIU-Evidence</SandboxFolder><ReadOnly>false</ReadOnly></MappedFolder>
  </MappedFolders>
</Configuration>
"@
$configuration | Set-Content -LiteralPath (Join-Path $output 'audit.wsb')
$sandbox = (& wsb start --raw --config $configuration | ConvertFrom-Json)
if ($LASTEXITCODE -or !$sandbox.Id) { throw 'Sandbox start did not return an ID.' }
$sandbox.Id | Set-Content -LiteralPath (Join-Path $output 'sandbox-id.txt')
try {
    Start-Process -FilePath (Get-Command wsb.exe).Source -ArgumentList 'connect','--id',$sandbox.Id -WindowStyle Hidden
    $ready = $false
    for ($probe = 0; $probe -lt 30; $probe++) {
        # Windows PowerShell 5.1 turns redirected native stderr into a terminating error under Stop.
        $ErrorActionPreference = 'Continue'
        $probeOutput = & wsb exec --id $sandbox.Id --command 'whoami.exe' --run-as ExistingLogin --raw 2>&1
        $ErrorActionPreference = 'Stop'
        if (!$LASTEXITCODE) { $ready = $true; break }
        $probeOutput | Out-File -LiteralPath (Join-Path $evidencePath 'desktop-ready.log') -Append
        Start-Sleep -Seconds 1
    }
    if (!$ready) { throw 'BLOCKED: Sandbox has not established its interactive guest session.' }
    $guestCommand = "powershell.exe -NoProfile -ExecutionPolicy Bypass -File C:\AIU-Input\Run.ps1"
    if ($FirstPage) { $guestCommand += " -FirstPage $FirstPage" }
    & wsb exec --id $sandbox.Id --command $guestCommand --run-as ExistingLogin --raw
    if ($LASTEXITCODE) { throw 'Sandbox command failed; inspect retained evidence.' }
} finally { & wsb stop --id $sandbox.Id --raw }
$result = Get-Content -LiteralPath (Join-Path $evidencePath 'run.json') -Raw | ConvertFrom-Json
$result | ConvertTo-Json
if ($result.Status -ne 'PASS_REQUIRES_VISUAL_INSPECTION') { throw 'The native run did not pass; inspect retained evidence.' }
