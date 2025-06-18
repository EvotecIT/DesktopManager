Import-Module .\DesktopManager.psd1 -Force

# Monitor display changes for 30 seconds and automatically unregister
Register-DesktopMonitorEvent -Duration 30 -Action { Write-Host "Display settings changed" }

