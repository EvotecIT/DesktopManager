describe 'Send-DesktopControlKey' {
    it 'supports WhatIf mode' -Skip:(-not $IsWindows) {
        { Send-DesktopControlKey -Control ([DesktopManager.WindowControlInfo]::new()) -Keys @([DesktopManager.VirtualKey]::VK_F24) -WhatIf } | Should -Not -Throw
    }

    it 'sends keys to background control' -Skip:(-not $IsWindows) {
        $proc1 = Start-Process notepad -PassThru
        $proc2 = Start-Process notepad -PassThru
        try {
            $win1 = Wait-DesktopWindow -Name '*Notepad*' -ProcessId $proc1.Id -TimeoutMs 10000
            $win2 = Wait-DesktopWindow -Name '*Notepad*' -ProcessId $proc2.Id -TimeoutMs 10000
            [DesktopManager.MonitorNativeMethods]::SetForegroundWindow($win2.Handle) | Out-Null
            $control = Get-DesktopWindowControl -Name '*Notepad*' -ProcessId $proc1.Id | Where-Object ClassName -eq 'Edit' | Select-Object -First 1
            Send-DesktopControlKey -Control $control -Keys @([DesktopManager.VirtualKey]::VK_H, [DesktopManager.VirtualKey]::VK_I)
            Start-Sleep -Milliseconds 500
            $sb = New-Object System.Text.StringBuilder 256
            [DesktopManager.MonitorNativeMethods]::GetWindowText($control.Handle, $sb, $sb.Capacity) | Out-Null
            $sb.ToString() | Should -Match 'HI$'
        }
        finally {
            $proc1 | Stop-Process -ErrorAction SilentlyContinue
            $proc2 | Stop-Process -ErrorAction SilentlyContinue
        }
    }
}
