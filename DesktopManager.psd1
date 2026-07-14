@{
    AliasesToExport        = @('Get-DesktopMonitors', 'Get-LockScreenWallpaper', 'Set-LockScreenWallpaper')
    Author                 = 'Przemyslaw Klys'
    CmdletsToExport        = @('Get-DesktopBackgroundColor', 'Get-DesktopBrightness', 'Get-DesktopClipboardText', 'Get-DesktopControlCheck', 'Get-DesktopControlState', 'Get-DesktopControlTarget', 'Get-DesktopFocusedControl', 'Get-DesktopHostedSessionDiagnostic', 'Get-DesktopMonitor', 'Get-DesktopMouseState', 'Get-DesktopSlideshow', 'Get-DesktopWallpaper', 'Get-DesktopWallpaperHistory', 'Get-DesktopWindow', 'Get-DesktopWindowControl', 'Get-DesktopWindowControlDiagnostic', 'Get-DesktopWindowGeometry', 'Get-DesktopWindowKeepAlive', 'Get-DesktopWindowProcessInfo', 'Get-DesktopWindowTarget', 'Get-DesktopWindowText', 'Get-LogonWallpaper', 'Invoke-DesktopControlClick', 'Invoke-DesktopKeyPress', 'Invoke-DesktopMouseClick', 'Invoke-DesktopMouseDrag', 'Invoke-DesktopMouseMove', 'Invoke-DesktopMouseScroll', 'Invoke-DesktopScreenshot', 'Invoke-DesktopWindowClick', 'Invoke-DesktopWindowDrag', 'Invoke-DesktopWindowScreenshot', 'Invoke-DesktopWindowScroll', 'Register-DesktopHotkey', 'Register-DesktopMonitorEvent', 'Register-DesktopOrientationEvent', 'Register-DesktopResolutionEvent', 'Restore-DesktopWindowLayout', 'Save-DesktopWindowLayout', 'Send-DesktopControlKey', 'Set-DefaultAudioDevice', 'Set-DesktopBackgroundColor', 'Set-DesktopBrightness', 'Set-DesktopClipboardText', 'Set-DesktopControlCheck', 'Set-DesktopControlEnabled', 'Set-DesktopControlFocus', 'Set-DesktopControlTarget', 'Set-DesktopControlText', 'Set-DesktopControlVisibility', 'Set-DesktopPosition', 'Set-DesktopResolution', 'Set-DesktopSlideshowOptions', 'Set-DesktopWallpaper', 'Set-DesktopWallpaperHistory', 'Set-DesktopWindow', 'Set-DesktopWindowPlacement', 'Set-DesktopWindowSnap', 'Set-DesktopWindowStyle', 'Set-DesktopWindowTarget', 'Set-DesktopWindowText', 'Set-DesktopWindowTransparency', 'Set-DesktopWindowVisibility', 'Set-LogonWallpaper', 'Set-TaskbarPosition', 'Start-DesktopProcess', 'Start-DesktopProcessAndWait', 'Start-DesktopSlideshow', 'Start-DesktopWindowKeepAlive', 'Step-DesktopSlideshow', 'Stop-DesktopSlideshow', 'Stop-DesktopWindowKeepAlive', 'Stop-DesktopWindowProcess', 'Test-DesktopElevation', 'Test-DesktopWindow', 'Test-DesktopWindowControl', 'Unregister-DesktopHotkey', 'Wait-DesktopFocusedControl', 'Wait-DesktopWindow', 'Wait-DesktopWindowClose', 'Wait-DesktopWindowControl', 'Wait-DesktopWindowInactive', 'Wait-DesktopWindowText')
    CompanyName            = 'Evotec'
    CompatiblePSEditions   = @('Desktop', 'Core')
    Copyright              = '(c) 2011 - 2026 Przemyslaw Klys @ Evotec. All rights reserved.'
    Description            = 'Desktop Manager is a PowerShell module that allows easy way to change wallpaper on multiple screens/monitors.'
    DotNetFrameworkVersion = '4.7.2'
    FunctionsToExport      = @()
    GUID                   = '56f85fa6-c622-4204-8e97-3d99e3e06e75'
    ModuleVersion          = '4.1.0'
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
