BeforeAll {
    Import-Module "$PSScriptRoot/..\DesktopManager.psd1" -Force
}

describe 'Set-LogonWallpaper' {
    it 'supports WhatIf mode' -Skip:(-not $IsWindows) {
        $tmp = New-TemporaryFile
        try {
            { Set-LogonWallpaper -ImagePath $tmp -WhatIf } | Should -Not -Throw
        } finally {
            Remove-Item $tmp -ErrorAction SilentlyContinue
        }
    }
}

describe 'Set-LockScreenWallpaper alias' {
    it 'supports WhatIf mode' -Skip:(-not $IsWindows) {
        $tmp = New-TemporaryFile
        try {
            { Set-LockScreenWallpaper -ImagePath $tmp -WhatIf } | Should -Not -Throw
        } finally {
            Remove-Item $tmp -ErrorAction SilentlyContinue
        }
    }
}
