@{
    AliasesToExport        = @('Get-DesktopMonitors', 'Set-LockScreenWallpaper', 'Get-LockScreenWallpaper')
    Author                 = 'Przemyslaw Klys'
    CmdletsToExport        = @('Get-DesktopMonitor', 'Get-DesktopWallpaper', 'Get-DesktopWindow', 'Invoke-DesktopScreenshot', 'Register-DesktopMonitorEvent', 'Register-DesktopOrientationEvent', 'Register-DesktopResolutionEvent', 'Set-DesktopPosition', 'Set-DesktopResolution', 'Set-DesktopDpiScaling', 'Set-DesktopWallpaper', 'Set-DesktopWindow', 'Set-DesktopWindowText', 'Save-DesktopWindowLayout', 'Restore-DesktopWindowLayout', 'Get-DesktopBackgroundColor', 'Set-DesktopBackgroundColor', 'Start-DesktopSlideshow', 'Stop-DesktopSlideshow', 'Advance-DesktopSlideshow', 'Get-LogonWallpaper', 'Set-LogonWallpaper')
    CompanyName            = 'Evotec'
    CompatiblePSEditions   = @('Desktop', 'Core')
    Copyright              = '(c) 2011 - 2025 Przemyslaw Klys @ Evotec. All rights reserved.'
    Description            = 'Desktop Manager is a PowerShell module that allows easy way to change wallpaper on multiple screens/monitors.'
    DotNetFrameworkVersion = '4.7.2'
    FunctionsToExport      = @()
    GUID                   = '56f85fa6-c622-4204-8e97-3d99e3e06e75'
    ModuleVersion        = '3.5.0'
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
