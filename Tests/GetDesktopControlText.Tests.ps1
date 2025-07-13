BeforeAll {
    Import-Module "$PSScriptRoot/..\DesktopManager.psd1" -Force
}

describe 'Get-DesktopControlText' {
    it 'retrieves text from Notepad' -Skip:(-not $IsWindows) {
        $proc = Start-Process notepad -PassThru
        try {
            $window = Wait-DesktopWindow -Name '*Notepad*' -TimeoutMs 10000
            $ctrl = Get-DesktopWindowControl -Name '*Notepad*' | Where-Object ClassName -eq 'Edit' | Select-Object -First 1
            Set-DesktopControlText -Control $ctrl -Text 'Test123'
            Start-Sleep -Milliseconds 200
            (Get-DesktopControlText -Control $ctrl) | Should -Be 'Test123'
        } finally {
            $proc | Stop-Process -ErrorAction SilentlyContinue
        }
    }
}
