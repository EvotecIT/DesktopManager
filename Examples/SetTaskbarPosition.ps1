Import-Module ./DesktopManager.psd1 -Force

# Move the taskbar on the primary monitor to the top
Set-TaskbarPosition -PrimaryOnly -Position Top

# Hide the taskbar on the second monitor
Set-TaskbarPosition -Index 1 -Hide
