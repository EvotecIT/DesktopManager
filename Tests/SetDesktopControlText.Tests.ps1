BeforeAll {
    Import-Module "$PSScriptRoot/..\DesktopManager.psd1" -Force
}

describe 'Set-DesktopControlText' {
    it 'supports WhatIf mode' -Skip:(-not $IsWindows) {
        { Set-DesktopControlText -Control ([DesktopManager.WindowControlInfo]::new()) -Text 'x' -WhatIf } | Should -Not -Throw
    }
}

