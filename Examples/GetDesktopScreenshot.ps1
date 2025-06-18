Import-Module .\DesktopManager.psd1 -Force

Get-DesktopScreenshot -Path "$PSScriptRoot\Output\DesktopScreenshot1.png" -Monitor 0
Get-DesktopScreenshot -Path "$PSScriptRoot\Output\DesktopScreenshot2.png" -Monitor 1
Get-DesktopScreenshot -Path "$PSScriptRoot\Output\DesktopScreenshot3.png" -Monitor 2
Get-DesktopScreenshot -Path "$PSScriptRoot\Output\DesktopScreenshot4.png" -Monitor 3
