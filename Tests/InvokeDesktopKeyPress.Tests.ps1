describe 'Invoke-DesktopKeyPress' {
    it 'supports WhatIf mode' -Skip:(-not $IsWindows) {
        { Invoke-DesktopKeyPress -Keys @([VirtualKey]::VK_F24) -WhatIf } | Should -Not -Throw
    }
}
