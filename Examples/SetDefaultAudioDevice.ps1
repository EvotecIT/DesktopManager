Import-Module .\DesktopManager.psd1 -Force

# Change the default playback device (preview with WhatIf)
Set-DefaultAudioDevice -DeviceId 'DEVICE_ID' -WhatIf
