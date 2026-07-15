param(
    [string] $ConfigPath = "$PSScriptRoot\project.build.json",
    [string] $DotNetPublishConfigPath = "$PSScriptRoot\..\powerforge.dotnetpublish.json",
    [Nullable[bool]] $UpdateVersions,
    [Nullable[bool]] $Build,
    [bool] $BuildModule = $true,
    [bool] $PublishTools = $true,
    [Nullable[bool]] $PublishNuget = $false,
    [Nullable[bool]] $PublishGitHub = $false,
    [switch] $Plan,
    [string] $PlanPath,
    [string[]] $Target = @(),
    [string[]] $Runtimes = @(),
    [string[]] $Frameworks = @(),
    [ValidateSet('Portable', 'PortableCompat', 'PortableSize', 'FrameworkDependent', 'AotSpeed', 'AotSize')]
    [string[]] $Styles = @(),
    [switch] $SkipInstallers,
    [switch] $Validate,
    [switch] $SkipRestore,
    [switch] $SkipBuild
)

$ErrorActionPreference = 'Stop'

Import-Module PSPublishModule -Force -ErrorAction Stop

function Invoke-DesktopManagerDotNetPublish {
    $dotNetPublishParams = @{
        ConfigPath = $DotNetPublishConfigPath
    }

    if ($Target.Count -gt 0) { $dotNetPublishParams.Target = $Target }
    if ($Runtimes.Count -gt 0) { $dotNetPublishParams.Runtimes = $Runtimes }
    if ($Frameworks.Count -gt 0) { $dotNetPublishParams.Frameworks = $Frameworks }
    if ($Styles.Count -gt 0) { $dotNetPublishParams.Styles = $Styles }
    if ($Plan) { $dotNetPublishParams.Plan = $true }
    if ($Validate) { $dotNetPublishParams.Validate = $true }
    if ($SkipInstallers) { $dotNetPublishParams.SkipInstallers = $true }
    if ($SkipRestore) { $dotNetPublishParams.SkipRestore = $true }
    if ($SkipBuild) { $dotNetPublishParams.SkipBuild = $true }

    Invoke-DotNetPublish @dotNetPublishParams
}

$dotNetPublishInvoked = $false
if ($Plan -and $PublishTools) {
    Invoke-DesktopManagerDotNetPublish
    $dotNetPublishInvoked = $true
}

$invokeParams = @{
    ConfigPath = $ConfigPath
}
if ($null -ne $UpdateVersions) { $invokeParams.UpdateVersions = $UpdateVersions }
if ($null -ne $Build) { $invokeParams.Build = $Build }
if ($null -ne $PublishNuget) { $invokeParams.PublishNuget = $PublishNuget }
if ($null -ne $PublishGitHub) { $invokeParams.PublishGitHub = $PublishGitHub }
if ($Plan) { $invokeParams.Plan = $true }
if ($PlanPath) { $invokeParams.PlanPath = $PlanPath }

Invoke-ProjectBuild @invokeParams

if ($BuildModule -and -not $Plan) {
    & (Join-Path $PSScriptRoot 'Build-Module.ps1') -SkipInstall
}

if ($PublishTools -and -not $dotNetPublishInvoked) {
    Invoke-DesktopManagerDotNetPublish
}
