describe 'Invoke-DesktopWindowScreenshot' {
    it 'captures screenshot of window' -Skip:(-not $IsWindows) {
        $proc = Start-Process notepad -PassThru
        try {
            $window = Wait-DesktopWindow -Name '*Notepad*' -TimeoutMs 10000
            $bmp = Invoke-DesktopWindowScreenshot -Window $window
            $rect = New-Object DesktopManager.RECT
            [DesktopManager.MonitorNativeMethods]::GetWindowRect($window.Handle, [ref]$rect) | Out-Null
            $bmp.Width | Should -Be ($rect.Right - $rect.Left)
            $bmp.Height | Should -Be ($rect.Bottom - $rect.Top)
            $bmp.Dispose()
        } finally {
            $proc | Stop-Process -ErrorAction SilentlyContinue
        }
    }
    it 'captures screenshot of control' -Skip:(-not $IsWindows) {
        $proc = Start-Process notepad -PassThru
        try {
            $window = Wait-DesktopWindow -Name '*Notepad*' -TimeoutMs 10000
            $control = Get-DesktopWindowControl -Name '*Notepad*' | Where-Object ClassName -eq 'Edit' | Select-Object -First 1
            $bmp = Invoke-DesktopWindowScreenshot -Control $control
            $rect = New-Object DesktopManager.RECT
            [DesktopManager.MonitorNativeMethods]::GetWindowRect($control.Handle, [ref]$rect) | Out-Null
            $bmp.Width | Should -Be ($rect.Right - $rect.Left)
            $bmp.Height | Should -Be ($rect.Bottom - $rect.Top)
            $bmp.Dispose()
        } finally {
            $proc | Stop-Process -ErrorAction SilentlyContinue
        }
    }
}
