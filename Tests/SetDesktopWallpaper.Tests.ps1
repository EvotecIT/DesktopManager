Describe 'Set-DesktopWallpaper parameter validation' {
    BeforeAll {
        . "$PSScriptRoot/TestBootstrap.ps1"
    }

    It 'Throws when using Index with DeviceId' {
        { Set-DesktopWallpaper -Index 1 -DeviceId 'Dummy' -WallpaperPath '/' } |
            Should -Throw -ErrorId 'AmbiguousParameterSet,DesktopManager.PowerShell.CmdletSetDesktopWallpaper'
    }

    It 'Throws when using Index with DeviceName' {
        { Set-DesktopWallpaper -Index 1 -DeviceName 'Dummy' -WallpaperPath '/' } |
            Should -Throw -ErrorId 'AmbiguousParameterSet,DesktopManager.PowerShell.CmdletSetDesktopWallpaper'
    }
}
