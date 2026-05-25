@{
    AliasesToExport        = @('Get-DesktopMonitors', 'Get-LockScreenWallpaper', 'Set-LockScreenWallpaper')
    Author                 = 'Przemyslaw Klys'
    CmdletsToExport        = @('Advance-DesktopSlideshow', 'Get-DesktopBackgroundColor', 'Get-DesktopBrightness', 'Get-DesktopControlCheck', 'Get-DesktopMonitor', 'Get-DesktopSlideshow', 'Get-DesktopWallpaper', 'Get-DesktopWallpaperHistory', 'Get-DesktopWindow', 'Get-DesktopWindowControl', 'Get-DesktopWindowKeepAlive', 'Get-DesktopWindowProcessInfo', 'Get-LogonWallpaper', 'Invoke-DesktopControlClick', 'Invoke-DesktopKeyPress', 'Invoke-DesktopMouseClick', 'Invoke-DesktopMouseDrag', 'Invoke-DesktopMouseMove', 'Invoke-DesktopMouseScroll', 'Invoke-DesktopScreenshot', 'Invoke-DesktopWindowScreenshot', 'Register-DesktopHotkey', 'Register-DesktopMonitorEvent', 'Register-DesktopOrientationEvent', 'Register-DesktopResolutionEvent', 'Restore-DesktopWindowLayout', 'Save-DesktopWindowLayout', 'Send-DesktopControlKey', 'Set-DefaultAudioDevice', 'Set-DesktopBackgroundColor', 'Set-DesktopBrightness', 'Set-DesktopControlCheck', 'Set-DesktopDpiScaling', 'Set-DesktopPosition', 'Set-DesktopResolution', 'Set-DesktopSlideshowOptions', 'Set-DesktopWallpaper', 'Set-DesktopWallpaperHistory', 'Set-DesktopWindow', 'Set-DesktopWindowSnap', 'Set-DesktopWindowStyle', 'Set-DesktopWindowText', 'Set-DesktopWindowTransparency', 'Set-DesktopWindowVisibility', 'Set-LogonWallpaper', 'Set-TaskbarPosition', 'Start-DesktopSlideshow', 'Start-DesktopWindowKeepAlive', 'Stop-DesktopSlideshow', 'Stop-DesktopWindowKeepAlive', 'Unregister-DesktopHotkey', 'Wait-DesktopWindow')
    CompanyName            = 'Evotec'
    CompatiblePSEditions   = @('Desktop', 'Core')
    Copyright              = '(c) 2011 - 2026 Przemyslaw Klys @ Evotec. All rights reserved.'
    Description            = 'Desktop Manager is a PowerShell module that allows easy way to change wallpaper on multiple screens/monitors.'
    DotNetFrameworkVersion = '4.7.2'
    FunctionsToExport      = @()
    GUID                   = '56f85fa6-c622-4204-8e97-3d99e3e06e75'
    ModuleVersion          = '3.6.1'
    PowerShellVersion      = '5.1'
    PrivateData            = @{
        PSData = @{
            IconUri                    = 'https://evotec.xyz/wp-content/uploads/2022/12/DesktopManager.png'
            ProjectUri                 = 'https://github.com/EvotecIT/DesktopManager'
            RequireLicenseAcceptance   = $false
            Tags                       = @('windows', 'image', 'wallpaper', 'monitor')
            ExternalModuleDependencies = @()
        }
    }
    RootModule             = 'DesktopManager.psm1'
    RequiredModules        = @()
    ScriptsToProcess       = @()
}
