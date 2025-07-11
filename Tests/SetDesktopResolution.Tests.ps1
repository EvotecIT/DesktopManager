describe 'Set-DesktopResolution' {
    it 'supports WhatIf mode' -Skip:(-not $IsWindows) {
        { Set-DesktopResolution -Width 800 -Height 600 -WhatIf } | Should -Not -Throw
    }

    it 'accepts orientation parameter' -Skip:(-not $IsWindows) {
        { Set-DesktopResolution -Width 800 -Height 600 -Orientation Default -WhatIf } | Should -Not -Throw
    }
}

