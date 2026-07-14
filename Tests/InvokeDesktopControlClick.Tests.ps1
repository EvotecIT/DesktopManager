BeforeAll {
    . "$PSScriptRoot/TestBootstrap.ps1"
}

describe 'Invoke-DesktopControlClick' {
    it 'supports WhatIf mode' -Skip:(-not $IsWindows) {
        { Invoke-DesktopControlClick -Control ([DesktopManager.WindowControlInfo]::new()) -WhatIf } | Should -Not -Throw
    }
}
