Import-Module .\DesktopManager.psd1 -Force

# Display brightness for all connected monitors
Get-DesktopMonitor -ConnectedOnly | ForEach-Object {
    $level = Get-DesktopBrightness -DeviceId $_.DeviceId
    "${($_.DeviceName)} brightness: $level"
}

# Change brightness on the first monitor (preview with WhatIf)
Set-DesktopBrightness -Index 0 -Brightness 50 -WhatIf

