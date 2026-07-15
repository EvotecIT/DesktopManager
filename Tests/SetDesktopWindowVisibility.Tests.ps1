BeforeAll {
    . "$PSScriptRoot/TestBootstrap.ps1"
}

describe 'Set-DesktopWindowVisibility' {
    it 'supports WhatIf mode' -Skip:(-not $IsWindows) {
        { Set-DesktopWindowVisibility -Name '__DesktopManager_WhatIf_NoMatch__' -Show -WhatIf } | Should -Not -Throw
    }
}
