Import-Module ./DesktopManager.psd1 -Force

# Move cursor to 100,100
Invoke-DesktopMouseMove -X 100 -Y 100

# Left click
Invoke-DesktopMouseClick -Button Left

# Scroll down
Invoke-DesktopMouseScroll -Delta -120
