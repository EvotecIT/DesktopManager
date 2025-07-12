describe 'Set-DesktopWindowTransparency' {
    it 'supports WhatIf mode' -Skip:(-not $IsWindows) {
        { Set-DesktopWindowTransparency -Name '*' -Alpha 128 -WhatIf } | Should -Not -Throw
    }
}
