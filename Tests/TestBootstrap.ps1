if (-not (Test-Path -LiteralPath (Join-Path $PSScriptRoot '..\Lib'))) {
    $env:DESKTOPMANAGER_DEVELOPMENT = 'true'
}

Import-Module (Join-Path $PSScriptRoot '..\DesktopManager.psd1') -Force -ErrorAction Stop
