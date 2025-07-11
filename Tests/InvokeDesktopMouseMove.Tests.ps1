describe 'Invoke-DesktopMouseMove' {
    it 'supports WhatIf mode' -Skip:(-not $IsWindows) {
        { Invoke-DesktopMouseMove -X 0 -Y 0 -WhatIf } | Should -Not -Throw
    }
}
