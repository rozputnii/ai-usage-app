$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Import-Module "$PSScriptRoot/../../tools/windows/PreviewRelease.psm1" -Force
$script:count = 0
function Equal($actual, $expected) { if ($actual -cne $expected) { throw "Expected '$expected', got '$actual'." }; $script:count++ }
function Reject([scriptblock] $action, [string] $because = '') { $rejected = $false; try { & $action | Out-Null } catch { $rejected = $_.Exception.Message -like "*$because*" }; Equal $rejected $true }
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
# ANL-12 (2026-10-06): the feed stays on Pages; packages come from the immutable release assets.
$release = 'https://github.com/rozputnii/ai-usage-app/releases/download/preview-2026.10.601.0'
$pagesFeed = 'https://rozputnii.github.io/ai-usage-app/AiUsage.appinstaller'
[xml]$releaseFeed = New-PreviewFeed -Version '2026.10.601.0' -FeedUri $pagesFeed -PackageUri "$release/AiUsage.Windows_2026.10.601.0_x64.msix" -Dependencies @(@{ Name='Microsoft.Example'; Publisher='CN=Microsoft Example'; Version='1.0.0.0'; ProcessorArchitecture='x64'; Uri="$release/Microsoft.WindowsAppRuntime.2.msix" })
Equal $releaseFeed.AppInstaller.Uri $pagesFeed
Equal $releaseFeed.AppInstaller.MainPackage.Uri "$release/AiUsage.Windows_2026.10.601.0_x64.msix"
Equal $releaseFeed.AppInstaller.Dependencies.Package.Uri "$release/Microsoft.WindowsAppRuntime.2.msix"
Write-Output "PASS: $script:count release policy assertions"
# Runner trust: hosted run 35785324979 proved CurrentUser\TrustedPeople does not satisfy SignTool
# /pa for the self-signed signer; a Root store anchors it, and CurrentUser\Root would prompt.
# These checks never open a store and never change trust on the machine running them.
$trustStore = & (Get-Module PreviewRelease) { New-PreviewRunnerTrustStore }
Equal $trustStore.Name 'Root'
Equal ([string]$trustStore.Location) 'LocalMachine'
$trustStore.Dispose()
$savedJob = $env:GITHUB_JOB
try {
    $env:GITHUB_JOB = 'validate'
    Reject { Add-PreviewRunnerTrust -CertificatePath (Join-Path $PSScriptRoot 'missing.cer') -SignerThumbprint ('A' * 40) } 'owner dispatch'
    Reject { Remove-PreviewRunnerTrust -Thumbprint ('A' * 40) } 'owner dispatch'
} finally { $env:GITHUB_JOB = $savedJob }
$publish = [Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot '../../tools/windows/Publish-Preview.ps1'), [ref]$null, [ref]$null)
Equal ($publish.Extent.Text -match 'TrustedPeople|CurrentUser\\Root|Import-Certificate') $false
$finally = @($publish.FindAll({ param($node) $node -is [Management.Automation.Language.TryStatementAst] -and $node.Finally }, $true))
Equal (@($finally | Where-Object { $_.Body.Extent.Text -match 'Add-PreviewRunnerTrust' -and $_.Finally.Extent.Text -match 'Remove-PreviewRunnerTrust' }).Count) 1
# ANL-12 (2026-10-06): package and dependency URIs use the owned repository's immutable release
# assets (IPv4-only hosts); the release is public before Pages deploys.
# 2026-10-07 owner decision: the feed address moves off Pages (its IPv6 path reset App Installer with
# 0x80072EFE) to the moving `feed-preview` release on github.com, the only asset ever replaced.
$publishText = $publish.Extent.Text
Equal ($publishText -match '(?m)^\$repo = \$env:GITHUB_REPOSITORY\r?\nif \(\$repo -cne ''rozputnii/ai-usage-app''\) \{ throw ') $true
Equal ($publishText -match '(?m)^\$feedUri = "https://github\.com/\$repo/releases/download/feed-preview/AiUsage\.appinstaller"\r?$') $true
Equal ($publishText -match '-FeedUri \$feedUri ') $true
Equal ($publishText -match '-FeedUri "\$SiteUrl') $false
Equal ([regex]::Matches($publishText, '--clobber').Count) 1
Equal ($publishText -match "'release','upload','feed-preview',[^\r\n]*'--clobber'") $true
Equal ('feed-preview'.StartsWith('preview-')) $false
$feedUpload = $publishText.IndexOf("'release','upload','feed-preview'")
Equal ($feedUpload -gt $publishText.IndexOf("'release','edit',`$tag")) $true
$promoteBlock = $publishText.IndexOf('if ($promote) {')
Equal ($promoteBlock -gt $publishText.IndexOf('$promote = Test-PreviewPromotion') -and $promoteBlock -lt $feedUpload) $true
Equal ($publishText -match '-PackageUri "https://github\.com/\$repo/releases/download/\$tag/\$\(\$package\.Name\)"') $true
Equal ($publishText -match 'Uri="https://github\.com/\$repo/releases/download/\$tag/\$\(\$file\.Name\)" \}') $true
Equal ($publishText -match '(?:-PackageUri |Uri=)"\$SiteUrl/\$\(\$(?:package|file)\.Name\)"') $false
Equal ($publishText -match 'Copy-Item -LiteralPath \$(?:package|file)\.FullName -Destination \$site') $false
Equal ($publishText -match "\.Replace\('\{\{TAG\}\}', \`$tag\)") $true
$indexPage = [IO.File]::ReadAllText((Join-Path $PSScriptRoot '../../tools/windows/preview-index.html'))
$releaseLinks = 'https://github\.com/rozputnii/ai-usage-app/releases/download/\{\{TAG\}\}/'
Equal ($indexPage -match "href=""$($releaseLinks)\{\{PACKAGE\}\}""") $true
Equal ($indexPage -match "href=""$($releaseLinks)Microsoft\.WindowsAppRuntime\.2\.msix""") $true
Equal ($indexPage -match "href=""(?!$releaseLinks)[^""]*(?:\.msix|\{\{PACKAGE\}\})""") $false
Equal ($indexPage -match 'href="https://github\.com/rozputnii/ai-usage-app/releases/download/feed-preview/AiUsage\.appinstaller"' -and $indexPage -match 'href="AiUsage\.Development\.cer"') $true
Equal ($indexPage -match 'href="AiUsage\.appinstaller"') $false
$madePublic =$publishText.IndexOf("'release','edit',`$tag,'--repo',`$repo,'--draft=false'")
Equal ($madePublic -gt 0 -and $madePublic -lt $publishText.IndexOf('"site=$site" >> $env:GITHUB_OUTPUT')) $true
$head = git rev-parse HEAD
if ($LASTEXITCODE -ne 0) { throw 'Release ancestry tests require a Git checkout.' }
$parent = git rev-parse HEAD~1
if ($LASTEXITCODE -ne 0) { throw 'Release ancestry tests require checkout depth >= 2.' }
Equal (Test-PreviewPromotion -Candidate $head -PublishedCommits @($parent)) $true
Equal (Test-PreviewPromotion -Candidate $parent -PublishedCommits @($head, $parent)) $false
Equal (Test-PreviewPromotion -Candidate $head -PublishedCommits @($head)) $true
Reject { Test-PreviewPromotion -Candidate ('0' * 40) -PublishedCommits @($head) }
# A green main push can publish (filtered by OD-17 below); an owner dispatch can republish (owner decision 2026-10-06, reverses AIU045-D1).
$runnerNames = 'GITHUB_ACTIONS', 'GITHUB_EVENT_NAME', 'GITHUB_REF', 'GITHUB_JOB', 'RUNNER_ENVIRONMENT', 'GITHUB_REPOSITORY'
$savedRunner = @{}
foreach ($name in $runnerNames) { $savedRunner[$name] = [Environment]::GetEnvironmentVariable($name) }
try {
    $env:GITHUB_ACTIONS = 'true'; $env:GITHUB_REF = 'refs/heads/main'; $env:GITHUB_JOB = 'preview'
    $env:RUNNER_ENVIRONMENT = 'github-hosted'; $env:GITHUB_REPOSITORY = 'rozputnii/ai-usage-app'
    $env:GITHUB_EVENT_NAME = 'pull_request'
    Reject { Assert-PreviewPublicationRunner } 'main push or owner dispatch'
    $env:GITHUB_EVENT_NAME = 'push'
    Assert-PreviewPublicationRunner; $script:count++
    $env:GITHUB_EVENT_NAME = 'workflow_dispatch'
    Assert-PreviewPublicationRunner; $script:count++
} finally { foreach ($name in $runnerNames) { [Environment]::SetEnvironmentVariable($name, $savedRunner[$name]) } }
$workflow = [IO.File]::ReadAllText((Join-Path $PSScriptRoot '../../.github/workflows/validation.yml'))
$previewIf = [regex]::Match($workflow, '(?m)^  preview:\r?\n    if: ([^\r\n]+)').Groups[1].Value
foreach ($term in @("(github.event_name == 'push' || github.event_name == 'workflow_dispatch' && inputs.PublishPreview == true)", "github.ref == 'refs/heads/main'", "vars.AIU_PREVIEW_ENABLED == 'true'")) { Equal ($previewIf.Contains($term)) $true }
Equal ($previewIf.Contains('pull_request')) $false
Equal ($workflow -match '(?m)^  preview:\r?\n    if: [^\r\n]+\r?\n    needs: \[validate, windows-package\]\r?$') $true
$publishStep = $workflow.IndexOf('./tools/windows/Publish-Preview.ps1')
Equal ($publishStep -gt 0 -and $publishStep -lt $workflow.IndexOf('- name: Deploy update feed')) $true
$dispatch = [regex]::Match($workflow, '(?m)^  workflow_dispatch:\r?\n(?:(?:    [^\r\n]*)?\r?\n)+').Value
$publishInput = [regex]::Match($dispatch, '(?m)^      PublishPreview:\r?\n(?:        [^\r\n]*\r?\n)+').Value
Equal ($publishInput -match '(?m)^        type: boolean\r?$') $true
Equal ($publishInput -match '(?m)^        default: false\r?$') $true
$versionInput = [regex]::Match($dispatch, '(?m)^      MsixVersion:\r?\n(?:        [^\r\n]*\r?\n)+').Value
Equal ($versionInput -match '(?m)^        required: false\r?$') $true
# OD-17 (2026-10-09): a main push publishes unless every path changed since the last published
# Preview is on the non-product allowlist; the same "product inputs" decide the launch-smoke gate.
Equal (Test-PreviewInputsChanged -ChangedPaths @('docs/backlog.md', '.claude/settings.json', '.agents/skills/x/SKILL.md', 'AGENTS.md', 'README.md')) $false
Equal (Test-PreviewInputsChanged -ChangedPaths @()) $false
Equal (Test-PreviewInputsChanged -ChangedPaths @('docs/backlog.md', 'src/windows/AiUsage.Windows/App.xaml')) $true
foreach ($path in @('Directory.Packages.props', 'global.json', '.github/workflows/validation.yml', 'tools/windows/Build-Package.ps1', 'tests/windows/X.cs', 'LICENSE', 'src/README.md', 'docs', '.gitignore')) {
    Equal (Test-PreviewInputsChanged -ChangedPaths @($path)) $true
}
# The decide step fails open: a dispatch, a missing or unknown served build, a non-ancestor and any
# lookup or diff failure publish; only a known ancestor with non-product changes skips. Reasons
# never carry exception text.
$sha = 'a' * 40
$docsOnly = { @('docs/backlog.md') }
$isAncestor = { param($base) $base -ceq $sha }
function Decide([string]$eventName, [scriptblock]$served, [scriptblock]$ancestor, [scriptblock]$paths) {
    Get-PreviewPublicationDecision -EventName $eventName -GetServedCommit $served -TestAncestor $ancestor -GetChangedPaths $paths
}
$decision = Decide 'workflow_dispatch' { throw 'unused' } $isAncestor $docsOnly
Equal $decision.Publish $true
Equal $decision.Reason 'owner dispatch'
Equal (Decide 'push' { $sha } $isAncestor $docsOnly).Publish $false
Equal (Decide 'push' { $sha } $isAncestor { @() }).Publish $false
Equal (Decide 'push' { $sha } $isAncestor { @('docs/backlog.md', 'src/x.cs') }).Publish $true
Equal (Decide 'push' { '' } $isAncestor $docsOnly).Publish $true
Equal (Decide 'push' { 'main' } $isAncestor $docsOnly).Publish $true
Equal (Decide 'push' { 'b' * 40 } $isAncestor $docsOnly).Publish $true
foreach ($failing in @(
    @({ throw 'secret-feed' }, $isAncestor, $docsOnly),
    @({ $sha }, { throw 'secret-ancestry' }, $docsOnly),
    @({ $sha }, $isAncestor, { throw 'secret-diff' }))) {
    $decision = Decide 'push' $failing[0] $failing[1] $failing[2]
    Equal $decision.Publish $true
    Equal ($decision.Reason -match 'secret') $false
}
$decide = [regex]::Match($workflow, '(?ms)^      - name: Decide Preview publication\r?\n.*?(?=^      - )').Value
Equal ($decide -match 'Get-PreviewPublicationDecision') $true
Equal ($decide -match '(?m)^\s+\} catch \{ \$publish = \$true; \$reason = ''[^'']+'' \}\r?$') $true
$previewJob = [regex]::Match($workflow, '(?ms)^  preview:\r?\n.*?(?=^  [a-z]|\z)').Value
Equal ($previewJob -match '(?m)^          fetch-depth: 0\r?$') $true
Equal ($decide -match 'GITHUB_STEP_SUMMARY') $true
$publishBlock = [regex]::Match($workflow, '(?ms)^      - name: Reserve, build, sign and publish development Preview\r?\n.*?(?=^      - )').Value
Equal ($publishBlock -match "(?m)^        if: steps\.decide\.outputs\.publish == 'true'\r?$") $true
Equal ($publishBlock -match 'Publish-Preview\.ps1') $true
Equal ($decide -match 'git diff --no-renames --name-only') $true
Equal ($decide -match 'release download feed-preview') $true
Equal (Test-PreviewInputsChanged -ChangedPaths @('Docs/x.cs')) $true
Equal (Test-PreviewInputsChanged -ChangedPaths @('NOTES.MD')) $false
Equal ($workflow -match 'AIU-002-routing') $false
Write-Output "PASS: $script:count release policy and source ancestry assertions"
# Expected negative native Git checks are assertions, not the script's exit status.
exit 0
