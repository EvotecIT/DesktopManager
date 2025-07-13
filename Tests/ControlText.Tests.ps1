describe 'Desktop control text cmdlets' {
    it 'supports WhatIf for Set-DesktopControlText' -Skip:(-not $IsWindows) {
        { Set-DesktopControlText -Control ([DesktopManager.WindowControlInfo]::new()) -Text 't' -WhatIf } | Should -Not -Throw
    }

    it 'can set and get text on Notepad edit control' -Skip:(-not $IsWindows) {
        $proc = Start-Process notepad -PassThru
        try {
            $window = Wait-DesktopWindow -Name '*Notepad*' -TimeoutMs 10000
            $control = Get-DesktopWindowControl -Name '*Notepad*' | Where-Object { $_.ClassName -eq 'Edit' } | Select-Object -First 1
            $control | Should -Not -BeNullOrEmpty
            Set-DesktopControlText -Control $control -Text 'Hello'
            Start-Sleep -Milliseconds 200
            Get-DesktopControlText -Control $control | Should -Be 'Hello'
        } finally {
            $proc | Stop-Process -ErrorAction SilentlyContinue
        }
    }
}
