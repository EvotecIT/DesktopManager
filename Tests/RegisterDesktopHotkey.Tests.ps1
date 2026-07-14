BeforeAll {
    . "$PSScriptRoot/TestBootstrap.ps1"
}

describe 'Register-DesktopHotkey' {
    it 'supports WhatIf mode' -Skip:(-not $IsWindows) {
        { Register-DesktopHotkey -Key ([DesktopManager.VirtualKey]::VK_F24) -Modifiers ([DesktopManager.HotkeyModifiers]::Control) -Action { } -WhatIf } | Should -Not -Throw
    }
}

describe 'Unregister-DesktopHotkey' {
    it 'supports WhatIf mode' -Skip:(-not $IsWindows) {
        { Unregister-DesktopHotkey -Id 1 -WhatIf } | Should -Not -Throw
    }
}
