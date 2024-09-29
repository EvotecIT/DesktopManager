Import-Module .\DesktopManager.psd1 -Force

$Desktop2 = Get-DesktopMonitor -ConnectedOnly
$Desktop2 | Format-Table