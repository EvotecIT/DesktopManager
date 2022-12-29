function Set-DesktopWallpaper {
    [CmdletBinding(DefaultParameterSetName = 'Index')]
    param(
        [Parameter(ParameterSetName = 'Index')][int] $Index,
        [Parameter(ParameterSetName = 'MonitorID')][string] $MonitorID,
        [Parameter(ParameterSetName = 'All')][switch] $All,

        [Parameter(ParameterSetName = 'MonitorID')]
        [Parameter(ParameterSetName = 'Index')]
        [Parameter(ParameterSetName = 'All')]
        [alias('FilePath')][string] $WallpaperPath,

        [Parameter(ParameterSetName = 'MonitorID')]
        [Parameter(ParameterSetName = 'Index')]
        [Parameter(ParameterSetName = 'All')]
        [DesktopManager.DesktopWallpaperPosition] $Position
    )
    $Monitors = [DesktopManager.Monitors]::new()
    if ($PSBoundParameters.ContainsKey('Index')) {
        $Monitors.SetWallpaper($Index, $WallpaperPath)
    } elseif ($PSBoundParameters.ContainsKey('All')) {
        $Monitors.SetWallpaper($WallpaperPath)
    } else {
        $Monitors.SetWallpaper($MonitorID, $WallpaperPath)
    }
    if ($PSBoundParameters.ContainsKey('Position')) {
        $Monitors.SetWallpaperPosition($Position)
    }
}