@{
    AliasesToExport        = @('Get-DesktopMonitors', 'Get-LockScreenWallpaper', 'Set-LockScreenWallpaper')
    Author                 = 'Przemyslaw Klys'
    CmdletsToExport        = @('Exit-DesktopSession', 'Get-DesktopAirplaneMode', 'Get-DesktopAudioEndpoint', 'Get-DesktopBackgroundColor', 'Get-DesktopBrightness', 'Get-DesktopClipboardText', 'Get-DesktopControlCheck', 'Get-DesktopControlState', 'Get-DesktopControlTarget', 'Get-DesktopFocusedControl', 'Get-DesktopHostedSessionDiagnostic', 'Get-DesktopMonitor', 'Get-DesktopMouseState', 'Get-DesktopPersonalization', 'Get-DesktopPowerStatus', 'Get-DesktopRadio', 'Get-DesktopSession', 'Get-DesktopSlideshow', 'Get-DesktopTaskbar', 'Get-DesktopVirtualDesktop', 'Get-DesktopWallpaper', 'Get-DesktopWallpaperHistory', 'Get-DesktopWindow', 'Get-DesktopWindowControl', 'Get-DesktopWindowControlDiagnostic', 'Get-DesktopWindowGeometry', 'Get-DesktopWindowKeepAlive', 'Get-DesktopWindowProcessInfo', 'Get-DesktopWindowTarget', 'Get-DesktopWindowText', 'Get-DesktopWorkstationProfile', 'Get-LogonWallpaper', 'Invoke-DesktopControlClick', 'Invoke-DesktopKeyPress', 'Invoke-DesktopMouseClick', 'Invoke-DesktopMouseDrag', 'Invoke-DesktopMouseMove', 'Invoke-DesktopMouseScroll', 'Invoke-DesktopScreenshot', 'Invoke-DesktopWindowClick', 'Invoke-DesktopWindowDrag', 'Invoke-DesktopWindowScreenshot', 'Invoke-DesktopWindowScroll', 'Lock-DesktopSession', 'Move-DesktopWindowToVirtualDesktop', 'Register-DesktopAudioEvent', 'Register-DesktopHotkey', 'Register-DesktopMonitorEvent', 'Register-DesktopOrientationEvent', 'Register-DesktopRadioEvent', 'Register-DesktopResolutionEvent', 'Register-DesktopSessionEvent', 'Remove-DesktopPersonalization', 'Remove-DesktopWorkstationProfile', 'Restore-DesktopPersonalization', 'Restore-DesktopWindowLayout', 'Restore-DesktopWorkstationProfile', 'Save-DesktopPersonalization', 'Save-DesktopWindowLayout', 'Save-DesktopWorkstationProfile', 'Send-DesktopControlKey', 'Set-DefaultAudioDevice', 'Set-DesktopAirplaneMode', 'Set-DesktopAudioEndpoint', 'Set-DesktopBackgroundColor', 'Set-DesktopBrightness', 'Set-DesktopClipboardText', 'Set-DesktopControlCheck', 'Set-DesktopControlEnabled', 'Set-DesktopControlFocus', 'Set-DesktopControlTarget', 'Set-DesktopControlText', 'Set-DesktopControlVisibility', 'Set-DesktopPersonalization', 'Set-DesktopPosition', 'Set-DesktopRadio', 'Set-DesktopResolution', 'Set-DesktopSlideshowOptions', 'Set-DesktopTaskbarAutoHide', 'Set-DesktopWallpaper', 'Set-DesktopWallpaperHistory', 'Set-DesktopWindow', 'Set-DesktopWindowPlacement', 'Set-DesktopWindowSnap', 'Set-DesktopWindowStyle', 'Set-DesktopWindowTarget', 'Set-DesktopWindowText', 'Set-DesktopWindowTransparency', 'Set-DesktopWindowVisibility', 'Set-LogonWallpaper', 'Set-TaskbarPosition', 'Start-DesktopKeepAwake', 'Start-DesktopProcess', 'Start-DesktopProcessAndWait', 'Start-DesktopSlideshow', 'Start-DesktopWindowKeepAlive', 'Step-DesktopSlideshow', 'Stop-DesktopSlideshow', 'Stop-DesktopWindowKeepAlive', 'Stop-DesktopWindowProcess', 'Suspend-DesktopSystem', 'Test-DesktopElevation', 'Test-DesktopWindow', 'Test-DesktopWindowControl', 'Unregister-DesktopHotkey', 'Wait-DesktopFocusedControl', 'Wait-DesktopWindow', 'Wait-DesktopWindowClose', 'Wait-DesktopWindowControl', 'Wait-DesktopWindowInactive', 'Wait-DesktopWindowText')
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
