Import-Module .\DesktopManager.psd1 -Force

# Get brightness for the first monitor
Get-DesktopBrightness -Index 0

# Set brightness for the first monitor
Set-DesktopBrightness -Index 0 -Brightness 50 -WhatIf
