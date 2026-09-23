[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $MsixVersion,
    [string] $CertificateThumbprint,
    [string] $OutputDirectory = '.ai-usage-local/AIU-002/packages',
    [switch] $NoRestore
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if ($MsixVersion -notmatch '^(\d{4})\.(\d{1,2})\.(\d{3,4})\.0$') { throw 'Expected YYYY.M.DDNN.0 with NN 01..99.' }
$year = [int]$Matches[1]; $month = [int]$Matches[2]; $dayCounter = [int]$Matches[3]
$day = [int][Math]::Floor($dayCounter / 100); $counter = $dayCounter % 100
if ($year -lt 2000 -or $month -lt 1 -or $month -gt 12 -or $counter -lt 1 -or $day -lt 1 -or $day -gt [DateTime]::DaysInMonth($year, $month)) { throw 'Invalid calendar date or build counter.' }
if ($MsixVersion -ne "$year.$month.$dayCounter.0") { throw 'Version must use canonical numeric components without leading zeros.' }
$sdk = Join-Path $HOME '.dotnet/ai-usage-sdk/dotnet.exe'
if (Test-Path -LiteralPath $sdk) { $env:DOTNET_ROOT = Split-Path $sdk; $env:PATH = "$env:DOTNET_ROOT;$env:PATH" }
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
if (!(Test-Path -LiteralPath $vswhere)) { throw 'Visual Studio Installer vswhere is required.' }
$msbuild = @(& $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\Current\Bin\MSBuild.exe')
if ($LASTEXITCODE -ne 0 -or $msbuild.Count -ne 1) { throw 'Exactly one latest VS MSBuild is required.' }
$certificate = $null
if ($CertificateThumbprint) {
    if ($CertificateThumbprint -notmatch '^[0-9a-fA-F]{40}$') { throw 'Expected exact certificate SHA-1 thumbprint.' }
    $certificate = Get-Item -LiteralPath "Cert:\CurrentUser\My\$CertificateThumbprint"
    if ($certificate.Subject -ne 'CN=AI Usage Development' -or !$certificate.HasPrivateKey -or $certificate.NotAfter -le (Get-Date) -or $certificate.NotBefore -gt (Get-Date) -or '1.3.6.1.5.5.7.3.3' -notin @($certificate.EnhancedKeyUsageList | ForEach-Object { [string]$_.ObjectId })) { throw 'Selected certificate is not a valid owned development code-signing identity.' }
}
$output = if ([IO.Path]::IsPathRooted($OutputDirectory)) { [IO.Path]::GetFullPath($OutputDirectory) } else { [IO.Path]::GetFullPath((Join-Path $root $OutputDirectory)) }
$versionOutput = Join-Path $output $MsixVersion
if (Test-Path -LiteralPath $versionOutput) { throw 'Version output already exists; reserve a fresh version. Previous bytes are retained.' }
New-Item -ItemType Directory -Path $versionOutput | Out-Null
[xml]$sourceManifest = Get-Content -LiteralPath (Join-Path $root 'src/windows/AiUsage.Windows/Package.appxmanifest')
$sourceManifest.Package.Identity.Version = $MsixVersion
$buildManifest = Join-Path $versionOutput 'Package.appxmanifest'
$sourceManifest.Save($buildManifest)
$report = [ordered]@{ version = $MsixVersion; identity = 'AiUsage.Dev'; publisher = 'CN=AI Usage Development'; status = 'unsigned-build-incomplete'; package = $null; sha256 = $null; utc = [DateTime]::UtcNow.ToString('o') }
try {
    $restoreArguments = @(if (!$NoRestore) { '/restore' })
    & $msbuild[0] (Join-Path $root 'src/windows/AiUsage.Windows/AiUsage.Windows.csproj') @restoreArguments /v:minimal /p:Configuration=Release /p:Platform=x64 /p:RuntimeIdentifier=win-x64 /p:GenerateAppxPackageOnBuild=true /p:AppxBundle=Never /p:UapAppxPackageBuildMode=SideloadOnly /p:AppxPackageSigningEnabled=false "/p:AppxPackageVersion=$MsixVersion" "/p:AiUsagePackageManifest=$buildManifest" "/p:AppxPackageDir=$versionOutput/"
    if ($LASTEXITCODE -ne 0) { throw "MSBuild failed with exit $LASTEXITCODE." }
    $packages = @(Get-ChildItem -LiteralPath $versionOutput -Recurse -File -Filter '*.msix' | Where-Object { $_.Name -like 'AiUsage*' })
    if ($packages.Count -ne 1) { throw 'Expected exactly one product MSIX.' }
    $package = $packages[0].FullName
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($package)
    try {
        $reader = New-Object IO.StreamReader($archive.GetEntry('AppxManifest.xml').Open())
        try { [xml]$actualManifest = $reader.ReadToEnd() } finally { $reader.Dispose() }
    } finally { $archive.Dispose() }
    if ($actualManifest.Package.Identity.Name -ne 'AiUsage.Dev' -or $actualManifest.Package.Identity.Publisher -ne 'CN=AI Usage Development' -or $actualManifest.Package.Identity.Version -ne $MsixVersion) { throw 'Produced MSIX identity/version does not match the reserved build.' }
    $report.package = $packages[0].Name
    $report.status = 'unsigned-validation-only'
    if ($certificate) {
        $nuget = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $HOME '.nuget/packages' }
        $signtool = Join-Path $nuget 'microsoft.windows.sdk.buildtools/10.0.26100.4654/bin/10.0.26100.0/x64/signtool.exe'
        if (!(Test-Path -LiteralPath $signtool)) { throw 'Pinned x64 SignTool is missing.' }
        & $signtool sign /fd SHA256 /sha1 $CertificateThumbprint /s My $package
        if ($LASTEXITCODE -ne 0) { throw "Signing failed with exit $LASTEXITCODE; artifact is not installable evidence." }
        Export-Certificate -Cert $certificate -FilePath (Join-Path $versionOutput 'AiUsage.Development.cer') -Type CERT | Out-Null
        $report.sha256 = (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash
        # Fail closed on untrusted roots too. Only a disposable guest or the hosted Preview runner
        # supplies root trust for the public CER; this command never imports trust itself.
        & $signtool verify /pa /v $package
        $report.signtoolVerifyExit = $LASTEXITCODE
        if ($LASTEXITCODE -ne 0) { throw 'SignTool verification failed; retained signed bytes require guest verification and are not installable success evidence.' }
        $signature = Get-AuthenticodeSignature -LiteralPath $package
        if (!$signature.SignerCertificate -or $signature.SignerCertificate.Thumbprint -ne $CertificateThumbprint -or $signature.Status -ne 'Valid') { throw 'Signed package certificate verification failed.' }
        $report.status = 'signed-verified'
    }
    $report.sha256 = (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash
} catch {
    $report.status = 'failed-not-installable'
    throw
} finally {
    $report | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $versionOutput 'package-evidence.json') -Encoding UTF8
}
$report | ConvertTo-Json
