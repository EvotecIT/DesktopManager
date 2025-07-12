Import-Module ./DesktopManager.psd1 -Force

# Make the first Notepad window semi-transparent
Set-DesktopWindowTransparency -Name '*Notepad*' -Alpha 128
