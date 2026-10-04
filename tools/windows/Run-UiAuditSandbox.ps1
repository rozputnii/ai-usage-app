param(
    [string]$PageDirectory = '.ai-usage-local/ui-audit/pages',
    [string]$OutputDirectory = '.ai-usage-local/ui-audit/sandbox',
    [string]$TestMethod = '*ReplayPagesRenderUsedAndLeft',
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
if ($TestMethod -notmatch '^[A-Za-z0-9.*]+$' -or ($FirstPage -and $FirstPage -notmatch '^(overview|page-\d{3})$')) { throw 'Invalid test/page filter.' }
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
    [ordered]@{
        Source = (& git rev-parse HEAD); Dirty = [bool](& git status --porcelain)
        ApplicationSha256 = (Get-FileHash -LiteralPath (Join-Path $inputPath 'app/AiUsage.dll')).Hash
        DriverSha256 = (Get-FileHash -LiteralPath (Join-Path $inputPath 'smoke/AiUsage.Windows.Tests.dll')).Hash
        TestMethod = $TestMethod; FirstPage = $FirstPage
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $inputPath 'build.json')
} finally { Pop-Location }

# This script runs only in the disposable guest, with its own writable work directory.
@'
param([string]$Method, [string]$FirstPage)
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
    for ($probe = 0; $probe -lt 6; $probe++) {
        & C:/AIU-Work/smoke/AiUsage.Windows.Tests.exe -class '*AuditDesktopPrerequisite' -parallel none -xml C:/AIU-Evidence/desktop.xml *> C:/AIU-Evidence/desktop.log
        $report.DesktopExitCode = $LASTEXITCODE
        if (!$LASTEXITCODE) { break }
        Start-Sleep -Seconds 1
    }
    if ($LASTEXITCODE) { $report.Status = 'BLOCKED'; return }
    & C:/AIU-Work/smoke/AiUsage.Windows.Tests.exe -method $Method -parallel none -xml C:/AIU-Evidence/native.xml *> C:/AIU-Evidence/native.log
    $report.NativeExitCode = $LASTEXITCODE
    $report.Status = if ($LASTEXITCODE) { 'FAIL' } else { 'PASS_REQUIRES_VISUAL_INSPECTION' }
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
    $guestCommand = "powershell.exe -NoProfile -ExecutionPolicy Bypass -File C:\AIU-Input\Run.ps1 -Method $TestMethod -FirstPage '$FirstPage'"
    & wsb exec --id $sandbox.Id --command $guestCommand --run-as ExistingLogin --raw
    if ($LASTEXITCODE) { throw 'Sandbox command failed; inspect retained evidence.' }
} finally { & wsb stop --id $sandbox.Id --raw }
Get-Content -LiteralPath (Join-Path $evidencePath 'run.json')
