describe 'Wallpaper history cmdlets' {
    it 'exports Get-DesktopWallpaperHistory' {
        Get-Command Get-DesktopWallpaperHistory | Should -Not -BeNullOrEmpty
    }
    it 'exports Set-DesktopWallpaperHistory' {
        Get-Command Set-DesktopWallpaperHistory | Should -Not -BeNullOrEmpty
    }
    it 'supports clearing history' {
        { Set-DesktopWallpaperHistory -Clear -WhatIf } | Should -Not -Throw
    }
}
