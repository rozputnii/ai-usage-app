$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Import-Module "$PSScriptRoot/../../tools/windows/PreviewRelease.psm1" -Force
$script:count = 0
function Equal($actual, $expected) { if ($actual -cne $expected) { throw "Expected '$expected', got '$actual'." }; $script:count++ }
function Reject([scriptblock] $action) { $rejected = $false; try { & $action | Out-Null } catch { $rejected = $true }; Equal $rejected $true }
$today = [datetime]::SpecifyKind([datetime]'2026-09-22', [DateTimeKind]::Utc)
Equal (Get-NextPreviewVersion -UtcDate $today -ExistingVersions @()) '2026.9.2223.0'
Equal (Get-NextPreviewVersion -UtcDate $today -ExistingVersions @('2026.9.2230.0','2026.9.2229.0')) '2026.9.2231.0'
Equal (Get-NextPreviewVersion -UtcDate $today.AddDays(1) -ExistingVersions @('2026.9.2299.0')) '2026.9.2301.0'
Equal (Get-NextPreviewVersion -UtcDate ([datetime]'2027-01-01Z') -ExistingVersions @('2026.12.3199.0')) '2027.1.101.0'
Reject { Get-NextPreviewVersion -UtcDate $today -ExistingVersions @('2026.9.2299.0') }
Reject { Get-NextPreviewVersion -UtcDate $today -ExistingVersions @('2026.9.2301.0') }
Reject { Get-NextPreviewVersion -UtcDate $today -ExistingVersions @('2026.9.2200.0') }
Reject { Get-NextPreviewVersion -UtcDate $today -ExistingVersions @('2026.09.2223.0') }
$args = @{ Version = '2026.9.2223.0'; FeedUri = 'https://example.org/preview/AiUsage.appinstaller'; PackageUri = 'https://example.org/releases/AiUsage.msix'; Dependencies = @(@{ Name='Microsoft.Example'; Publisher='CN=Microsoft & Example'; Version='1.0.0.0'; ProcessorArchitecture='x64'; Uri='https://example.org/releases/framework.msix' }) }
[xml]$feed = New-PreviewFeed @args
Equal $feed.AppInstaller.Uri $args.FeedUri
Equal $feed.AppInstaller.MainPackage.Name 'AiUsage.Dev'
Equal $feed.AppInstaller.MainPackage.Publisher 'CN=AI Usage Development'
Equal $feed.AppInstaller.MainPackage.Version $args.Version
Equal $feed.AppInstaller.Dependencies.Package.Publisher 'CN=Microsoft & Example'
Equal $feed.AppInstaller.UpdateSettings.OnLaunch.HoursBetweenUpdateChecks '0'
Equal $feed.AppInstaller.UpdateSettings.OnLaunch.UpdateBlocksActivation 'false'
Equal $feed.AppInstaller.UpdateSettings.ForceUpdateFromAnyVersion 'false'
Equal ($null -ne $feed.AppInstaller.UpdateSettings.AutomaticBackgroundTask) $true
$args.PackageUri = 'http://untrusted.example/package.msix'
Reject { New-PreviewFeed @args }
$args.PackageUri = 'https://example.org/releases/AiUsage.msix'
$args.Dependencies[0].Uri = 'file:///C:/untrusted.msix'
Reject { New-PreviewFeed @args }
Write-Output "PASS: $script:count release policy assertions"
# Runner trust: WinVerifyTrust (SignTool /pa, Get-AuthenticodeSignature) anchors a self-signed
# chain only in a Root store; TrustedPeople never satisfies it, and CurrentUser\Root prompts.
# These checks never open a store and never change trust on the machine running them.
$trustStore = & (Get-Module PreviewRelease) { New-PreviewRunnerTrustStore }
Equal $trustStore.Name 'Root'
Equal ([string]$trustStore.Location) 'LocalMachine'
$trustStore.Dispose()
$savedJob = $env:GITHUB_JOB
try {
    $env:GITHUB_JOB = 'validate'
    Reject { Add-PreviewRunnerTrust -CertificatePath (Join-Path $PSScriptRoot 'missing.cer') -SignerThumbprint ('A' * 40) }
    Reject { Remove-PreviewRunnerTrust -Thumbprint ('A' * 40) }
} finally { $env:GITHUB_JOB = $savedJob }
$publish = [Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot '../../tools/windows/Publish-Preview.ps1'), [ref]$null, [ref]$null)
Equal ($publish.Extent.Text -match 'TrustedPeople|CurrentUser\\Root|Import-Certificate') $false
$finally = @($publish.FindAll({ param($node) $node -is [Management.Automation.Language.TryStatementAst] -and $node.Finally }, $true))
Equal (@($finally | Where-Object { $_.Body.Extent.Text -match 'Add-PreviewRunnerTrust' -and $_.Finally.Extent.Text -match 'Remove-PreviewRunnerTrust' }).Count) 1
$head = git rev-parse HEAD
if ($LASTEXITCODE -ne 0) { throw 'Release ancestry tests require a Git checkout.' }
$parent = git rev-parse HEAD~1
if ($LASTEXITCODE -ne 0) { throw 'Release ancestry tests require checkout depth >= 2.' }
Equal (Test-PreviewPromotion -Candidate $head -PublishedCommits @($parent)) $true
Equal (Test-PreviewPromotion -Candidate $parent -PublishedCommits @($head, $parent)) $false
Equal (Test-PreviewPromotion -Candidate $head -PublishedCommits @($head)) $true
Reject { Test-PreviewPromotion -Candidate ('0' * 40) -PublishedCommits @($head) }
Write-Output "PASS: $script:count release policy and source ancestry assertions"
# Expected negative native Git checks are assertions, not the script's exit status.
exit 0
