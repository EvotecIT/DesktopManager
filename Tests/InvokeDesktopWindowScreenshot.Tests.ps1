$RunInteractive = $false
if ($env:RUN_UI_TESTS -eq 'true' -or $env:DESKTOPMANAGER_RUN_UI_TESTS -eq 'true') {
    $RunInteractive = $true
}

BeforeAll {
    . "$PSScriptRoot/TestBootstrap.ps1"
}

describe 'Invoke-DesktopWindowScreenshot' -Tag 'Interactive' {
    it 'captures screenshot of window' -Skip:(-not $IsWindows -or -not $RunInteractive) {
        $proc = Start-Process notepad -PassThru -WindowStyle Minimized
        try {
            $window = Wait-DesktopWindow -Name '*Notepad*' -TimeoutMs 10000
            $bmp = Invoke-DesktopWindowScreenshot -Window $window
            $rect = New-Object DesktopManager.RECT
            [DesktopManager.MonitorNativeMethods]::GetWindowRect($window.Handle, [ref]$rect) | Out-Null
            $bmp.Width | Should -Be ($rect.Right - $rect.Left)
            $bmp.Height | Should -Be ($rect.Bottom - $rect.Top)
            $bmp.Dispose()
        } finally {
            if ($null -ne $proc) {
                try {
                    $null = $proc.CloseMainWindow()
                } catch {
                    # Ignore close errors
                }
                $proc | Wait-Process -Timeout 3 -ErrorAction SilentlyContinue
                if (-not $proc.HasExited) {
                    $proc | Stop-Process -Force -ErrorAction SilentlyContinue
                }
            }
        }
    }
    it 'captures screenshot of control' -Skip:(-not $IsWindows -or -not $RunInteractive) {
        $proc = Start-Process notepad -PassThru -WindowStyle Minimized
        try {
            $window = Wait-DesktopWindow -Name '*Notepad*' -TimeoutMs 10000
            $editableClasses = @('Edit', 'RichEditD2DPT', 'NotepadTextBox')
            $control = Get-DesktopWindowControl -Name '*Notepad*' | Where-Object { $editableClasses -contains $_.ClassName } | Select-Object -First 1
            $bmp = Invoke-DesktopWindowScreenshot -Control $control
            $rect = New-Object DesktopManager.RECT
            [DesktopManager.MonitorNativeMethods]::GetWindowRect($control.Handle, [ref]$rect) | Out-Null
            $bmp.Width | Should -Be ($rect.Right - $rect.Left)
            $bmp.Height | Should -Be ($rect.Bottom - $rect.Top)
            $bmp.Dispose()
        } finally {
            if ($null -ne $proc) {
                try {
                    $null = $proc.CloseMainWindow()
                } catch {
                    # Ignore close errors
                }
                $proc | Wait-Process -Timeout 3 -ErrorAction SilentlyContinue
                if (-not $proc.HasExited) {
                    $proc | Stop-Process -Force -ErrorAction SilentlyContinue
                }
            }
        }
    }
}
