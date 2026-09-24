[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $InputDirectory,
    [Parameter(Mandatory)][string] $EvidenceDirectory
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
# Deliberate package/trust/fault operations are restricted to the disposable Sandbox account.
if ($env:USERNAME -ne 'WDAGUtilityAccount') { throw 'Windows Sandbox is required. Never run this harness on the host.' }
if (Get-AppxPackage -Name AiUsage.Dev) { throw 'A fresh guest is required.' }
if (Test-Path -LiteralPath $EvidenceDirectory) {
    if (@(Get-ChildItem -LiteralPath $EvidenceDirectory -Force).Count -ne 0) { throw 'Use an empty evidence directory.' }
} else { New-Item -ItemType Directory -Path $EvidenceDirectory | Out-Null }
$report = [ordered]@{ status = 'STARTED'; utc = [DateTime]::UtcNow.ToString('o'); oldUi = 'NOT_RUN'; update = 'NOT_RUN'; recovery = 'NOT_RUN'; credentials = 'NOT_RUN' }
function Save-Report { $report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $EvidenceDirectory 'upgrade-report.json') -Encoding UTF8 }
Save-Report
try {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    Add-Type -AssemblyName System.Security
    $old = Join-Path $InputDirectory 'old.msix'
    $next = Join-Path $InputDirectory 'new.msix'
    $cer = Join-Path $InputDirectory 'AiUsage.Development.cer'
    $certificate = New-Object Security.Cryptography.X509Certificates.X509Certificate2($cer)
    $versions = @()
    foreach ($package in @($old, $next)) {
        $report.stage = 'Inspecting ' + [IO.Path]::GetFileName($package); Save-Report
        $zip = [IO.Compression.ZipFile]::OpenRead($package)
        try {
            $reader = New-Object IO.StreamReader($zip.GetEntry('AppxManifest.xml').Open())
            try { [xml]$manifest = $reader.ReadToEnd() } finally { $reader.Dispose() }
        } finally { $zip.Dispose() }
        if ($manifest.Package.Identity.Name -ne 'AiUsage.Dev' -or $manifest.Package.Identity.Publisher -ne 'CN=AI Usage Development') { throw 'Unexpected package identity.' }
        $signature = Get-AuthenticodeSignature -LiteralPath $package
        if (!$signature.SignerCertificate -or $signature.SignerCertificate.Thumbprint -ne $certificate.Thumbprint -or $certificate.Subject -ne $manifest.Package.Identity.Publisher) { throw 'Package signer mismatch.' }
        $versions += [version]$manifest.Package.Identity.Version
    }
    if ($versions[1] -le $versions[0]) { throw 'Expected a strictly newer package version.' }
    $dependencies = @(Get-ChildItem -LiteralPath (Join-Path $InputDirectory 'dependencies') -File | Where-Object Extension -In '.msix','.appx')
    if ($dependencies.Count -eq 0) { throw 'Offline framework dependencies are required.' }
    $runtime = Join-Path $InputDirectory 'dotnet-runtime-10.0.12-win-x64.exe'
    foreach ($path in @($runtime) + @($dependencies.FullName)) {
        $report.stage = 'Verifying ' + [IO.Path]::GetFileName($path); Save-Report
        $signature = Get-AuthenticodeSignature -LiteralPath $path
        if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation') { throw 'Official prerequisite signature invalid.' }
    }
    $report.stage = 'Guest trust'; Save-Report
    Import-Certificate -FilePath $cer -CertStoreLocation Cert:/LocalMachine/TrustedPeople | Out-Null
    foreach ($package in @($old, $next)) { if ((Get-AuthenticodeSignature -LiteralPath $package).Status -ne 'Valid') { throw 'Guest package trust verification failed.' } }
    $report.stage = 'Runtime installation'; Save-Report
    $dotnet = Join-Path $env:ProgramFiles 'dotnet/dotnet.exe'
    $runtimes = @(if (Test-Path -LiteralPath $dotnet) { & $dotnet --list-runtimes })
    if (-not ($runtimes -match '^Microsoft.NETCore.App 10\.0\.12 ')) {
        # Start-Process -Wait also waits for descendants, including long-lived MSI service processes.
        # Hold and wait for this installer's own handle instead.
        $installer = Start-Process $runtime -ArgumentList '/install','/quiet','/norestart' -WindowStyle Hidden -PassThru
        $null = $installer.Handle
        if (!$installer.WaitForExit(600000)) { throw 'Runtime installer exceeded ten minutes.' }
        if ($installer.ExitCode -ne 0) { throw 'Runtime installation failed.' }
        $installer.Dispose()
    }
    $report.stage = 'Old package installation'; Save-Report
    Add-AppxPackage -Path $old -DependencyPath @($dependencies.FullName)
    $installed = Get-AppxPackage -Name AiUsage.Dev
    if ([version]$installed.Version -ne $versions[0]) { throw 'Old version was not installed.' }
    $report.oldVersion = [string]$installed.Version
    $family = $installed.PackageFamilyName
    $state = Join-Path $env:LOCALAPPDATA "Packages/$family/LocalState"
    New-Item -ItemType Directory -Force -Path (Join-Path $state 'providers') | Out-Null
    $preferences = '{"Version":1,"Theme":2,"AlwaysOnTop":true,"Labels":{"opaque/provider":"Synthetic checkpoint"},"future":{"raw":[null,7]}}'
    [IO.File]::WriteAllText((Join-Path $state 'appearance.v1.json'), $preferences)
    # Synthetic credentials exist only in this network-disabled guest. No personal state is mapped.
    $grantPath = Join-Path $state 'providers/codex.grant'
    $plain = [Text.Encoding]::UTF8.GetBytes('{"v":1,"account":"synthetic-upgrade-account","refresh":"synthetic-upgrade-refresh"}')
    $entropy = [Text.Encoding]::UTF8.GetBytes('AiUsage.Codex.Grant.v1')
    $cipher = [Security.Cryptography.ProtectedData]::Protect($plain, $entropy, [Security.Cryptography.DataProtectionScope]::CurrentUser)
    [IO.File]::WriteAllBytes($grantPath, $cipher)
    $beforeGrant = (Get-FileHash -LiteralPath $grantPath -Algorithm SHA256).Hash
    $beforePreferences = (Get-FileHash -LiteralPath (Join-Path $state 'appearance.v1.json') -Algorithm SHA256).Hash
    $env:AIU_SMOKE_AUMID = "$family!App"
    $env:AIU_SMOKE_EXE = $null
    $env:AIU_RECOVERY_GUEST_ROOT = $state
    $env:AIU_UPGRADE_PHASE = 'old'
    $env:AIU_SMOKE_EVIDENCE_DIRECTORY = Join-Path $EvidenceDirectory 'old'
    $smoke = Join-Path $InputDirectory 'smoke/AiUsage.Windows.Tests.exe'
    & $smoke -noLogo -explicit on -method '*UpgradeRecovery' *> (Join-Path $EvidenceDirectory 'old-ui.log')
    if ($LASTEXITCODE -ne 0) { throw 'Old package UI failed.' }
    $report.oldUi = 'PASS'
    Add-AppxPackage -Path $next
    $updated = Get-AppxPackage -Name AiUsage.Dev
    if ([version]$updated.Version -ne $versions[1] -or $updated.PackageFamilyName -ne $family) { throw 'Same-family update failed.' }
    if ((Get-FileHash -LiteralPath $grantPath).Hash -ne $beforeGrant -or (Get-FileHash -LiteralPath (Join-Path $state 'appearance.v1.json')).Hash -ne $beforePreferences) { throw 'Package update changed durable bytes.' }
    $report.newVersion = [string]$updated.Version
    $report.update = 'PASS'
    Save-Report
    $env:AIU_UPGRADE_PHASE = 'new'
    $env:AIU_SMOKE_EVIDENCE_DIRECTORY = Join-Path $EvidenceDirectory 'new'
    & $smoke -noLogo -explicit on -method '*UpgradeRecovery' *> (Join-Path $EvidenceDirectory 'new-ui.log')
    if ($LASTEXITCODE -ne 0) { throw 'New package recovery UI failed.' }
    $report.recovery = 'PASS'
    if ((Get-FileHash -LiteralPath $grantPath).Hash -ne $beforeGrant -or (Get-FileHash -LiteralPath (Join-Path $state 'preferences/appearance.v1.json')).Hash -ne $beforePreferences) { throw 'Recovery changed protected credentials or preferences.' }
    $decoded = [Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($grantPath), $entropy, [Security.Cryptography.DataProtectionScope]::CurrentUser)
    if ([Text.Encoding]::UTF8.GetString($decoded) -ne [Text.Encoding]::UTF8.GetString($plain)) { throw 'Synthetic credential decryption mismatch.' }
    $report.credentials = 'PASS'
    $report.oldPackageSha256 = (Get-FileHash -LiteralPath $old).Hash
    $report.newPackageSha256 = (Get-FileHash -LiteralPath $next).Hash
    $report.status = 'PASS'
} catch {
    $report.status = 'FAIL'
    $report.error = $_.Exception.Message
} finally { Save-Report }
