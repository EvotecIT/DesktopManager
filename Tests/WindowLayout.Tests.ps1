describe 'Save-DesktopWindowLayout and Restore-DesktopWindowLayout' {
    it 'supports WhatIf for saving' -Skip:(-not $IsWindows) {
        { Save-DesktopWindowLayout -Path "$env:TEMP/layout.json" -WhatIf } | Should -Not -Throw
    }
    it 'supports WhatIf for restoring' -Skip:(-not $IsWindows) {
        { Restore-DesktopWindowLayout -Path "$env:TEMP/layout.json" -WhatIf } | Should -Not -Throw
    }
}
