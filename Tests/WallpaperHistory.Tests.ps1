describe 'Wallpaper history cmdlets' {
    it 'exports Get-DesktopWallpaperHistory' -Skip:(-not $IsWindows) {
        Get-Command Get-DesktopWallpaperHistory | Should -Not -BeNullOrEmpty
    }
    it 'exports Set-DesktopWallpaperHistory' -Skip:(-not $IsWindows) {
        Get-Command Set-DesktopWallpaperHistory | Should -Not -BeNullOrEmpty
    }
    it 'supports clearing history' -Skip:(-not $IsWindows) {
        { Set-DesktopWallpaperHistory -Clear -WhatIf } | Should -Not -Throw
    }
}
