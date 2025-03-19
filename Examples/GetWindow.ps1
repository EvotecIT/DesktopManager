Import-Module .\DesktopManager.psd1 -Force

Get-DesktopWindow | Format-Table *

Set-DesktopWindow -Name '*Zadanie - Notepad' -Height 800 -Width 1200 -Left 100