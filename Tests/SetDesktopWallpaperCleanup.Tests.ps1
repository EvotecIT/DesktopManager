describe 'Set-DesktopWallpaper cleanup' {
    BeforeAll {
        Import-Module "$PSScriptRoot/..\DesktopManager.psd1" -Force
    }

    it 'removes temporary file when using ImageData' -Skip:(-not $IsWindows) {
        $tempDir = Join-Path $env:TEMP ([System.IO.Path]::GetRandomFileName())
        New-Item -ItemType Directory -Path $tempDir | Out-Null
        $oldTemp = $env:TEMP
        $oldTmp  = $env:TMP
        $env:TEMP = $tempDir
        $env:TMP  = $tempDir
        try {
            $bytes  = 0..10
            $stream = New-Object System.IO.MemoryStream
            $stream.Write($bytes, 0, $bytes.Length)
            $stream.Position = 0
            try {
                Set-DesktopWallpaper -All -ImageData $stream
            } catch {
                # ignore failures from COM APIs
            }
            $count = (Get-ChildItem -Path $tempDir).Count
        } finally {
            $stream.Dispose()
            $env:TEMP = $oldTemp
            $env:TMP  = $oldTmp
            Remove-Item $tempDir -Recurse -Force
        }
        $count | Should -Be 0
    }
}
