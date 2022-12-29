@{
    AliasesToExport        = @()
    Author                 = 'Przemyslaw Klys'
    CmdletsToExport        = @()
    CompanyName            = 'Evotec'
    CompatiblePSEditions   = @('Desktop', 'Core')
    Copyright              = '(c) 2011 - 2022 Przemyslaw Klys @ Evotec. All rights reserved.'
    Description            = 'Desktop Manager is a PowerShell module that allows easy way to change wallpaper on multiple screens/monitors.'
    DotNetFrameworkVersion = '4.7.2'
    FunctionsToExport      = @('Get-DesktopMonitors', 'Get-DesktopWallpaper', 'Set-DesktopWallpaper')
    GUID                   = '56f85fa6-c622-4204-8e97-3d99e3e06e75'
    ModuleVersion          = '0.0.2'
    PowerShellVersion      = '5.1'
    PrivateData            = @{
        PSData = @{
            Tags       = @('windows', 'image', 'wallpaper', 'monitor')
            LicenseUri = 'https://github.com/EvotecIT/DesktopManager/blob/master/License'
            ProjectUri = 'https://github.com/EvotecIT/DesktopManager'
            IconUri    = 'https://evotec.xyz/wp-content/uploads/2022/12/DesktopManager.png'
        }
    }
    RootModule             = 'DesktopManager.psm1'
}