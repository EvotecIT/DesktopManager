Import-Module ./DesktopManager.psd1 -Force

# Save current window layout
Save-DesktopWindowLayout -Path './layout.json'

# ... move windows around ...

# Restore saved layout
Restore-DesktopWindowLayout -Path './layout.json' -Validate
