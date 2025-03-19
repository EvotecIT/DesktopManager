@{
    AliasesToExport        = @('Get-DesktopMonitors')
    Author                 = 'Przemyslaw Klys'
    CmdletsToExport        = @(
        'Get-DesktopMonitor',
        'Get-DesktopWallpaper',
        'Set-DesktopPosition',
        'Set-DesktopWallpaper',
        'Get-DesktopWindow',
        'Get-DesktopWindowPosition',
        'Set-DesktopWindowPosition',
        'Set-DesktopWindowState'
    )
    CompanyName            = 'Evotec'
    CompatiblePSEditions   = @('Desktop', 'Core')
    Copyright              = '(c) 2011 - 2025 Przemyslaw Klys @ Evotec. All rights reserved.'
    Description            = 'Desktop Manager is a PowerShell module that allows easy way to change wallpaper and manage windows on multiple screens/monitors.'
    DotNetFrameworkVersion = '4.7.2'
    FunctionsToExport      = @()
    GUID                   = '56f85fa6-c622-4204-8e97-3d99e3e06e75'
    ModuleVersion          = '2.0.1'
    PowerShellVersion      = '5.1'
    PrivateData            = @{
        PSData = @{
            Tags       = @('Windows', 'Desktop', 'Display', 'Wallpaper', 'Monitor')
            ProjectUri = 'https://github.com/EvotecIT/DesktopManager'
            IconUri    = 'https://raw.githubusercontent.com/EvotecIT/DesktopManager/master/Assets/Icons/DesktopManager.png'
        }
    }
    RootModule             = 'DesktopManager.psm1'
}