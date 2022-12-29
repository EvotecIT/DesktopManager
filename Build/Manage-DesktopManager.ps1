Clear-Host

Import-Module "C:\Support\GitHub\PSPublishModule\PSPublishModule.psd1" -Force

$Configuration = @{
    Information = @{
        ModuleName        = 'DesktopManager'

        DirectoryProjects = 'C:\Support\GitHub'

        Manifest          = @{
            # Minimum version of the Windows PowerShell engine required by this module
            PowerShellVersion      = '5.1'
            # prevent using over CORE/PS 7
            CompatiblePSEditions   = @('Desktop', 'Core')
            # ID used to uniquely identify this module
            GUID                   = '56f85fa6-c622-4204-8e97-3d99e3e06e75'
            # Version number of this module.
            ModuleVersion          = '0.0.X'
            # Author of this module
            Author                 = 'Przemyslaw Klys'
            # Company or vendor of this module
            CompanyName            = 'Evotec'
            # Copyright statement for this module
            Copyright              = "(c) 2011 - $((Get-Date).Year) Przemyslaw Klys @ Evotec. All rights reserved."
            # Description of the functionality provided by this module
            Description            = 'Desktop Manager is a PowerShell module that allows easy way to change wallpaper on multiple screens/monitors.'
            # Tags applied to this module. These help with module discovery in online galleries.
            Tags                   = @('windows', 'image', 'wallpaper','monitor')
            # A URL to the main website for this project.
            ProjectUri             = 'https://github.com/EvotecIT/DesktopManager'

            IconUri              = 'https://evotec.xyz/wp-content/uploads/2022/12/DesktopManager.png'

            LicenseUri             = 'https://github.com/EvotecIT/DesktopManager/blob/master/License'

            RequiredModules        = @(
                #@{ ModuleName = 'PSSharedGoods'; ModuleVersion = "Latest"; Guid = 'ee272aa8-baaa-4edf-9f45-b6d6f7d844fe' }
            )
            DotNetFrameworkVersion = '4.7.2'
        }
    }
    Options     = @{
        Merge             = @{
            Sort           = 'None'
            FormatCodePSM1 = @{
                Enabled           = $true
                RemoveComments    = $true
                FormatterSettings = @{
                    IncludeRules = @(
                        'PSPlaceOpenBrace',
                        'PSPlaceCloseBrace',
                        'PSUseConsistentWhitespace',
                        'PSUseConsistentIndentation',
                        'PSAlignAssignmentStatement',
                        'PSUseCorrectCasing'
                    )

                    Rules        = @{
                        PSPlaceOpenBrace           = @{
                            Enable             = $true
                            OnSameLine         = $true
                            NewLineAfter       = $true
                            IgnoreOneLineBlock = $true
                        }

                        PSPlaceCloseBrace          = @{
                            Enable             = $true
                            NewLineAfter       = $false
                            IgnoreOneLineBlock = $true
                            NoEmptyLineBefore  = $false
                        }

                        PSUseConsistentIndentation = @{
                            Enable              = $true
                            Kind                = 'space'
                            PipelineIndentation = 'IncreaseIndentationAfterEveryPipeline'
                            IndentationSize     = 4
                        }

                        PSUseConsistentWhitespace  = @{
                            Enable          = $true
                            CheckInnerBrace = $true
                            CheckOpenBrace  = $true
                            CheckOpenParen  = $true
                            CheckOperator   = $true
                            CheckPipe       = $true
                            CheckSeparator  = $true
                        }

                        PSAlignAssignmentStatement = @{
                            Enable         = $true
                            CheckHashtable = $true
                        }

                        PSUseCorrectCasing         = @{
                            Enable = $true
                        }
                    }
                }
            }
            FormatCodePSD1 = @{
                Enabled        = $true
                RemoveComments = $false
            }
            Integrate      = @{
                ApprovedModules = @('PSSharedGoods', 'PSWriteColor', 'Connectimo', 'PSUnifi', 'PSWebToolbox', 'PSMyPassword')
            }
        }
        Standard          = @{
            FormatCodePSM1 = @{

            }
            FormatCodePSD1 = @{
                Enabled = $true
                #RemoveComments = $true
            }
        }
        PowerShellGallery = @{
            ApiKey   = 'C:\Support\Important\PowerShellGalleryAPI.txt'
            FromFile = $true
        }
        GitHub            = @{
            ApiKey   = 'C:\Support\Important\GithubAPI.txt'
            FromFile = $true
            UserName = 'EvotecIT'
            #RepositoryName = 'PSWriteHTML'
        }
        Documentation     = @{
            Path       = 'Docs'
            PathReadme = 'Docs\Readme.md'
        }
    }
    Steps       = @{
        BuildLibraries     = @{
            Enable        = $true # build once every time nuget gets updated
            Configuration = 'Release'
            Framework     = 'netstandard2.0', 'net472'
            #ProjectName   = 'ImagePlayground.PowerShell'
        }
        BuildModule        = @{  # requires Enable to be on to process all of that
            Enable              = $true
            DeleteBefore        = $true
            Merge               = $true
            MergeMissing        = $true
            LibrarySeparateFile = $false
            LibraryDotSource    = $true
            ClassesDotSource    = $false
            SignMerged          = $true
            CreateFileCatalog   = $false # not working
            Releases            = $false
            ReleasesUnpacked    = @{
                Enabled         = $true
                IncludeTagName  = $true
                Path            = "$PSScriptRoot\..\Artefacts"
                RequiredModules = $false
                DirectoryOutput = @{

                }
                FilesOutput     = @{

                }
            }
            RefreshPSD1Only     = $false
            DebugDLL            = $false
            #ResolveBinaryConflicts = @{
            #    ProjectName = 'ImagePlayground.PowerShell'
            #}
        }
        BuildDocumentation = $true
        ImportModules      = @{
            Self            = $false
            RequiredModules = $false
            Verbose         = $false
        }
        PublishModule      = @{  # requires Enable to be on to process all of that
            Enabled      = $false
            Prerelease   = ''
            RequireForce = $false
            GitHub       = $false
        }
    }
}

New-PrepareModule -Configuration $Configuration