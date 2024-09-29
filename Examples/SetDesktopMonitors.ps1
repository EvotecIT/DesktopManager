Import-Module .\DesktopManager.psd1 -Force

$Desktop2 = Get-DesktopMonitor -ConnectedOnly
$Desktop2 | Format-Table

Set-DesktopPosition -Index 0 -Left -3840 -Top 0 -Right 0 -Bottom 1660 -WhatIf
Set-DesktopPosition -Index 1 -Left 0 -Top 0 -Right 3840 -Bottom 2160 -WhatIf