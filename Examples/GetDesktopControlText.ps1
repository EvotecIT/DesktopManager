Import-Module ./DesktopManager.psd1 -Force

# Retrieve the text from a Notepad edit control
$control = Get-DesktopWindowControl -Name '*Notepad*' | Where-Object ClassName -eq 'Edit'
Get-DesktopControlText -Control $control

