@{
    AliasesToExport        = @('Get-DesktopMonitors', 'Get-LockScreenWallpaper', 'Set-LockScreenWallpaper')
    Author                 = 'Przemyslaw Klys'
    CmdletsToExport        = @('Advance-DesktopSlideshow', 'Get-DesktopBackgroundColor', 'Get-DesktopBrightness', 'Get-DesktopControlText', 'Get-DesktopMonitor', 'Get-DesktopWallpaper', 'Get-DesktopWallpaperHistory', 'Get-DesktopWindow', 'Get-DesktopWindowControl', 'Get-DesktopWindowKeepAlive', 'Get-LogonWallpaper', 'Invoke-DesktopControlClick', 'Invoke-DesktopKeyPress', 'Invoke-DesktopMouseClick', 'Invoke-DesktopMouseDrag', 'Invoke-DesktopMouseMove', 'Invoke-DesktopMouseScroll', 'Invoke-DesktopScreenshot', 'Invoke-DesktopWindowScreenshot', 'Register-DesktopMonitorEvent', 'Register-DesktopOrientationEvent', 'Register-DesktopResolutionEvent', 'Restore-DesktopWindowLayout', 'Save-DesktopWindowLayout', 'Set-DefaultAudioDevice', 'Set-DesktopBackgroundColor', 'Set-DesktopBrightness', 'Set-DesktopControlText', 'Set-DesktopDpiScaling', 'Set-DesktopPosition', 'Set-DesktopResolution', 'Set-DesktopWallpaper', 'Set-DesktopWallpaperHistory', 'Set-DesktopWindow', 'Set-DesktopWindowSnap', 'Set-DesktopWindowStyle', 'Set-DesktopWindowText', 'Set-DesktopWindowTransparency', 'Set-LogonWallpaper', 'Set-TaskbarPosition', 'Start-DesktopSlideshow', 'Start-DesktopWindowKeepAlive', 'Stop-DesktopSlideshow', 'Stop-DesktopWindowKeepAlive', 'Wait-DesktopWindow')
    CompanyName            = 'Evotec'
    CompatiblePSEditions   = @('Desktop', 'Core')
    Copyright              = '(c) 2011 - 2025 Przemyslaw Klys @ Evotec. All rights reserved.'
    Description            = 'Desktop Manager is a PowerShell module that allows easy way to change wallpaper on multiple screens/monitors.'
    DotNetFrameworkVersion = '4.7.2'
    FunctionsToExport      = @()
    GUID                   = '56f85fa6-c622-4204-8e97-3d99e3e06e75'
    ModuleVersion          = '3.6.0'
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