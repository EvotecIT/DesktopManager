Describe 'DesktopManager basic tests' {
    BeforeAll {
        Import-Module "$PSScriptRoot/..\DesktopManager.psd1" -Force
    }

    It 'Exports Get-DesktopMonitor' {
        Get-Command Get-DesktopMonitor | Should -Not -BeNullOrEmpty
    }

    It 'Exports Save-DesktopWindowLayout' {
        Get-Command Save-DesktopWindowLayout | Should -Not -BeNullOrEmpty
    }

    It 'Exports Restore-DesktopWindowLayout' {
        Get-Command Restore-DesktopWindowLayout | Should -Not -BeNullOrEmpty
    }

    It 'Exports Get-DesktopBackgroundColor' {
        Get-Command Get-DesktopBackgroundColor | Should -Not -BeNullOrEmpty
    }

    It 'Exports Set-DesktopBackgroundColor' {
        Get-Command Set-DesktopBackgroundColor | Should -Not -BeNullOrEmpty
    }

    It 'Exports Register-DesktopOrientationEvent' {
        Get-Command Register-DesktopOrientationEvent | Should -Not -BeNullOrEmpty
    }

    It 'Exports Register-DesktopResolutionEvent' {
        Get-Command Register-DesktopResolutionEvent | Should -Not -BeNullOrEmpty
    }
    It 'Exports Get-DesktopWallpaperHistory' {
        Get-Command Get-DesktopWallpaperHistory | Should -Not -BeNullOrEmpty
    }
    It 'Exports Set-DesktopWallpaperHistory' {
        Get-Command Set-DesktopWallpaperHistory | Should -Not -BeNullOrEmpty
    }

    It 'Exports Get-DesktopSlideshow' {
        Get-Command Get-DesktopSlideshow | Should -Not -BeNullOrEmpty
    }

    It 'Exports Set-DesktopSlideshowOptions' {
        Get-Command Set-DesktopSlideshowOptions | Should -Not -BeNullOrEmpty
    }

    It 'Exports Start-DesktopWindowKeepAlive' {
        Get-Command Start-DesktopWindowKeepAlive | Should -Not -BeNullOrEmpty
    }

    It 'Exports Stop-DesktopWindowKeepAlive' {
        Get-Command Stop-DesktopWindowKeepAlive | Should -Not -BeNullOrEmpty
    }

    It 'Exports Get-DesktopWindowKeepAlive' {
        Get-Command Get-DesktopWindowKeepAlive | Should -Not -BeNullOrEmpty
    }
    It 'Exports Wait-DesktopWindow' {
        Get-Command Wait-DesktopWindow | Should -Not -BeNullOrEmpty
    }

    It 'Exports Get-DesktopWindowControl' {
        Get-Command Get-DesktopWindowControl | Should -Not -BeNullOrEmpty
    }
    It 'Exports Get-DesktopControlCheck' {
        Get-Command Get-DesktopControlCheck | Should -Not -BeNullOrEmpty
    }
    It 'Exports Set-DesktopControlCheck' {
        Get-Command Set-DesktopControlCheck | Should -Not -BeNullOrEmpty
    }
    It 'Exports Set-DefaultAudioDevice' {
        Get-Command Set-DefaultAudioDevice | Should -Not -BeNullOrEmpty
    }


    It 'Exports Set-DesktopWindowText' {
        Get-Command Set-DesktopWindowText | Should -Not -BeNullOrEmpty
    }

    It 'Exports Set-DesktopWindowTransparency' {
        Get-Command Set-DesktopWindowTransparency | Should -Not -BeNullOrEmpty
    }

    It 'Exports Set-DesktopWindowVisibility' {
        Get-Command Set-DesktopWindowVisibility | Should -Not -BeNullOrEmpty
    }

    It 'Exports Set-LogonWallpaper' {
        Get-Command Set-LogonWallpaper | Should -Not -BeNullOrEmpty
    }

    It 'Exports Get-LogonWallpaper' {
        Get-Command Get-LogonWallpaper | Should -Not -BeNullOrEmpty
    }

    It 'Exports Set-LockScreenWallpaper alias' {
        Get-Command Set-LockScreenWallpaper | Should -Not -BeNullOrEmpty
    }

    It 'Exports Get-LockScreenWallpaper alias' {
        Get-Command Get-LockScreenWallpaper | Should -Not -BeNullOrEmpty
    }

    It 'Exports Register-DesktopHotkey' {
        Get-Command Register-DesktopHotkey | Should -Not -BeNullOrEmpty
    }

    It 'Exports Unregister-DesktopHotkey' {
        Get-Command Unregister-DesktopHotkey | Should -Not -BeNullOrEmpty
    }
}
