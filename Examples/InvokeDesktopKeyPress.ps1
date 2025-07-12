Import-Module ./DesktopManager.psd1 -Force

# Press WIN+R to open Run dialog
Invoke-DesktopKeyPress -Keys @([DesktopManager.VirtualKey]::VK_LWIN, [DesktopManager.VirtualKey]::VK_R)

# Press and hold WIN key then release
Invoke-DesktopKeyPress -Keys @([DesktopManager.VirtualKey]::VK_LWIN) -KeyDown
Start-Sleep -Seconds 1
Invoke-DesktopKeyPress -Keys @([DesktopManager.VirtualKey]::VK_LWIN) -KeyUp

