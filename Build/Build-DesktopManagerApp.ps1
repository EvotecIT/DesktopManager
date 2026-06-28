param(
    [string] $ConfigPath = "$PSScriptRoot\..\powerforge.dotnetpublish.json",
    [ValidateSet('win-x64')]
    [string[]] $Runtimes = @('win-x64'),
    [ValidateSet('net10.0-windows10.0.19041.0')]
    [string[]] $Frameworks = @('net10.0-windows10.0.19041.0'),
    [ValidateSet('PortableCompat')]
    [string[]] $Styles = @('PortableCompat'),
    [switch] $Plan,
    [switch] $Validate,
    [switch] $SkipRestore,
    [switch] $SkipBuild
)

$ErrorActionPreference = 'Stop'

Import-Module PSPublishModule -Force -ErrorAction Stop

$invokeParams = @{
    ConfigPath      = $ConfigPath
    Target          = @('DesktopManager.App')
    Runtimes        = $Runtimes
    Frameworks      = $Frameworks
    Styles          = $Styles
    SkipInstallers  = $true
}

if ($Plan) { $invokeParams.Plan = $true }
if ($Validate) { $invokeParams.Validate = $true }
if ($SkipRestore) { $invokeParams.SkipRestore = $true }
if ($SkipBuild) { $invokeParams.SkipBuild = $true }

Invoke-DotNetPublish @invokeParams
