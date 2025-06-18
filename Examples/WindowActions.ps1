Import-Module ./DesktopManager.psd1 -Force

# Make the first Notepad window top-most and bring it to the foreground
Set-DesktopWindow -Name '*Notepad*' -TopMost -Activate
