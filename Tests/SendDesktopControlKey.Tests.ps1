BeforeAll {
    . "$PSScriptRoot/TestBootstrap.ps1"
}

describe 'Send-DesktopControlKey' {
    it 'supports WhatIf mode' -Skip:(-not $IsWindows) {
        { Send-DesktopControlKey -Control ([DesktopManager.WindowControlInfo]::new()) -Keys @([DesktopManager.VirtualKey]::VK_F24) -WhatIf } | Should -Not -Throw
    }
}
