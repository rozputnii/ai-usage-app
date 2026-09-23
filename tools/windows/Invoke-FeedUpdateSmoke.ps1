[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('Install', 'Update')][string] $Phase,
    [Parameter(Mandatory)][string] $InputDirectory,
    [Parameter(Mandatory)][string] $EvidenceDirectory,
    [Parameter(Mandatory)][string] $ExpectedVersion,
    [Parameter(Mandatory)][string] $ExpectedThumbprint,
    [string] $FeedUri = 'https://rozputnii.github.io/ai-usage-app/AiUsage.appinstaller',
    [int] $UpdateTimeoutMinutes = 20
)
# Windows PowerShell 5.1 guest harness for the AIU-014 development Preview channel. Install
# trusts only the public CER in the guest, installs through the published App Installer feed
# and seeds synthetic preferences. Update waits for the OS-managed feed update. No host use.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ($env:USERNAME -ne 'WDAGUtilityAccount') { throw 'Windows Sandbox is required. Never run this harness on the host.' }
if ($FeedUri -notmatch '^https://') { throw 'The feed must use HTTPS.' }
$phaseEvidence = Join-Path $EvidenceDirectory $Phase.ToLowerInvariant()
if (Test-Path -LiteralPath $phaseEvidence) { throw 'Use a fresh phase evidence directory.' }
New-Item -ItemType Directory -Path $phaseEvidence | Out-Null
$report = [ordered]@{ phase = $Phase; status = 'STARTED'; utc = [DateTime]::UtcNow.ToString('o'); expectedVersion = $ExpectedVersion }
function Save-Report { $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $phaseEvidence 'report.json') -Encoding UTF8 }
function Get-Installed { @(Get-AppxPackage -Name AiUsage.Dev) }
function Save-Registration([string] $name) {
    $package = (Get-Installed)[0]
    $settings = try { Get-AppxPackageAutoUpdateSettings -PackageFamilyName $package.PackageFamilyName | Format-List * | Out-String -Width 400 } catch { "UNAVAILABLE: $($_.Exception.Message)" }
    $settings | Set-Content -LiteralPath (Join-Path $phaseEvidence "$name-autoupdate.txt") -Encoding UTF8
    $package | Format-List * | Out-String -Width 400 | Set-Content -LiteralPath (Join-Path $phaseEvidence "$name-package.txt") -Encoding UTF8
    $null = [Windows.Management.Deployment.PackageManager, Windows.Management.Deployment, ContentType = WindowsRuntime]
    $info = ([Windows.Management.Deployment.PackageManager]::new()).FindPackageForUser('', $package.PackageFullName).GetAppInstallerInfo()
    return [ordered]@{ version = [string]$package.Version; family = $package.PackageFamilyName; publisher = $package.Publisher; signatureKind = [string]$package.SignatureKind; appInstallerUri = if ($info) { [string]$info.Uri } else { $null } }
}
function Invoke-Smoke([string] $version, [string] $name) {
    $env:AIU_SMOKE_AUMID = "$((Get-Installed)[0].PackageFamilyName)!App"
    $env:AIU_UPDATE_EXPECTED_VERSION = $version
    $env:AIU_SMOKE_EVIDENCE_DIRECTORY = Join-Path $phaseEvidence $name
    $smoke = Join-Path $InputDirectory 'smoke/AiUsage.Windows.Tests.exe'
    & $smoke -noLogo -explicit on -method '*AppInstallerActivationPreservesPreferences' *> (Join-Path $phaseEvidence "$name.log")
    if ($LASTEXITCODE -ne 0) { throw "Update activation smoke failed for $version." }
}
function Get-StateHashes([string] $family) {
    $state = Join-Path $env:LOCALAPPDATA "Packages/$family/LocalState"
    $hashes = [ordered]@{}
    foreach ($file in Get-ChildItem -LiteralPath $state -Recurse -File | Sort-Object FullName) { $hashes[$file.FullName.Substring($state.Length + 1)] = (Get-FileHash -LiteralPath $file.FullName).Hash }
    return $hashes
}
Save-Report
try {
    # Windows PowerShell returns application/appinstaller content as bytes; decode it explicitly.
    $feedText = [Text.Encoding]::UTF8.GetString((Invoke-WebRequest -Uri $FeedUri -UseBasicParsing).RawContentStream.ToArray())
    [IO.File]::WriteAllText((Join-Path $phaseEvidence 'feed.xml'), $feedText)
    [xml]$feedXml = $feedText
    $report.feedVersion = [string]$feedXml.AppInstaller.MainPackage.Version
    if ($feedXml.AppInstaller.MainPackage.Name -cne 'AiUsage.Dev' -or $feedXml.AppInstaller.MainPackage.Publisher -cne 'CN=AI Usage Development') { throw 'Unexpected feed identity.' }
    if ($report.feedVersion -ne $ExpectedVersion) { throw "Feed offers $($report.feedVersion), expected $ExpectedVersion." }
    if ($Phase -eq 'Install') {
        if (Get-Installed) { throw 'A fresh guest is required.' }
        $report.stage = 'Guest trust'; Save-Report
        $cer = Join-Path $InputDirectory 'AiUsage.Development.cer'
        $certificate = New-Object Security.Cryptography.X509Certificates.X509Certificate2($cer)
        if ($certificate.Thumbprint -cne $ExpectedThumbprint -or $certificate.HasPrivateKey -or $certificate.Subject -cne 'CN=AI Usage Development') { throw 'Unexpected public development CER.' }
        Import-Certificate -FilePath $cer -CertStoreLocation Cert:/LocalMachine/TrustedPeople | Out-Null
        $report.guestTrust = 'LocalMachine/TrustedPeople ' + $certificate.Thumbprint
        $report.stage = 'Runtime installation'; Save-Report
        $runtime = Join-Path $InputDirectory 'dotnet-runtime-10.0.12-win-x64.exe'
        $signature = Get-AuthenticodeSignature -LiteralPath $runtime
        if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation') { throw 'Official runtime signature invalid.' }
        $installer = Start-Process $runtime -ArgumentList '/install', '/quiet', '/norestart' -WindowStyle Hidden -PassThru
        $null = $installer.Handle
        if (!$installer.WaitForExit(600000) -or $installer.ExitCode -ne 0) { throw 'Runtime installation failed.' }
        $report.stage = 'App Installer feed installation'; Save-Report
        Add-AppxPackage -AppInstallerFile $FeedUri
        $report.installed = Save-Registration 'installed'
        if ($report.installed.version -ne $ExpectedVersion) { throw 'Feed installation installed an unexpected version.' }
        $report.stage = 'Synthetic durable state'; Save-Report
        $state = Join-Path $env:LOCALAPPDATA "Packages/$($report.installed.family)/LocalState"
        New-Item -ItemType Directory -Force -Path $state | Out-Null
        [IO.File]::WriteAllText((Join-Path $state 'appearance.v1.json'), '{"Version":1,"Theme":2,"Labels":{"opaque/provider":"Synthetic feed update"}}')
        $report.stage = 'Installed activation'; Save-Report
        Invoke-Smoke $ExpectedVersion 'installed-ui'
        $report.stateHashes = Get-StateHashes $report.installed.family
    } else {
        $before = Save-Registration 'before'
        $report.before = $before
        if ([version]$before.version -ge [version]$ExpectedVersion) { throw 'The installed package is not older than the feed.' }
        $report.stage = 'Launch-triggered OS update check'; Save-Report
        # Activation of the installed version lets Windows check the feed without blocking launch.
        Invoke-Smoke $before.version 'pre-update-ui'
        $beforeHashes = Get-StateHashes $before.family
        $report.stage = 'Waiting for Windows-managed update'; Save-Report
        $deadline = [DateTime]::UtcNow.AddMinutes($UpdateTimeoutMinutes)
        while ([DateTime]::UtcNow -lt $deadline -and !@(Get-Installed | Where-Object { $_.Version -eq $ExpectedVersion }).Count) { Start-Sleep -Seconds 15 }
        $report.updateObservedUtc = [DateTime]::UtcNow.ToString('o')
        $report.after = Save-Registration 'after'
        if ($report.after.version -ne $ExpectedVersion) { throw 'Windows did not apply the feed update before the timeout.' }
        if ($report.after.family -ne $before.family) { throw 'The update changed package family.' }
        $afterHashes = Get-StateHashes $before.family
        $report.stateBefore = $beforeHashes
        $report.stateAfterUpdate = $afterHashes
        foreach ($key in $beforeHashes.Keys) { if ($afterHashes[$key] -ne $beforeHashes[$key]) { throw "Update changed durable file $key." } }
        $report.stage = 'Updated activation'; Save-Report
        Invoke-Smoke $ExpectedVersion 'updated-ui'
    }
    $report.status = 'PASS'
} catch {
    $report.status = 'FAIL'
    $report.error = $_.Exception.Message
} finally { $report.completedUtc = [DateTime]::UtcNow.ToString('o'); Save-Report }
