param(
    [ValidateSet('Manifest', 'Documentation', 'Build', 'Publish')]
    [string] $ConfigurationGateMode = 'Build',

    [bool] $SignModule = $true,

    [string] $ProjectBuildConfigPath = 'Build\project.build.json',

    [string] $PowerShellGalleryApiKeyPath = 'C:\Support\Important\PowerShellGalleryAPI.txt',

    [string] $GitHubApiKeyPath = 'C:\Support\Important\GitHubAPI.txt'
)

Import-Module PSPublishModule -MinimumVersion '3.0.129' -Force -ErrorAction Stop

Build-Module -ModuleName 'DesktopManager' {
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
        UseConsistentWhitespaceCheckOperator        = $true
        UseConsistentWhitespaceCheckOpenParen       = $true
        UseConsistentWhitespaceCheckPipe            = $true
        UseConsistentWhitespaceCheckSeparator       = $true
        AlignAssignmentStatementEnable              = $true
        AlignAssignmentStatementCheckHashtable      = $true
        UseCorrectCasingEnable                      = $true
    }

    New-ConfigurationFormat -ApplyTo 'OnMergePSM1', 'OnMergePSD1' -Sort None @configurationFormat
    New-ConfigurationFormat -ApplyTo 'DefaultPSD1', 'DefaultPSM1' -EnableFormatting -Sort None
    New-ConfigurationFormat -ApplyTo 'DefaultPSD1', 'OnMergePSD1' -PSD1Style 'Minimal'

    New-ConfigurationDocumentation -Enable -PathReadme 'Docs\Readme.md' -Path 'Docs' -SyncExternalHelpToProjectRoot
    New-ConfigurationImportModule -ImportSelf -ImportRequiredModules

    $newConfigurationBuild = @{
        Enable                            = $true
        SignModule                        = $SignModule
        MergeModuleOnBuild                = $true
        MergeFunctionsFromApprovedModules = $true
        CertificateThumbprint             = '92E95FB58EFFA6A4A75E77A33CDD6BFE6DD30F1A'
        ResolveBinaryConflicts            = $true
        ResolveBinaryConflictsName        = 'DesktopManager.PowerShell'
        NETProjectName                    = 'DesktopManager.PowerShell'
        NETProjectPath                    = 'Sources\DesktopManager.PowerShell\DesktopManager.PowerShell.csproj'
        NETConfiguration                  = 'Release'
        NETFramework                      = 'net8.0-windows10.0.19041.0', 'net472'
        NETSearchClass                    = 'DesktopManager.PowerShell.CmdletSetDesktopWallpaper'
        NETHandleAssemblyWithSameName     = $true
        NETAssemblyLoadContext            = $true
        NETBinaryModuleDocumentation      = $true
        DotSourceLibraries                = $true
        DotSourceClasses                  = $true
        DeleteTargetModuleBeforeBuild     = $true
    }
    New-ConfigurationBuild @newConfigurationBuild

    New-ConfigurationProjectBuild -Name 'DesktopManager' -ConfigPath $ProjectBuildConfigPath -Enabled -BuildBeforeModule -ProvideLocalNuGetFeed -PublishNuget
    New-ConfigurationRelease -StageRoot 'Artefacts\UploadReady' -VersionSource Module -BuildOrder 'Packages', 'Module' -PublishOrder 'NuGet', 'PowerShellGallery', 'GitHub'

    New-ConfigurationArtefact -Type Unpacked -Enable -Path "$PSScriptRoot\..\Artefacts\Unpacked" -RequiredModulesPath "$PSScriptRoot\..\Artefacts\Unpacked\Modules"
    New-ConfigurationArtefact -Type Packed -Enable -Path "$PSScriptRoot\..\Artefacts\Packed" -IncludeTagName -ArtefactName 'DesktopManager-PowerShellModule.<TagModuleVersionWithPreRelease>.zip' -ID 'ToGitHub'

    New-ConfigurationPublish -Type PowerShellGallery -FilePath $PowerShellGalleryApiKeyPath -Enabled:$false
    New-ConfigurationPublish -Type GitHub -FilePath $GitHubApiKeyPath -UserName 'EvotecIT' -RepositoryName 'DesktopManager' -Enabled:$false -GenerateReleaseNotes -OverwriteTagName 'DesktopManager-PowerShellModule.<TagModuleVersionWithPreRelease>'

    New-ConfigurationGate -Mode $ConfigurationGateMode
} -ExitCode
