BeforeAll {
    . "$PSScriptRoot/TestBootstrap.ps1"
}

describe 'Set-DesktopControlCheck' {
    it 'supports WhatIf mode' -Skip:(-not $IsWindows) {
        { Set-DesktopControlCheck -Control ([DesktopManager.WindowControlInfo]::new()) -Check $true -WhatIf } | Should -Not -Throw
    }
}
