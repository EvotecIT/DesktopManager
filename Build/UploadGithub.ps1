[xml] $XML = Get-Content -Raw "C:\Support\GitHub\DesktopManager\Sources\DesktopManager\DesktopManager.csproj"
$Version = $XML.Project.PropertyGroup[0].VersionPrefix
$ZipPath = "C:\Support\GitHub\DesktopManager\Sources\DesktopManager\bin\Release\DesktopManager.$Version.zip"
$IsPreRelease = $false
$TagName = "v$Version"
$GitHubAccessToken = Get-Content -Raw 'C:\Support\Important\GithubAPI.txt'
$UserName = 'EvotecIT'
$GitHubRepositoryName = 'DesktopManager'

if (Test-Path -LiteralPath $ZipPath) {
    $StatusGithub = Send-GitHubRelease -GitHubUsername $UserName -GitHubRepositoryName $GitHubRepositoryName -GitHubAccessToken $GitHubAccessToken -TagName $TagName -AssetFilePaths $ZipPath -IsPreRelease $IsPreRelease
    $StatusGithub
}