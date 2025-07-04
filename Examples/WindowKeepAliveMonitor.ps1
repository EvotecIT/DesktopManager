Import-Module ./DesktopManager.psd1 -Force

# Keep RDP windows alive every 30 seconds and monitor activity
Start-DesktopWindowKeepAlive -Name '*RDP*' -Interval 00:00:30

for ($i = 0; $i -lt 3; $i++) {
    Start-Sleep -Seconds 10
    Get-DesktopWindowKeepAlive | ForEach-Object { "Active: $($_.Title)" }
}

# Stop RDP keep-alive sessions
Stop-DesktopWindowKeepAlive -Name '*RDP*'
