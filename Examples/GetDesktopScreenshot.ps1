Import-Module .\DesktopManager.psd1 -Force

Invoke-DesktopScreenshot -Path "$PSScriptRoot\Output\DesktopScreenshot0.png"
Invoke-DesktopScreenshot -Path "$PSScriptRoot\Output\DesktopScreenshot1.png" -Index 0
Invoke-DesktopScreenshot -Path "$PSScriptRoot\Output\DesktopScreenshot2.png" -Index 1
Invoke-DesktopScreenshot -Path "$PSScriptRoot\Output\DesktopScreenshot3.png" -Index 2
Invoke-DesktopScreenshot -Path "$PSScriptRoot\Output\DesktopScreenshot4.png" -Index 3
Invoke-DesktopScreenshot -Path "$PSScriptRoot\Output\DesktopScreenshotRegion.png" -Left 100 -Top 100 -Width 800 -Height 600
