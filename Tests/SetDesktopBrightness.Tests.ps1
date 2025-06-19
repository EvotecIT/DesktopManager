BeforeAll {
    Import-Module "$PSScriptRoot/../DesktopManager.psd1" -Force
}

describe 'Set-DesktopBrightness' {
    it 'cmdlet is available' {
        Get-Command Set-DesktopBrightness | Should -Not -BeNullOrEmpty
    }
}
