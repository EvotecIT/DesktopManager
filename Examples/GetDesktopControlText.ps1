Import-Module ./DesktopManager.psd1 -Force

# Get text from the first Notepad edit control
$control = Get-DesktopWindowControl -Name '*Notepad*' | Where-Object { $_.ClassName -eq 'Edit' } | Select-Object -First 1
if ($control) {
    Get-DesktopControlText -Control $control
}
