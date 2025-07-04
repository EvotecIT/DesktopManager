Import-Module ./DesktopManager.psd1 -Force

# Keep Notepad and Calculator windows alive
Start-DesktopWindowKeepAlive -Name '*Notepad*'
Start-DesktopWindowKeepAlive -Name '*Calculator*' -Interval 00:00:30

# Wait a bit and show active sessions
Start-Sleep -Seconds 5
Get-DesktopWindowKeepAlive | Format-Table Title, Handle

# Stop all keep-alive timers
Stop-DesktopWindowKeepAlive -All
