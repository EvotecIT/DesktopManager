Import-Module ./DesktopManager.psd1 -Force

# Wait for a Notepad window to appear for up to 10 seconds
Wait-DesktopWindow -Name '*Notepad*' -TimeoutMs 10000

