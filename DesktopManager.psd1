@{
    AliasesToExport        = @('Get-DesktopMonitors')
    Author                 = 'Przemyslaw Klys'
    CmdletsToExport        = @('Get-DesktopMonitor', 'Get-DesktopWallpaper', 'Get-DesktopWindow', 'Invoke-DesktopScreenshot', 'Set-DesktopPosition', 'Set-DesktopWallpaper', 'Set-DesktopWindow')
    CompanyName            = 'Evotec'
    CompatiblePSEditions   = @('Desktop', 'Core')
    Copyright              = '(c) 2011 - 2025 Przemyslaw Klys @ Evotec. All rights reserved.'
    Description            = 'Desktop Manager is a PowerShell module that allows easy way to change wallpaper on multiple screens/monitors.'
    DotNetFrameworkVersion = '4.7.2'
    FunctionsToExport      = @()
    GUID                   = '56f85fa6-c622-4204-8e97-3d99e3e06e75'
    ModuleVersion          = '3.1.0'
    PowerShellVersion      = '5.1'
    PrivateData            = @{
        PSData = @{
            IconUri    = 'https://evotec.xyz/wp-content/uploads/2022/12/DesktopManager.png'
            Prerelease = 'Preview1'
            ProjectUri = 'https://github.com/EvotecIT/DesktopManager'
            Tags       = @('windows', 'image', 'wallpaper', 'monitor')
        }
    }
    RootModule             = 'DesktopManager.psm1'
}