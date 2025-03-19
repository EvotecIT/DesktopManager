Import-Module .\DesktopManager.psd1 -Force

Get-DesktopWindow | Format-Table *

Set-DesktopWindowPosition -Name '*Zadanie - Notepad' -State Maximize