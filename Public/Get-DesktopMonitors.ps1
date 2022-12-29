function Get-DesktopMonitors {
    [CmdletBinding()]
    param()
    $Monitors = [DesktopManager.Monitors]::new()
    $Monitors.GetMonitors()
}