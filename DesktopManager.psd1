@{
    AliasesToExport        = @()
    Author                 = 'Przemyslaw Klys'
    CmdletsToExport        = @()
    CompanyName            = 'Evotec'
    CompatiblePSEditions   = @('Desktop', 'Core')
    Copyright              = '(c) 2011 - 2022 Przemyslaw Klys @ Evotec. All rights reserved.'
    Description            = 'Desktop Manager'
    DotNetFrameworkVersion = '4.7.2'
    FunctionsToExport      = @('Get-DesktopMonitors', 'Get-DesktopWallpaper', 'Set-DesktopWallpaper')
    GUID                   = '56f85fa6-c622-4204-8e97-3d99e3e06e75'
    ModuleVersion          = '0.0.1'
    PowerShellVersion      = '5.1'
    PrivateData            = @{
        PSData = @{
            Tags       = @('windows', 'image', 'charts', 'qrcodes', 'barcodes')
            LicenseUri = 'https://github.com/EvotecIT/DesktopManager/blob/master/License'
            ProjectUri = 'https://github.com/EvotecIT/DesktopManager'
        }
    }
    RootModule             = 'DesktopManager.psm1'
}