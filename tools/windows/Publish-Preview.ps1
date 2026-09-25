# Runs only in the serialized, successful-main GitHub-hosted release job.
[CmdletBinding()]
param([Parameter(Mandatory)][string]$SiteUrl)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Import-Module "$PSScriptRoot/PreviewRelease.psm1" -Force
Assert-PreviewPublicationRunner
if (!$env:AIU_CI_PFX_BASE64 -or !$env:AIU_CI_PFX_PASSWORD) { throw 'Dedicated Preview signing secrets are not configured.' }
if ($env:GITHUB_SHA -notmatch '^[0-9a-f]{40}$') { throw 'Expected immutable source SHA.' }
if ($SiteUrl -cne 'https://rozputnii.github.io/ai-usage-app') { throw 'Unexpected Preview site.' }
function Invoke-Gh([string[]]$Arguments) {
    $output = & gh @Arguments
    if ($LASTEXITCODE -ne 0) { throw "GitHub operation failed: $($Arguments[0])." }
    return $output
}
$repo = $env:GITHUB_REPOSITORY
$all = @(Invoke-Gh @('api', '--paginate', '--slurp', "repos/$repo/releases?per_page=100") | ConvertFrom-Json | ForEach-Object { $_ } | ForEach-Object { $_ })
$previews = @($all | Where-Object { $_.tag_name.StartsWith('preview-') })
$version = Get-NextPreviewVersion -UtcDate ([datetime]::UtcNow) -ExistingVersions @($previews | ForEach-Object { $_.tag_name.Substring(8) })
$tag = "preview-$version"
$work = Join-Path $env:RUNNER_TEMP "aiu-preview-$env:GITHUB_RUN_ID-$env:GITHUB_RUN_ATTEMPT"
$site = Join-Path $work 'site'
$assets = Join-Path $work 'assets'
New-Item -ItemType Directory -Path $site, $assets -ErrorAction Stop | Out-Null
$notes = Join-Path $work 'notes.md'
@"
Development Preview $version

