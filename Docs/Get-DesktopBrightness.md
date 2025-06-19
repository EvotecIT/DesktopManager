---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version:
schema: 2.0.0
---

# Get-DesktopBrightness

## SYNOPSIS
Gets the brightness level for one or more desktop monitors.

## SYNTAX
```
Get-DesktopBrightness [-Index <Int32>] [-DeviceId <String>] [-DeviceName <String>] [-ConnectedOnly] [-PrimaryOnly] [<CommonParameters>]
```

## DESCRIPTION
Retrieves the current brightness level for the specified monitor. When no monitor is specified, all monitors are queried.

## EXAMPLES
### Example 1
```powershell
PS C:\> Get-DesktopBrightness -Index 0
```
Gets the brightness of the first monitor.

## PARAMETERS
### -Index
Index of the monitor returned by Get-DesktopMonitor.

### -DeviceId
Device ID of the monitor.

### -DeviceName
Device name of the monitor.

### -ConnectedOnly
Limit the query to monitors currently connected.

### -PrimaryOnly
Limit the query to the primary monitor.

### CommonParameters
This cmdlet supports the common parameters. For more information, see about_CommonParameters.

## INPUTS
### None

## OUTPUTS
### System.Int32
The current brightness level.

## NOTES

## RELATED LINKS
- Set-DesktopBrightness
- Get-DesktopMonitor


