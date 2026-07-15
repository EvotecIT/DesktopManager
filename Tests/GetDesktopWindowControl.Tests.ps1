$RunInteractive = $false
if ($env:RUN_UI_TESTS -eq 'true' -or $env:DESKTOPMANAGER_RUN_UI_TESTS -eq 'true') {
    $RunInteractive = $true
}

BeforeAll {
    . "$PSScriptRoot/TestBootstrap.ps1"
}

describe 'Get-DesktopWindowControl' -Tag 'Interactive' {
    it 'enumerates Notepad controls' -Skip:(-not $IsWindows -or -not $RunInteractive) {
        $proc = Start-Process notepad -PassThru -WindowStyle Minimized
        try {
            $window = Wait-DesktopWindow -Name '*Notepad*' -TimeoutMs 10000
            $controls = Get-DesktopWindowControl -Name '*Notepad*'
            $controls | Should -Not -BeNullOrEmpty
            $editableClasses = @('Edit', 'RichEditD2DPT', 'NotepadTextBox')
            $controls | Where-Object { $editableClasses -contains $_.ClassName } | Should -Not -BeNullOrEmpty
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