Source: $env:GITHUB_SHA
Self-signed testing build. One-time explicit certificate trust is required.
Install and automatic-update instructions: $SiteUrl/
"@ | Set-Content -LiteralPath $notes -Encoding utf8
# Draft is the durable reservation: retain it if build/sign/upload fails.
$null = Invoke-Gh @('release','create',$tag,'--repo',$repo,'--target',$env:GITHUB_SHA,'--title',"Development Preview $version",'--notes-file',$notes,'--draft','--prerelease')
$pfxPath = Join-Path $work 'ci-signing.pfx'
$certificate = $null
$trusted = $null
try {
    [IO.File]::WriteAllBytes($pfxPath, [Convert]::FromBase64String($env:AIU_CI_PFX_BASE64))
    $password = ConvertTo-SecureString $env:AIU_CI_PFX_PASSWORD -AsPlainText -Force
    Remove-Item Env:AIU_CI_PFX_BASE64, Env:AIU_CI_PFX_PASSWORD
    $certificate = Import-PfxCertificate -FilePath $pfxPath -Password $password -CertStoreLocation Cert:\CurrentUser\My
    if (@($certificate).Count -ne 1 -or $certificate.Subject -ne 'CN=AI Usage Development') { throw 'Unexpected CI certificate.' }
    $cerPath = Join-Path $assets 'AiUsage.Development.cer'
    Export-Certificate -Cert $certificate -FilePath $cerPath | Out-Null
    Remove-Item -LiteralPath $pfxPath
    # Only this disposable runner trusts the public CER, so SignTool can verify before upload.
    $trusted = Add-PreviewRunnerTrust -CertificatePath $cerPath -SignerThumbprint $certificate.Thumbprint
    & "$PSScriptRoot/Build-Package.ps1" -MsixVersion $version -CertificateThumbprint $certificate.Thumbprint -OutputDirectory (Join-Path $work 'packages')
    if ($LASTEXITCODE -ne 0) { throw 'Signed package build failed.' }
    $packages = @(Get-ChildItem -LiteralPath (Join-Path $work 'packages') -Recurse -Filter 'AiUsage*.msix' -File)
    if ($packages.Count -ne 1) { throw 'Expected one signed product package.' }
    $package = $packages[0]
    Copy-Item -LiteralPath $package.FullName -Destination $assets
    # App Installer downloads from the site itself: GitHub release downloads redirect to short-lived storage URLs
    # that App Installer fails to fetch (0x80072EFE). The release keeps its own immutable copy.
    Copy-Item -LiteralPath $package.FullName -Destination $site
    $dependencyFiles = @(Get-ChildItem -LiteralPath (Join-Path $package.DirectoryName 'Dependencies/x64') -File | Where-Object { $_.Extension -in '.msix','.appx' })
    $dependencies = @()
    foreach ($file in $dependencyFiles) {
        Copy-Item -LiteralPath $file.FullName -Destination $assets
        Copy-Item -LiteralPath $file.FullName -Destination $site
        $zip = [IO.Compression.ZipFile]::OpenRead($file.FullName)
        try {
            $reader = [IO.StreamReader]::new($zip.GetEntry('AppxManifest.xml').Open())
            try { [xml]$manifest = $reader.ReadToEnd() } finally { $reader.Dispose() }
        } finally { $zip.Dispose() }
        $identity = $manifest.Package.Identity
        $dependencies += @{ Name=[string]$identity.Name; Publisher=[string]$identity.Publisher; Version=[string]$identity.Version; ProcessorArchitecture=[string]$identity.ProcessorArchitecture; Uri="$SiteUrl/$($file.Name)" }
    }
    $feed = New-PreviewFeed -Version $version -FeedUri "$SiteUrl/AiUsage.appinstaller" -PackageUri "$SiteUrl/$($package.Name)" -Dependencies $dependencies
    [IO.File]::WriteAllText((Join-Path $site 'AiUsage.appinstaller'), $feed)
    Copy-Item -LiteralPath $cerPath -Destination $site
    $evidence = [ordered]@{ version=$version; commit=$env:GITHUB_SHA; run=$env:GITHUB_RUN_ID; sha256=(Get-FileHash $package.FullName -Algorithm SHA256).Hash; certificateThumbprint=$certificate.Thumbprint; signed='PASS'; interactiveSmoke='NOT_RUN'; trust='development-only' }
    $evidence | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $assets 'release.json') -Encoding utf8
    Copy-Item -LiteralPath (Join-Path $assets 'release.json') -Destination $site
    $index = [IO.File]::ReadAllText("$PSScriptRoot/preview-index.html").Replace('{{PACKAGE}}', $package.Name).Replace('{{VERSION}}', $version)
    [IO.File]::WriteAllText((Join-Path $site 'index.html'), $index, [Text.UTF8Encoding]::new($false))
    Copy-Item -LiteralPath (Join-Path $site 'AiUsage.appinstaller') -Destination $assets
    # Never use --clobber: released bytes are immutable under this workflow.
    foreach ($file in Get-ChildItem -LiteralPath $assets -File) {
        $null = Invoke-Gh @('release','upload',$tag,$file.FullName,'--repo',$repo)
    }
    $null = Invoke-Gh @('release','edit',$tag,'--repo',$repo,'--draft=false','--prerelease','--latest=false')
    # Include every published source: an older commit's later retry must not become
    # eligible merely because its preceding non-promoted build has a larger version.
    $promote = Test-PreviewPromotion -Candidate $env:GITHUB_SHA -PublishedCommits @($previews | Where-Object { !$_.draft } | ForEach-Object { $_.target_commitish })
    "site=$site" >> $env:GITHUB_OUTPUT
    "promote=$($promote.ToString().ToLowerInvariant())" >> $env:GITHUB_OUTPUT
    "version=$version" >> $env:GITHUB_OUTPUT
    Write-Output "Published development Preview $version; promote feed: $promote"
} finally {
    if (Test-Path -LiteralPath $pfxPath) { Remove-Item -LiteralPath $pfxPath }
    try { if ($trusted) { Remove-PreviewRunnerTrust -Thumbprint $trusted } }
    finally { if ($certificate) { Remove-Item -LiteralPath "Cert:\CurrentUser\My\$($certificate.Thumbprint)" -DeleteKey } }
}
