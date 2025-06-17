Import-Module .\DesktopManager.psd1 -Force

Get-DesktopScreenshot -Path "$PSScriptRoot\DesktopScreenshot.png"
