describe 'Set-DesktopWindowStyle' {
    it 'supports WhatIf mode' -Skip:(-not $IsWindows) {
        { Set-DesktopWindowStyle -Name '*' -ExStyle TopMost -WhatIf } | Should -Not -Throw
    }
}

