describe 'Invoke-DesktopKeyPress' {
    it 'supports WhatIf mode' -Skip:(-not $IsWindows) {
        { Invoke-DesktopKeyPress -Keys @([DesktopManager.VirtualKey]::VK_F24) -WhatIf } | Should -Not -Throw
    }

    it 'supports KeyDown mode' -Skip:(-not $IsWindows) {
        { Invoke-DesktopKeyPress -Keys @([DesktopManager.VirtualKey]::VK_F24) -KeyDown -WhatIf } | Should -Not -Throw
    }

    it 'supports KeyUp mode' -Skip:(-not $IsWindows) {
        { Invoke-DesktopKeyPress -Keys @([DesktopManager.VirtualKey]::VK_F24) -KeyUp -WhatIf } | Should -Not -Throw
    }
}
