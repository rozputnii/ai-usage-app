# Explicit one-time owner setup. Creates a NEW key; never reads an existing key.
[CmdletBinding()]
param([switch]$Apply)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if (!$Apply) { throw 'Review this script and use -Apply only after authorizing dedicated CI key provisioning and Pages activation.' }
$repo = 'rozputnii/ai-usage-app'
$actual = & gh repo view --json nameWithOwner --jq .nameWithOwner
if ($LASTEXITCODE -ne 0 -or $actual -cne $repo) { throw 'Run from the owned repository with authenticated GitHub CLI.' }
$names = @(& gh secret list --repo $repo --json name --jq '.[].name')
if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect existing secret names.' }
if ($names -contains 'AIU_CI_PFX_BASE64' -or $names -contains 'AIU_CI_PFX_PASSWORD') { throw 'Preview signing already exists; certificate rotation requires a separate plan.' }
function Set-Secret([string]$Name, [string]$Value) {
    $start = [Diagnostics.ProcessStartInfo]::new((Get-Command gh).Source)
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardInput = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    foreach ($argument in @('secret','set',$Name,'--repo',$repo)) { $start.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::Start($start)
    try {
        $process.StandardInput.Write($Value)
        $process.StandardInput.Close()
        $output = $process.StandardOutput.ReadToEndAsync()
        $errorOutput = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        $null = $output.GetAwaiter().GetResult()
        $null = $errorOutput.GetAwaiter().GetResult()
        if ($process.ExitCode -ne 0) { throw 'GitHub secret provisioning failed; publication remains disabled.' }
    } finally { $process.Dispose() }
}
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$outputDirectory = Join-Path $root '.ai-usage-local/AIU-014/signing'
if (Test-Path -LiteralPath $outputDirectory) { throw 'Setup evidence directory already exists; inspect prior provisioning before retry.' }
New-Item -ItemType Directory -Path $outputDirectory | Out-Null
$rsa = [Security.Cryptography.RSA]::Create(3072)
$certificate = $null
try {
    $request = [Security.Cryptography.X509Certificates.CertificateRequest]::new('CN=AI Usage Development', $rsa, [Security.Cryptography.HashAlgorithmName]::SHA256, [Security.Cryptography.RSASignaturePadding]::Pkcs1)
    $request.CertificateExtensions.Add([Security.Cryptography.X509Certificates.X509BasicConstraintsExtension]::new($false, $false, 0, $true))
    $request.CertificateExtensions.Add([Security.Cryptography.X509Certificates.X509KeyUsageExtension]::new([Security.Cryptography.X509Certificates.X509KeyUsageFlags]::DigitalSignature, $true))
    $oids = [Security.Cryptography.OidCollection]::new()
    $null = $oids.Add([Security.Cryptography.Oid]::new('1.3.6.1.5.5.7.3.3'))
    $request.CertificateExtensions.Add([Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension]::new($oids, $true))
    $certificate = $request.CreateSelfSigned([DateTimeOffset]::UtcNow.AddMinutes(-5), [DateTimeOffset]::UtcNow.AddYears(2))
    $password = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
    $pfx = $certificate.Export([Security.Cryptography.X509Certificates.X509ContentType]::Pfx, $password)
    [IO.File]::WriteAllBytes((Join-Path $outputDirectory 'AiUsage.Development.cer'), $certificate.Export([Security.Cryptography.X509Certificates.X509ContentType]::Cert))
    # The only exported private key is this new in-memory CI key. No local private
    # key file or Windows certificate-store entry is created by this script.
    Set-Secret 'AIU_CI_PFX_BASE64' ([Convert]::ToBase64String($pfx))
    Set-Secret 'AIU_CI_PFX_PASSWORD' $password
    [Array]::Clear($pfx, 0, $pfx.Length)
    $password = $null
    # Fail closed if either secret failed. Enabling is deliberately the final step.
    & gh api --method POST "repos/$repo/pages" -f build_type=workflow | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Pages activation failed; inspect configuration before enabling publication.' }
    & gh variable set AIU_PREVIEW_ENABLED --repo $repo --body true
    if ($LASTEXITCODE -ne 0) { throw 'Could not enable Preview publication.' }
    [ordered]@{ thumbprint=$certificate.Thumbprint; expires=$certificate.NotAfter.ToUniversalTime().ToString('o'); repository=$repo; pages='https://rozputnii.github.io/ai-usage-app'; hostTrustChanged=$false } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputDirectory 'setup.json') -Encoding utf8
    Write-Output "Preview signing configured. Public certificate thumbprint: $($certificate.Thumbprint). Host trust unchanged."
} finally {
    if ($certificate) { $certificate.Dispose() }
    $rsa.Dispose()
}
