describe 'Invoke-DesktopMouseClick' {
    it 'supports WhatIf mode' -Skip:(-not $IsWindows) {
        { Invoke-DesktopMouseClick -Button Left -WhatIf } | Should -Not -Throw
    }
}
