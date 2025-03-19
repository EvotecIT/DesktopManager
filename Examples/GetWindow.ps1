Import-Module .\DesktopManager.psd1 -Force

Get-DesktopWindow | Format-Table

#Set-DesktopWindowPosition -Name 'PSWriteHTML - CHANGELOG.MD' -Left -1500 -Top 0