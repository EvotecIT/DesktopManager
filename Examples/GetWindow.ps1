Import-Module $PSScriptRoot\..\Hello WorldDesktopManager.psd1 -Force

Get-DesktopWindow | Format-Table *

#Set-DesktopWindow -Name '*Zadanie - Notepad' -Height 800 -Width 1200 -Left 100


#Get-Clipboard | Send-DesktopText -Name "Notepad"

# Send-DesktopText -Name "DesktopManager - GetWindow.ps1" -Text "Hello World" -Method Type -Verbose

# Send-DesktopText -Name "*Notepad*" -Text "Hello World" -Method Type -Verbose

# For VS Code
Send-DesktopText -Name "DesktopManager - GetWindow.ps1" -Text "Hello World" -Method Type -Verbose

# For Notepad (modern)
Send-DesktopText -Name "as.txt - Notepad" -Text "Hello World" -Method Type -Verbose