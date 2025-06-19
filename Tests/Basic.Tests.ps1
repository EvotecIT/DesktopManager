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
}
