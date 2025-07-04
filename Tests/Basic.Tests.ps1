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

    It 'Exports Start-DesktopWindowKeepAlive' {
        Get-Command Start-DesktopWindowKeepAlive | Should -Not -BeNullOrEmpty
    }

    It 'Exports Stop-DesktopWindowKeepAlive' {
        Get-Command Stop-DesktopWindowKeepAlive | Should -Not -BeNullOrEmpty
    }

    It 'Exports Get-DesktopWindowKeepAlive' {
        Get-Command Get-DesktopWindowKeepAlive | Should -Not -BeNullOrEmpty
    }
}
