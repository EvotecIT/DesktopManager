BeforeAll {
    Import-Module "$PSScriptRoot/..\DesktopManager.psd1" -Force
}

describe 'Set-TaskbarPosition' {
    it 'supports WhatIf mode' -Skip:(-not $IsWindows) {
        { Set-TaskbarPosition -Index 0 -Position Bottom -WhatIf } | Should -Not -Throw
    }
}
