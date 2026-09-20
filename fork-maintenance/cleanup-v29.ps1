param(
    [string] $Repo = "ARMoir/AnkhSVN",
    [string] $Owner = "ARMoir",
    [switch] $Execute
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

function Get-PagedJson([string] $Path) {
    $raw = gh api --paginate --slurp $Path
    $pages = $raw | ConvertFrom-Json
    $items = @()
    foreach ($page in $pages) {
        if ($page -is [System.Array]) { $items += $page } else { $items += ,$page }
    }
    return @($items)
}

function Get-V29Patch([string] $Version, [bool] $Tagged = $false) {
    $pattern = if ($Tagged) { '^v2\.9\.(\d+)$' } else { '^2\.9\.(\d+)$' }
    if ($Version -match $pattern) { return [int]$Matches[1] }
    return -1
}

function Invoke-Delete([scriptblock] $Action, [string] $Description) {
    if ($Execute) {
        Write-Host $Description
        & $Action
    } else {
        Write-Host "[DRY RUN] $Description"
    }
}

Write-Host "Repository: $Repo"
Write-Host "Mode: $(if ($Execute) { 'EXECUTE' } else { 'DRY RUN' })"
Write-Host ""

$allReleases = Get-PagedJson "repos/$Repo/releases?per_page=100"
$v29Releases = @($allReleases | Where-Object { (Get-V29Patch ([string]$_.tag_name) $true) -ge 0 } | Sort-Object { Get-V29Patch ([string]$_.tag_name) $true } -Descending)
if ($v29Releases.Count -eq 0) { throw "No v2.9.xxx releases found; refusing cleanup." }

$keepRelease = $v29Releases[0]
$keepReleaseTag = [string]$keepRelease.tag_name
$firstV29Date = ($v29Releases | ForEach-Object { [datetime]$_.created_at } | Sort-Object | Select-Object -First 1)
Write-Host "Keeping release: $keepReleaseTag"

$releaseDeletes = 0
foreach ($release in ($v29Releases | Select-Object -Skip 1)) {
    $tag = [string]$release.tag_name
    Invoke-Delete { gh release delete $tag --repo $Repo --yes } "Delete release $tag"
    $releaseDeletes++
}

$packagePath = "users/$Owner/packages/nuget/AnkhSVN.VSIX.2022/versions?per_page=100"
$allPackageVersions = Get-PagedJson $packagePath
$v29PackageVersions = @($allPackageVersions | Where-Object { (Get-V29Patch ([string]$_.name) $false) -ge 0 } | Sort-Object { Get-V29Patch ([string]$_.name) $false } -Descending)

$packageDeletes = 0
$keepPackageVersion = $null
if ($v29PackageVersions.Count -gt 0) {
    $keepPackageVersion = [string]$v29PackageVersions[0].name
    Write-Host "Keeping package: $keepPackageVersion"
    foreach ($packageVersion in ($v29PackageVersions | Select-Object -Skip 1)) {
        $name = [string]$packageVersion.name
        $id = [long]$packageVersion.id
        Invoke-Delete { gh api --method DELETE "users/$Owner/packages/nuget/AnkhSVN.VSIX.2022/versions/$id" } "Delete package version $name ($id)"
        $packageDeletes++
    }
} else {
    Write-Host "No 2.9.xxx package versions found."
}

$allDeployments = Get-PagedJson "repos/$Repo/deployments?ref=main&per_page=100"
$eraDeployments = @($allDeployments | Where-Object { [datetime]$_.created_at -ge $firstV29Date } | Sort-Object { [datetime]$_.created_at } -Descending)

$seenEnvironments = @{}
$deploymentDeletes = 0
foreach ($deployment in $eraDeployments) {
    $environment = [string]$deployment.environment
    if ([string]::IsNullOrWhiteSpace($environment)) { $environment = "(default)" }
    if (-not $seenEnvironments.ContainsKey($environment)) {
        $seenEnvironments[$environment] = $true
        Write-Host "Keeping newest deployment $($deployment.id) for '$environment'"
        continue
    }
    $id = [long]$deployment.id
    Invoke-Delete {
        gh api --method POST "repos/$Repo/deployments/$id/statuses" -f state=inactive -f description="Retiring old v2.9 deployment before cleanup" | Out-Null
        gh api --method DELETE "repos/$Repo/deployments/$id"
    } "Retire and delete deployment $id from '$environment'"
    $deploymentDeletes++
}

Write-Host ""
Write-Host "Summary"
Write-Host "-------"
Write-Host "Keep release: $keepReleaseTag"
if ($keepPackageVersion) { Write-Host "Keep package: $keepPackageVersion" }
Write-Host "Release deletions: $releaseDeletes"
Write-Host "Package deletions: $packageDeletes"
Write-Host "Deployment deletions: $deploymentDeletes"
Write-Host "Legacy non-v2.9 releases are untouched."

if (-not $Execute) {
    Write-Host ""
    Write-Host "Dry run only. Re-run with -Execute to perform the deletions."
}