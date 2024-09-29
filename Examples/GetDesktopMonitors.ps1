Import-Module .\DesktopManager.psd1 -Force

$Desktop1 = Get-DesktopMonitor
$Desktop1 | Format-Table

$Desktop2 = Get-DesktopMonitor -ConnectedOnly
$Desktop2 | Format-Table

$Desktop3 = Get-DesktopMonitor -PrimaryOnly
$Desktop3 | Format-Table

$Desktop4 = Get-DesktopMonitor -Index 0
$Desktop4 | Format-Table *

$Desktop5 = Get-DesktopMonitor -Index 1
$Desktop5 | Format-Table *

$Desktop6 = Get-DesktopMonitor -DeviceName "\\.\DISPLAY2"
$Desktop6 | Format-Table *