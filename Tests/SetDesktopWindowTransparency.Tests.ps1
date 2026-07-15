BeforeAll {
    . "$PSScriptRoot/TestBootstrap.ps1"
}

describe 'Set-DesktopWindowTransparency' {
    it 'supports WhatIf mode' -Skip:(-not $IsWindows) {
        { Set-DesktopWindowTransparency -Name '__DesktopManager_WhatIf_NoMatch__' -Alpha 128 -WhatIf } | Should -Not -Throw
    }
}
