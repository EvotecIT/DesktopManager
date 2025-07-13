Import-Module ./DesktopManager.psd1 -Force

$window = Get-DesktopWindow -Name '*Notepad*' | Select-Object -First 1
if ($window) {
    Invoke-DesktopWindowScreenshot -Window $window -Path "$PSScriptRoot/Output/Window.png"
}
$control = Get-DesktopWindowControl -Name '*Notepad*' | Where-Object ClassName -eq 'Edit' | Select-Object -First 1
if ($control) {
    Invoke-DesktopWindowScreenshot -Control $control -Path "$PSScriptRoot/Output/Control.png"
}
