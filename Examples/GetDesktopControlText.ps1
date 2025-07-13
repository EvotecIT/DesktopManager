Import-Module ./DesktopManager.psd1 -Force

# Retrieve text from the Notepad edit box
$control = Get-DesktopWindowControl -Name '*Notepad*' | Where-Object ClassName -eq 'Edit' | Select-Object -First 1
Get-DesktopControlText -Control $control
