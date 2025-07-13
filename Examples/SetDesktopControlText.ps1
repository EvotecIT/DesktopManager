Import-Module ./DesktopManager.psd1 -Force

# Set the text of a Notepad edit control
$control = Get-DesktopWindowControl -Name '*Notepad*' | Where-Object ClassName -eq 'Edit'
Set-DesktopControlText -Control $control -Text 'Hello world'

