param(
    [ValidateSet('Manifest', 'Documentation', 'Build', 'Publish')]
    [string] $ConfigurationGateMode = 'Build',
    [bool] $BuildModule = $true,
    [bool] $PublishTools = $true,
    [switch] $Plan,
    [string[]] $Target = @(),
    [string[]] $Runtimes = @(),
    [string[]] $Frameworks = @(),
    [ValidateSet('Portable', 'PortableCompat', 'PortableSize', 'FrameworkDependent', 'AotSpeed', 'AotSize')]
    [string[]] $Styles = @(),
    [switch] $SkipInstallers,
    [switch] $Validate,
    [switch] $SkipRestore,
    [switch] $SkipBuild,
    [string] $DotNetPublishConfigPath = "$PSScriptRoot\..\powerforge.dotnetpublish.json"
)

$ErrorActionPreference = 'Stop'

Import-Module PSPublishModule -MinimumVersion '3.0.129' -Force -ErrorAction Stop

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

if ($BuildModule -and -not $Plan) {
    $powerShellExecutable = (Get-Process -Id $PID).Path
    & $powerShellExecutable -NoLogo -NoProfile -File (Join-Path $PSScriptRoot 'Build-Module.ps1') -ConfigurationGateMode $ConfigurationGateMode
    if ($LASTEXITCODE -ne 0) {
        throw "DesktopManager package and module build failed with exit code $LASTEXITCODE."
    }
}

if ($PublishTools) {
    Invoke-DesktopManagerDotNetPublish
}
