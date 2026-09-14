[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $PackagePath,
    [Parameter(Mandatory)][string] $CertificatePath,
    [Parameter(Mandatory)][string] $DependenciesDirectory,
    [Parameter(Mandatory)][string] $DotNetRuntimeInstaller,
    [Parameter(Mandatory)][string] $SmokeExecutable,
    [Parameter(Mandatory)][string] $EvidenceDirectory,
    [switch] $ConfirmDisposableGuest
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
# Sandbox identifies its disposable account. Other VMs require an explicit invocation switch.
if ($env:USERNAME -ne 'WDAGUtilityAccount' -and !$ConfirmDisposableGuest) { throw 'Prerequisite: run only inside Windows Sandbox or pass -ConfirmDisposableGuest inside a disposable VM. Never run on the host.' }
foreach ($file in @($PackagePath, $CertificatePath, $DotNetRuntimeInstaller, $SmokeExecutable)) {
    if (!(Test-Path -LiteralPath $file -PathType Leaf)) { throw "Prerequisite: required staging file missing: $([IO.Path]::GetFileName($file))" }
}
if (!(Test-Path -LiteralPath $DependenciesDirectory -PathType Container)) { throw 'Prerequisite: offline dependency directory missing.' }
$dependencies = @(Get-ChildItem -LiteralPath $DependenciesDirectory -File | Where-Object { $_.Extension -in @('.msix', '.appx') })
if ($dependencies.Count -eq 0) { throw 'Prerequisite: offline Microsoft framework packages missing.' }
if ([Environment]::OSVersion.Version.Build -lt 26100 -or !$env:PROCESSOR_ARCHITECTURE.Equals('AMD64')) { throw 'Prerequisite: Windows 11 24H2+ x64 required.' }
$admin = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (!$admin.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Prerequisite: guest administrator required; no automatic elevation.' }
if (Test-Path -LiteralPath $EvidenceDirectory) {
    if (@(Get-ChildItem -LiteralPath $EvidenceDirectory -Force).Count -ne 0) { throw 'Prerequisite: use a fresh empty evidence directory.' }
} else { New-Item -ItemType Directory -Path $EvidenceDirectory | Out-Null }
$report = [ordered]@{ status = 'prerequisite-check'; utc = [DateTime]::UtcNow.ToString('o'); positive = 'NOT_RUN'; remote = 'NOT_RUN' }
function Assert-MicrosoftSignature([string] $Path) {
    $signature = Get-AuthenticodeSignature -LiteralPath $Path
    if ($signature.Status -ne 'Valid' -or !$signature.SignerCertificate -or $signature.SignerCertificate.Subject -notmatch '(^|, )O=Microsoft Corporation(,|$)') { throw "Prerequisite: invalid Microsoft signature: $([IO.Path]::GetFileName($Path))" }
}
function Read-ZipText($Zip, [string] $Name) {
    $entry = $Zip.GetEntry($Name)
    if (!$entry) { throw "Prerequisite: package entry missing: $Name" }
    $reader = New-Object IO.StreamReader($entry.Open())
    try { return $reader.ReadToEnd() } finally { $reader.Dispose() }
}
try {
    Assert-MicrosoftSignature $DotNetRuntimeInstaller
    foreach ($dependency in $dependencies) { Assert-MicrosoftSignature $dependency.FullName }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [IO.Compression.ZipFile]::OpenRead($PackagePath)
    try {
        [xml]$manifest = Read-ZipText $zip 'AppxManifest.xml'
        $runtime = (Read-ZipText $zip 'AiUsage.runtimeconfig.json' | ConvertFrom-Json).runtimeOptions.framework
    } finally { $zip.Dispose() }
    $report.dependencies = @($dependencies | ForEach-Object { [ordered]@{
        name = $_.Name; sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
        signer = (Get-AuthenticodeSignature -LiteralPath $_.FullName).SignerCertificate.Thumbprint
    } })
    $report.runtimeInstaller = [ordered]@{
        name = [IO.Path]::GetFileName($DotNetRuntimeInstaller)
        sha256 = (Get-FileHash -LiteralPath $DotNetRuntimeInstaller -Algorithm SHA256).Hash
        signer = (Get-AuthenticodeSignature -LiteralPath $DotNetRuntimeInstaller).SignerCertificate.Thumbprint
    }
    if ($manifest.Package.Identity.Name -ne 'AiUsage.Dev' -or $manifest.Package.Identity.Publisher -ne 'CN=AI Usage Development' -or $runtime.name -ne 'Microsoft.NETCore.App' -or $runtime.version -notmatch '^10\.') { throw 'Prerequisite: unexpected app identity or runtime contract.' }
    $certificate = New-Object Security.Cryptography.X509Certificates.X509Certificate2($CertificatePath)
    $signature = Get-AuthenticodeSignature -LiteralPath $PackagePath
    if ($certificate.Subject -ne $manifest.Package.Identity.Publisher -or !$signature.SignerCertificate -or $signature.SignerCertificate.Thumbprint -ne $certificate.Thumbprint) { throw 'Prerequisite: package and public certificate mismatch.' }
    $dotnet = Join-Path $env:ProgramFiles 'dotnet/dotnet.exe'
    $report.signer = $certificate.Thumbprint
    $runtimeInventory = @(if (Test-Path -LiteralPath $dotnet) { & $dotnet --list-runtimes })
    $frameworkInventory = @(Get-AppxPackage -PackageTypeFilter Framework | Select-Object Name, Version, Architecture)
    $report.runtimeRequired = $runtime
    $report.initialRuntimes = $runtimeInventory
    $report.initialFrameworks = $frameworkInventory
    $report.packageHash = (Get-FileHash -LiteralPath $PackagePath -Algorithm SHA256).Hash
    $report.packageVersion = [string]$manifest.Package.Identity.Version
    if (Get-AppxPackage -Name 'AiUsage.Dev') { throw 'Prerequisite: fresh guest must not contain AiUsage.Dev.' }
    $sdks = @(if (Test-Path -LiteralPath $dotnet) { & $dotnet --list-sdks })
    if ($sdks.Count -ne 0 -or (Test-Path "${env:ProgramFiles(x86)}/Microsoft Visual Studio/Installer/vswhere.exe")) { throw 'Prerequisite: guest must not contain a developer SDK or Visual Studio.' }
    # Trust is changed only after all inputs validate, and only inside this disposable guest.
    Import-Certificate -FilePath $CertificatePath -CertStoreLocation Cert:\LocalMachine\TrustedPeople | Out-Null
    if ((Get-AuthenticodeSignature -LiteralPath $PackagePath).Status -ne 'Valid') { throw 'Package signature is not valid after guest-only trust provisioning.' }
    $report.frameworkNegative = 'NOT_RUN'
    $report.dotnetNegative = 'NOT_RUN'
    try {
        Add-AppxPackage -Path $PackagePath
        $report.frameworkNegative = 'NO_MISSING_PREREQUISITE_OBSERVED'
    } catch {
        $report.frameworkNegative = 'OBSERVED_INSTALL_FAILURE'
        $report.frameworkNegativeHResult = $_.Exception.HResult
        $report.frameworkNegativeErrorId = $_.FullyQualifiedErrorId
    }
    Add-AppxPackage -Path $PackagePath -DependencyPath @($dependencies.FullName)
    $installed = Get-AppxPackage -Name 'AiUsage.Dev'
    $env:AIU_SMOKE_AUMID = "$($installed.PackageFamilyName)!App"
    if ($runtimeInventory -match '^Microsoft.NETCore.App 10\.') {
        $report.dotnetNegative = 'PREREQUISITE_ALREADY_PRESENT'
    } else {
        $env:AIU_SMOKE_EVIDENCE_DIRECTORY = Join-Path $EvidenceDirectory 'negative-dotnet'
        & $SmokeExecutable -noLogo
        $report.dotnetNegativeExitCode = $LASTEXITCODE
        $attempts = @(Get-ChildItem -LiteralPath $env:AIU_SMOKE_EVIDENCE_DIRECTORY -Filter '*-activation.json' -ErrorAction SilentlyContinue)
        $activationAttempted = @($attempts | ForEach-Object { Get-Content -LiteralPath $_.FullName | ConvertFrom-Json } | Where-Object phase -eq 'activation-attempted').Count -gt 0
        $report.dotnetNegative = if ($LASTEXITCODE -eq 0) { 'NO_MISSING_PREREQUISITE_OBSERVED' } elseif ($activationAttempted) { 'OBSERVED_FAILURE_AFTER_ACTIVATION_ATTEMPT' } else { 'HARNESS_PREREQUISITE_FAILURE' }
    }
    $installer = Start-Process -FilePath $DotNetRuntimeInstaller -ArgumentList '/install', '/quiet', '/norestart' -Wait -PassThru
    $report.runtimeInstallerExitCode = $installer.ExitCode
    if ($installer.ExitCode -ne 0) { throw 'Runtime installer failed or requires reboot; no reboot is authorized.' }
    if (!(Test-Path -LiteralPath $dotnet)) { throw 'Installed runtime host is missing.' }
    $report.provisionedRuntimes = @(& $dotnet --list-runtimes)
    if (!($report.provisionedRuntimes -match '^Microsoft.NETCore.App 10\.')) { throw 'Required framework-dependent .NET 10 runtime was not installed.' }
    $installed = Get-AppxPackage -Name 'AiUsage.Dev'
    if (!$installed -or [string]$installed.Version -ne [string]$manifest.Package.Identity.Version) { throw 'Installed package version mismatch.' }
    $env:AIU_SMOKE_AUMID = "$($installed.PackageFamilyName)!App"
    $report.aumid = $env:AIU_SMOKE_AUMID
    $env:AIU_SMOKE_EVIDENCE_DIRECTORY = Join-Path $EvidenceDirectory 'positive'
    & $SmokeExecutable -noLogo
    $report.smokeExitCode = $LASTEXITCODE
    if ($LASTEXITCODE -ne 0) { $report.positive = 'FAIL'; throw 'Installed app smoke failed.' }
    $report.positive = 'PASS'
    $report.status = if ($report.frameworkNegative -eq 'OBSERVED_INSTALL_FAILURE' -and $report.dotnetNegative -eq 'OBSERVED_FAILURE_AFTER_ACTIVATION_ATTEMPT') { 'PASS_REQUIRES_EVIDENCE_REVIEW' } else { 'BLOCKED_FRESH_NEGATIVE_SNAPSHOT_REQUIRED' }
} catch {
    $report.status = 'FAIL'
    $report.failureType = $_.Exception.GetType().Name
    $report.failureHResult = $_.Exception.HResult
    throw
} finally {
    $report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $EvidenceDirectory 'guest-report.json') -Encoding UTF8
}
$report | ConvertTo-Json -Depth 8
if ($report.status -ne 'PASS_REQUIRES_EVIDENCE_REVIEW') { exit 1 }
