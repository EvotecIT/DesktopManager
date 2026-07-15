Import-Module .\DesktopManager.psd1 -Force

Get-DesktopMonitors | Format-Table

Get-DesktopWallpaper -Index 0

Set-DesktopWallpaper -Index 1 -WallpaperPath "C:\Wallpapers\Landscape.jpg" -Position Fit -WhatIf
Set-DesktopWallpaper -Index 0 -WallpaperPath "C:\Wallpapers\Portrait.jpg" -WhatIf
