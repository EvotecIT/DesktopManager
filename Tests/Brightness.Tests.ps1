BeforeAll {
    Import-Module "$PSScriptRoot/../DesktopManager.psd1" -Force
}

describe 'Brightness cmdlets' {
    it 'exports Get-DesktopBrightness' -Skip:(-not $IsWindows) {
        Get-Command Get-DesktopBrightness | Should -Not -BeNullOrEmpty
    }
    it 'exports Set-DesktopBrightness' -Skip:(-not $IsWindows) {
        Get-Command Set-DesktopBrightness | Should -Not -BeNullOrEmpty
    }
    it 'supports WhatIf for Set-DesktopBrightness' -Skip:(-not $IsWindows) {
        { Set-DesktopBrightness -Index 0 -Brightness 50 -WhatIf } | Should -Not -Throw
    }

    It 'Throws when using Index with DeviceId' -Skip:(-not $IsWindows) {
        { Set-DesktopBrightness -Index 0 -DeviceId 'Dummy' -Brightness 50 } |
            Should -Throw -ErrorId 'AmbiguousParameterSet,DesktopManager.PowerShell.CmdletSetDesktopBrightness'
    }

    It 'Throws when using Index with DeviceName' -Skip:(-not $IsWindows) {
        { Set-DesktopBrightness -Index 0 -DeviceName 'Dummy' -Brightness 50 } |
            Should -Throw -ErrorId 'AmbiguousParameterSet,DesktopManager.PowerShell.CmdletSetDesktopBrightness'
    }
}

