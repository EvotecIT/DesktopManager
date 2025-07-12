Import-Module ./DesktopManager.psd1 -Force

# Press WIN+R to open Run dialog
Invoke-DesktopKeyPress -Keys @([DesktopManager.VirtualKey]::VK_LWIN, [DesktopManager.VirtualKey]::VK_R)

