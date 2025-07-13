Import-Module ./DesktopManager.psd1 -Force

# Make Notepad a topmost window
Set-DesktopWindowStyle -Name '*Notepad*' -ExStyle TopMost

