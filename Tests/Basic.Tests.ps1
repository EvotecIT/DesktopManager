Describe 'DesktopManager basic tests' {
    BeforeAll {
        Import-Module "$PSScriptRoot/..\DesktopManager.psd1" -Force
    }

    It 'Exports Get-DesktopMonitor' {
        Get-Command Get-DesktopMonitor | Should -Not -BeNullOrEmpty
    }
}
