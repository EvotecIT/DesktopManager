BeforeAll {
    Import-Module "$PSScriptRoot/../DesktopManager.psd1" -Force
}

describe 'Set-DesktopDpiScaling' {
    it 'supports WhatIf mode' -Skip:(-not $IsWindows) {
        { Set-DesktopDpiScaling -Index 0 -Scaling 100 -WhatIf } | Should -Not -Throw
    }
}
