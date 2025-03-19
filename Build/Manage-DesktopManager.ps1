Import-Module C:\Support\Github\PSPublishModule\PSPublishModule.psd1 -Force

Build-Module -ModuleName 'DesktopManager' {
    # Usual defaults as per standard module
    $Manifest = [ordered] @{
        # Minimum version of the Windows PowerShell engine required by this module
        PowerShellVersion      = '5.1'
        # prevent using over CORE/PS 7
        CompatiblePSEditions   = @('Desktop', 'Core')
        # ID used to uniquely identify this module
        GUID                   = '56f85fa6-c622-4204-8e97-3d99e3e06e75'
        # Version number of this module.
        ModuleVersion          = '3.0.X'
        # Author of this module
        Author                 = 'Przemyslaw Klys'
        # Company or vendor of this module
        CompanyName            = 'Evotec'
        # Copyright statement for this module
        Copyright              = "(c) 2011 - $((Get-Date).Year) Przemyslaw Klys @ Evotec. All rights reserved."
        # Description of the functionality provided by this module
        Description            = 'Desktop Manager is a PowerShell module that allows easy way to change wallpaper on multiple screens/monitors.'
        # Tags applied to this module. These help with module discovery in online galleries.
        Tags                   = @('windows', 'image', 'wallpaper', 'monitor')
        # A URL to the main website for this project.
        ProjectUri             = 'https://github.com/EvotecIT/DesktopManager'

        IconUri                = 'https://evotec.xyz/wp-content/uploads/2022/12/DesktopManager.png'

        DotNetFrameworkVersion = '4.7.2'
    }
    New-ConfigurationManifest @Manifest

    $ConfigurationFormat = [ordered] @{
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
    # format PSD1 and PSM1 files when merging into a single file
    # enable formatting is not required as Configuration is provided
    New-ConfigurationFormat -ApplyTo 'OnMergePSM1', 'OnMergePSD1' -Sort None @ConfigurationFormat
    # format PSD1 and PSM1 files within the module
    # enable formatting is required to make sure that formatting is applied (with default settings)
    New-ConfigurationFormat -ApplyTo 'DefaultPSD1', 'DefaultPSM1' -EnableFormatting -Sort None
    # when creating PSD1 use special style without comments and with only required parameters
    New-ConfigurationFormat -ApplyTo 'DefaultPSD1', 'OnMergePSD1' -PSD1Style 'Minimal'

    # configuration for documentation, at the same time it enables documentation processing
    New-ConfigurationDocumentation -Enable:$false -StartClean -UpdateWhenNew -PathReadme 'Docs\Readme.md' -Path 'Docs'

    $newConfigurationBuildSplat = @{
        Enable                            = $true
        SignModule                        = $true
        MergeModuleOnBuild                = $true
        MergeFunctionsFromApprovedModules = $true
        CertificateThumbprint             = '483292C9E317AA13B07BB7A96AE9D1A5ED9E7703'
        ResolveBinaryConflicts            = $true
        ResolveBinaryConflictsName        = 'DesktopManager.PowerShell'
        NETProjectName                    = 'DesktopManager.PowerShell'
        NETConfiguration                  = 'Release'
        NETFramework                      = 'net8.0', 'net472'
        NETSearchClass                    = "DesktopManager.PowerShell.CmdletSetDesktopWallpaper"
        NETHandleAssemblyWithSameName     = $true
        NETBinaryModuleDocumenation       = $true
        #NETMergeLibraryDebugging          = $true
        DotSourceLibraries                = $true
        DotSourceClasses                  = $true
        DeleteTargetModuleBeforeBuild     = $true
    }

    New-ConfigurationBuild @newConfigurationBuildSplat

    New-ConfigurationArtefact -Type Unpacked -Enable -Path "$PSScriptRoot\..\Artefacts\Unpacked" -RequiredModulesPath "$PSScriptRoot\..\Artefacts\Unpacked\Modules"
    New-ConfigurationArtefact -Type Packed -Enable -Path "$PSScriptRoot\..\Artefacts\Packed" -IncludeTagName

    # global options for publishing to github/psgallery
    #New-ConfigurationPublish -Type PowerShellGallery -FilePath 'C:\Support\Important\PowerShellGalleryAPI.txt' -Enabled:$true
    #New-ConfigurationPublish -Type GitHub -FilePath 'C:\Support\Important\GitHubAPI.txt' -UserName 'EvotecIT' -Enabled:$true
}