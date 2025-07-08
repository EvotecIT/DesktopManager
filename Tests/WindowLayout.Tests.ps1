describe 'Save-DesktopWindowLayout and Restore-DesktopWindowLayout' {
    it 'supports WhatIf for saving' {
        { Save-DesktopWindowLayout -Path "$env:TEMP/layout.json" -WhatIf } | Should -Not -Throw
    }
    it 'supports WhatIf for restoring' {
        { Restore-DesktopWindowLayout -Path "$env:TEMP/layout.json" -WhatIf } | Should -Not -Throw
    }
}
