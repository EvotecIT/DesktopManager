Import-Module PSPublishModule -Force -ErrorAction Stop

$GitHubAccessToken = Get-Content -Raw 'C:\Support\Important\GithubAPI.txt'

$publishGitHubReleaseAssetSplat = @{
    ProjectPath          = "$PSScriptRoot\..\Sources\DesktopManager"
    GitHubAccessToken    = $GitHubAccessToken
    GitHubUsername       = "EvotecIT"
    GitHubRepositoryName = "DesktopManager"
    IsPreRelease         = $false
}

Publish-GitHubReleaseAsset @publishGitHubReleaseAssetSplat
