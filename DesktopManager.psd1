@{
    AliasesToExport        = @('Get-DesktopMonitors')
    Author                 = 'Przemyslaw Klys'
    CmdletsToExport        = @('Advance-DesktopSlideshow', 'Get-DesktopBackgroundColor', 'Get-DesktopBrightness', 'Get-DesktopMonitor', 'Get-DesktopWallpaper', 'Get-DesktopWallpaperHistory', 'Get-DesktopWindow', 'Get-DesktopWindowKeepAlive', 'Invoke-DesktopScreenshot', 'Register-DesktopMonitorEvent', 'Register-DesktopOrientationEvent', 'Register-DesktopResolutionEvent', 'Restore-DesktopWindowLayout', 'Save-DesktopWindowLayout', 'Set-DesktopBackgroundColor', 'Set-DesktopBrightness', 'Set-DesktopDpiScaling', 'Set-DesktopPosition', 'Set-DesktopResolution', 'Set-DesktopWallpaper', 'Set-DesktopWallpaperHistory', 'Set-DesktopWindow', 'Set-DesktopWindowSnap', 'Set-DesktopWindowText', 'Start-DesktopSlideshow', 'Start-DesktopWindowKeepAlive', 'Stop-DesktopSlideshow', 'Stop-DesktopWindowKeepAlive')
    CompanyName            = 'Evotec'
    CompatiblePSEditions   = @('Desktop', 'Core')
    Copyright              = '(c) 2011 - 2025 Przemyslaw Klys @ Evotec. All rights reserved.'
    Description            = 'Desktop Manager is a PowerShell module that allows easy way to change wallpaper on multiple screens/monitors.'
    DotNetFrameworkVersion = '4.7.2'
    FunctionsToExport      = @()
    GUID                   = '56f85fa6-c622-4204-8e97-3d99e3e06e75'
    ModuleVersion          = '3.5.0'
    PowerShellVersion      = '5.1'
    PrivateData            = @{
        PSData = @{
            IconUri    = 'https://evotec.xyz/wp-content/uploads/2022/12/DesktopManager.png'
            ProjectUri = 'https://github.com/EvotecIT/DesktopManager'
            Tags       = @('windows', 'image', 'wallpaper', 'monitor')
        }
    }
    RootModule             = 'DesktopManager.psm1'
}