BeforeAll {
    Import-Module "$PSScriptRoot/..\DesktopManager.psd1" -Force
}

describe 'Get-DesktopControlText' {
    it 'returns text' -Skip:(-not $IsWindows) {
        $info = [DesktopManager.WindowControlInfo]::new()
        { Get-DesktopControlText -Control $info } | Should -Not -Throw
    }
}

