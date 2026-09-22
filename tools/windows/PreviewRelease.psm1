Set-StrictMode -Version Latest
function Get-NextPreviewVersion {
    param([datetime]$UtcDate, [string[]]$ExistingVersions)
    $date = $UtcDate.ToUniversalTime().Date
    $maximum = [version]'2026.9.2222.0'
    foreach ($value in $ExistingVersions) {
        if ($value -notmatch '^(\d{4})\.(\d{1,2})\.(\d{3,4})\.0$') { throw 'Invalid reserved Preview version.' }
        $parsed = [version]$value
        $day = [int][Math]::Floor($parsed.Build / 100)
        if ($parsed.ToString() -cne $value -or $parsed.Major -lt 2000 -or $parsed.Minor -lt 1 -or $parsed.Minor -gt 12 -or
            $day -lt 1 -or $day -gt [datetime]::DaysInMonth($parsed.Major, $parsed.Minor) -or $parsed.Build % 100 -eq 0) {
            throw 'Invalid reserved Preview calendar version.'
        }
        if ($parsed -gt $maximum) { $maximum = $parsed }
    }
    $lastDate = [datetime]::new($maximum.Major, $maximum.Minor, [int][Math]::Floor($maximum.Build / 100))
    if ($date -lt $lastDate) { throw 'UTC clock precedes the reserved version floor.' }
    $counter = if ($date -eq $lastDate) { $maximum.Build % 100 + 1 } else { 1 }
    if ($counter -gt 99) { throw 'Daily version counter exhausted; wait for the next UTC day.' }
    return '{0}.{1}.{2}.0' -f $date.Year, $date.Month, ($date.Day * 100 + $counter)
}
function New-PreviewFeed {
    param([string]$Version, [string]$FeedUri, [string]$PackageUri, [object[]]$Dependencies)
    $null = [version]$Version
    foreach ($value in @($FeedUri, $PackageUri) + @($Dependencies | ForEach-Object { $_.Uri })) {
        $uri = [uri]$value
        if (!$uri.IsAbsoluteUri -or $uri.Scheme -ne 'https' -or $uri.UserInfo -or $uri.Fragment) { throw 'Release URIs must use absolute HTTPS without credentials or fragments.' }
    }
    $document = [xml]'<AppInstaller xmlns="http://schemas.microsoft.com/appx/appinstaller/2021" />'
    $root = $document.DocumentElement
    $root.SetAttribute('Version', $Version)
    $root.SetAttribute('Uri', $FeedUri)
    function Add-Element($parent, [string]$name, [hashtable]$attributes) {
        $element = $document.CreateElement($name, $root.NamespaceURI)
        foreach ($key in $attributes.Keys) { $element.SetAttribute($key, [string]$attributes[$key]) }
        $null = $parent.AppendChild($element)
        return $element
    }
    $null = Add-Element $root 'MainPackage' @{ Name='AiUsage.Dev'; Publisher='CN=AI Usage Development'; Version=$Version; ProcessorArchitecture='x64'; Uri=$PackageUri }
    if ($Dependencies.Count) {
        $container = Add-Element $root 'Dependencies' @{}
        foreach ($dependency in $Dependencies) {
            $null = [version]$dependency.Version
            if ($dependency.ProcessorArchitecture -notin @('x64','neutral')) { throw 'Unsupported dependency architecture.' }
            $null = Add-Element $container 'Package' @{ Name=$dependency.Name; Publisher=$dependency.Publisher; Version=$dependency.Version; ProcessorArchitecture=$dependency.ProcessorArchitecture; Uri=$dependency.Uri }
        }
    }
    $settings = Add-Element $root 'UpdateSettings' @{}
    $null = Add-Element $settings 'OnLaunch' @{ HoursBetweenUpdateChecks='0'; ShowPrompt='false'; UpdateBlocksActivation='false' }
    $null = Add-Element $settings 'AutomaticBackgroundTask' @{}
    (Add-Element $settings 'ForceUpdateFromAnyVersion' @{}).InnerText = 'false'
    return $document.OuterXml
}
function Test-PreviewPromotion {
    param([string]$Candidate, [string[]]$PublishedCommits)
    foreach ($commit in @($Candidate) + $PublishedCommits) {
        if ($commit -notmatch '^[0-9a-f]{40}$') { throw 'Preview source must be an immutable SHA.' }
        & git cat-file -e "$commit^{commit}" 2>$null
        if ($LASTEXITCODE -ne 0) { throw 'Preview source commit is unavailable.' }
    }
    foreach ($previous in $PublishedCommits) {
        & git merge-base --is-ancestor $previous $Candidate
        if ($LASTEXITCODE -eq 1) { return $false }
        if ($LASTEXITCODE -ne 0) { throw 'Cannot establish Preview ancestry.' }
    }
    return $true
}
Export-ModuleMember -Function Get-NextPreviewVersion, New-PreviewFeed, Test-PreviewPromotion
