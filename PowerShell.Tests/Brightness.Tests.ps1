BeforeAll {
    Import-Module "$PSScriptRoot/../DesktopManager.psd1" -Force
}

describe 'Brightness cmdlets' {
    it 'exports Get-DesktopBrightness' {
        Get-Command Get-DesktopBrightness | Should -Not -BeNullOrEmpty
    }
    it 'exports Set-DesktopBrightness' {
        Get-Command Set-DesktopBrightness | Should -Not -BeNullOrEmpty
    }
}
