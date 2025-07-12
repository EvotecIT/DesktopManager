Import-Module ./DesktopManager.psd1 -Force

# Launch Notepad and wait until it starts
$proc = Start-Process -FilePath notepad -PassThru
Start-Sleep -Seconds 1

# Get the Notepad window and bring it to the foreground
$window = Get-DesktopWindow -Name '*Notepad*'

# Type "HELLO WORLD" into Notepad
Invoke-DesktopKeyPress -Window $window -Keys @(
    [DesktopManager.VirtualKey]::VK_H,
    [DesktopManager.VirtualKey]::VK_E,
    [DesktopManager.VirtualKey]::VK_L,
    [DesktopManager.VirtualKey]::VK_L,
    [DesktopManager.VirtualKey]::VK_O,
    [DesktopManager.VirtualKey]::VK_SPACE,
    [DesktopManager.VirtualKey]::VK_W,
    [DesktopManager.VirtualKey]::VK_O,
    [DesktopManager.VirtualKey]::VK_R,
    [DesktopManager.VirtualKey]::VK_L,
    [DesktopManager.VirtualKey]::VK_D
)

# Press Ctrl+S to open the Save dialog
Invoke-DesktopKeyPress -Window $window -Keys @(
    [DesktopManager.VirtualKey]::VK_CONTROL,
    [DesktopManager.VirtualKey]::VK_S
)

