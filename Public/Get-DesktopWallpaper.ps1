function Get-DesktopWallpaper {
    [CmdletBinding(DefaultParameterSetName = 'Index')]
    param(
        [parameter(Mandatory, ParameterSetName = 'MonitorID')][string] $MonitorID,
        [parameter(Mandatory, ParameterSetName = 'Index')][int] $Index
    )
    $Monitors = [DesktopManager.Monitors]::new()
    if ($PSBoundParameters.ContainsKey('Index')) {
        $Monitors.GetWallpaper($Index)
    } else {
        $Monitors.GetWallpaper($MonitorID)
    }
}