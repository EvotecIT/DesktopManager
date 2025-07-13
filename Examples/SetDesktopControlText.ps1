Import-Module ./DesktopManager.psd1 -Force

# Set text in the first Notepad edit control
$control = Get-DesktopWindowControl -Name '*Notepad*' | Where-Object { $_.ClassName -eq 'Edit' } | Select-Object -First 1
if ($control) {
    Set-DesktopControlText -Control $control -Text 'Hello from DesktopManager'
}
