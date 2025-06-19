---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version:
schema: 2.0.0
---

# Set-DesktopBrightness

## SYNOPSIS
Sets the brightness level for one or more desktop monitors.

## SYNTAX
```
Set-DesktopBrightness [-Index <Int32>] [-DeviceId <String>] [-DeviceName <String>] [-PrimaryOnly] -Brightness <Int32> [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Changes the brightness level for the specified monitor. Use -WhatIf to preview the change.

## EXAMPLES
### Example 1
```powershell
PS C:\> Set-DesktopBrightness -Index 0 -Brightness 50
```
Sets the brightness of the first monitor to 50 percent.

## PARAMETERS
### -Index
Index of the monitor returned by Get-DesktopMonitor.

### -DeviceId
Device ID of the monitor.

### -DeviceName
Device name of the monitor.

### -PrimaryOnly
Target only the primary monitor.

### -Brightness
New brightness level from 0 to 100.

### CommonParameters
This cmdlet supports the common parameters. For more information, see about_CommonParameters.

## INPUTS
### None

## OUTPUTS
### None

## NOTES

## RELATED LINKS
- Get-DesktopBrightness
- Get-DesktopMonitor

