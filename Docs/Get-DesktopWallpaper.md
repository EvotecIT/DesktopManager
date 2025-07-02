---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version:
schema: 2.0.0
---

# Get-DesktopWallpaper

## SYNOPSIS
Gets the current desktop wallpaper path for one or more monitors.

## SYNTAX

```
Get-DesktopWallpaper [-Index <Int32>] [-DeviceId <String>] [-DeviceName <String>] [-ConnectedOnly] [-PrimaryOnly] [<CommonParameters>]
```

## DESCRIPTION
Returns the wallpaper file path for the specified monitor. When no monitor is specified the wallpaper for every monitor is returned. You can target a monitor by index, device ID or device name and optionally limit the query to connected or primary monitors.

## EXAMPLES

### Example 1
```powershell
PS C:\> Get-DesktopWallpaper -Index 0
```
Retrieves the wallpaper path for the first monitor.

## PARAMETERS

### -Index
Index of the monitor returned by `Get-DesktopMonitor`.

```yaml
Type: Int32
Parameter Sets: Index
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MonitorID
Device identifier of the monitor to query.

```yaml
Type: String
Parameter Sets: MonitorID
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None

## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS
