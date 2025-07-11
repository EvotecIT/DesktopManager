describe 'Invoke-DesktopMouseScroll' {
    it 'supports WhatIf mode' -Skip:(-not $IsWindows) {
        { Invoke-DesktopMouseScroll -Delta 120 -WhatIf } | Should -Not -Throw
    }
}
