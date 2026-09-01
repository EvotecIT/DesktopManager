---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Get-DesktopWallpaper
## SYNOPSIS
Gets the current desktop wallpaper for one or more monitors.

## SYNTAX
### __AllParameterSets
```powershell
Get-DesktopWallpaper [[-Index] <int>] [[-DeviceId] <string>] [[-DeviceName] <string>] [-ConnectedOnly] [-PrimaryOnly] [<CommonParameters>]
```

## DESCRIPTION
Gets the current desktop wallpaper for one or more monitors.

Retrieves the current desktop wallpaper for one or more monitors. You can specify the monitor by index, device ID, or device name. You can also get the wallpaper for all monitors or only the primary monitor.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-DesktopWallpaper
```

Get the wallpaper for all monitors

### EXAMPLE 2
```powershell
Get-DesktopWallpaper -Index 1
```

Get the wallpaper for a specific monitor by index

### EXAMPLE 3
```powershell
Get-DesktopWallpaper -PrimaryOnly
```

Get the wallpaper for the primary monitor only

## PARAMETERS

### -ConnectedOnly
Get the wallpaper for connected monitors only.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 3
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DeviceId
The device ID of the monitor to get the wallpaper for.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DeviceName
The device name of the monitor to get the wallpaper for.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Index
The index of the monitor to get the wallpaper for.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PrimaryOnly
Get the wallpaper for the primary monitor only.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 4
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `None`

## RELATED LINKS

- None
