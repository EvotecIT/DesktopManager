Import-Module ./DesktopManager.psd1 -Force

# Keep Notepad alive for one minute
Start-DesktopWindowKeepAlive -Name '*Notepad*' -Interval 00:01:00

# List active keep-alive sessions
Get-DesktopWindowKeepAlive | ForEach-Object { "Keeping window $($_.Title) alive" }

# Stop the keep-alive later
# Stop-DesktopWindowKeepAlive -Name '*Notepad*'
