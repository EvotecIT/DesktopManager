describe 'Brightness cmdlets' {
    it 'exports Get-DesktopBrightness' {
        Get-Command Get-DesktopBrightness | Should -Not -BeNullOrEmpty
    }
    it 'exports Set-DesktopBrightness' {
        Get-Command Set-DesktopBrightness | Should -Not -BeNullOrEmpty
    }
    it 'supports WhatIf for Set-DesktopBrightness' -Skip:(-not $IsWindows) {
        { Set-DesktopBrightness -Index 0 -Brightness 50 -WhatIf } | Should -Not -Throw
    }
}

