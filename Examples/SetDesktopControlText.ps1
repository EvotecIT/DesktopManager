Import-Module ./DesktopManager.psd1 -Force

# Set text in the Notepad edit box
$control = Get-DesktopWindowControl -Name '*Notepad*' | Where-Object ClassName -eq 'Edit' | Select-Object -First 1
Set-DesktopControlText -Control $control -Text 'Hello from DesktopManager'
