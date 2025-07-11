Import-Module .\DesktopManager.psd1 -Force

Get-DesktopWindow | Format-Table *

# Filter windows by process name
Get-DesktopWindow -ProcessName 'notepad'

# Filter by class name
Get-DesktopWindow -ClassName 'Notepad'

# Filter using regex
Get-DesktopWindow -Regex '.*Notepad.*'

Set-DesktopWindow -Name '*Notepad' -Height 800 -Width 1200 -Left 100 -Activate

Set-DesktopWindow -Name '*Notepad' -TopMost
