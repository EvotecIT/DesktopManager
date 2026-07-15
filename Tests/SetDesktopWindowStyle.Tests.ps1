BeforeAll {
    . "$PSScriptRoot/TestBootstrap.ps1"
}

describe 'Set-DesktopWindowStyle' {
    it 'supports WhatIf mode' -Skip:(-not $IsWindows) {
        { Set-DesktopWindowStyle -Name '__DesktopManager_WhatIf_NoMatch__' -ExStyle TopMost -WhatIf } | Should -Not -Throw
    }
}
