param(
    [switch] $SkipInstall,
    [switch] $JsonOnly,
    [string] $JsonPath,
    [switch] $NoInteractive
)

$ErrorActionPreference = 'Stop'

Import-Module PSPublishModule -Force -ErrorAction Stop

$refreshPsd1Only = $false
if ($env:RefreshPSD1Only) {
    $refreshPsd1Only = $env:RefreshPSD1Only -eq 'true'
}

$invokeModuleBuild = @{
    ModuleName = 'DesktopManager'
}

if ($SkipInstall) {
    $invokeModuleBuild.SkipInstall = $true
}
if ($JsonOnly) {
    $invokeModuleBuild.JsonOnly = $true
}
if ($JsonPath) {
    $invokeModuleBuild.JsonPath = $JsonPath
}
if ($NoInteractive) {
    $invokeModuleBuild.NoInteractive = $true
}

Invoke-ModuleBuild @invokeModuleBuild -Settings {
    $manifest = [ordered] @{
        PowerShellVersion      = '5.1'
        CompatiblePSEditions   = @('Desktop', 'Core')
        GUID                   = '56f85fa6-c622-4204-8e97-3d99e3e06e75'
        ModuleVersion          = '4.X.0'
        Author                 = 'Przemyslaw Klys'
        CompanyName            = 'Evotec'
        Copyright              = "(c) 2011 - $((Get-Date).Year) Przemyslaw Klys @ Evotec. All rights reserved."
        Description            = 'Desktop Manager is a PowerShell module that allows easy way to change wallpaper on multiple screens/monitors.'
        Tags                   = @('windows', 'image', 'wallpaper', 'monitor')
        ProjectUri             = 'https://github.com/EvotecIT/DesktopManager'
        IconUri                = 'https://evotec.xyz/wp-content/uploads/2022/12/DesktopManager.png'
        DotNetFrameworkVersion = '4.7.2'
    }
    New-ConfigurationManifest @manifest

    $configurationFormat = [ordered] @{
        RemoveComments                              = $false
        PlaceOpenBraceEnable                        = $true
        PlaceOpenBraceOnSameLine                    = $true
        PlaceOpenBraceNewLineAfter                  = $true
        PlaceOpenBraceIgnoreOneLineBlock            = $false
        PlaceCloseBraceEnable                       = $true
        PlaceCloseBraceNewLineAfter                 = $false
        PlaceCloseBraceIgnoreOneLineBlock           = $false
        PlaceCloseBraceNoEmptyLineBefore            = $true
        UseConsistentIndentationEnable              = $true
        UseConsistentIndentationKind                = 'space'
        UseConsistentIndentationPipelineIndentation = 'IncreaseIndentationAfterEveryPipeline'
        UseConsistentIndentationIndentationSize     = 4
        UseConsistentWhitespaceEnable               = $true
        UseConsistentWhitespaceCheckInnerBrace      = $true
        UseConsistentWhitespaceCheckOpenBrace       = $true
        UseConsistentWhitespaceCheckOpenParen       = $true
        UseConsistentWhitespaceCheckOperator        = $true
        UseConsistentWhitespaceCheckPipe            = $true
        UseConsistentWhitespaceCheckSeparator       = $true
        AlignAssignmentStatementEnable              = $true
        AlignAssignmentStatementCheckHashtable      = $true
        UseCorrectCasingEnable                      = $true
    }

    New-ConfigurationFormat -ApplyTo 'OnMergePSM1', 'OnMergePSD1' -Sort None @configurationFormat
    New-ConfigurationFormat -ApplyTo 'DefaultPSD1', 'DefaultPSM1' -EnableFormatting -Sort None
    New-ConfigurationFormat -ApplyTo 'DefaultPSD1', 'OnMergePSD1' -PSD1Style 'Minimal'

    New-ConfigurationDocumentation -Enable:$true -PathReadme 'Docs\Readme.md' -Path 'Docs'

    $newConfigurationBuild = @{
        Enable                            = $true
        SignModule                        = -not $refreshPsd1Only
        MergeModuleOnBuild                = $true
        MergeFunctionsFromApprovedModules = $true
        CertificateThumbprint             = '92E95FB58EFFA6A4A75E77A33CDD6BFE6DD30F1A'
        ResolveBinaryConflicts            = $true
        ResolveBinaryConflictsName        = 'DesktopManager.PowerShell'
        NETProjectName                    = 'DesktopManager.PowerShell'
        NETProjectPath                    = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\Sources\DesktopManager.PowerShell\DesktopManager.PowerShell.csproj')).Path
        NETConfiguration                  = 'Release'
        NETFramework                      = 'net8.0-windows10.0.19041.0', 'net472'
        NETSearchClass                    = 'DesktopManager.PowerShell.CmdletSetDesktopWallpaper'
        NETHandleAssemblyWithSameName     = $true
        NETAssemblyLoadContext            = $true
        NETBinaryModuleDocumentation      = $true
        DotSourceLibraries                = $true
        DotSourceClasses                  = $true
        DeleteTargetModuleBeforeBuild     = $true
        RefreshPSD1Only                   = $refreshPsd1Only
    }
    New-ConfigurationBuild @newConfigurationBuild

    New-ConfigurationArtefact -Type Unpacked -Enable -Path "$PSScriptRoot\..\Artefacts\Unpacked" -RequiredModulesPath "$PSScriptRoot\..\Artefacts\Unpacked\Modules"
    New-ConfigurationArtefact -Type Packed -Enable -Path "$PSScriptRoot\..\Artefacts\Packed" -IncludeTagName -ArtefactName 'DesktopManager-PowerShellModule.<TagModuleVersionWithPreRelease>.zip' -ID 'ToGitHub'

}
