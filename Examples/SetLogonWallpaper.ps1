Import-Module ..\DesktopManager.psd1 -Force

$wallpaper = Get-ChildItem -Path . -Filter *.jpg | Select-Object -First 1
if ($null -ne $wallpaper) {
    Set-LogonWallpaper -ImagePath $wallpaper.FullName -WhatIf
}
