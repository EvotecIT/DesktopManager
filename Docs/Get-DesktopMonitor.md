---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Get-DesktopMonitor
## SYNOPSIS
Gets the desktop monitors information.

## SYNTAX
### __AllParameterSets
```powershell
Get-DesktopMonitor [[-Index] <int>] [[-DeviceId] <string>] [[-DeviceName] <string>] [-ConnectedOnly] [-PrimaryOnly] [<CommonParameters>]
```

## DESCRIPTION
Gets the desktop monitors information.

Retrieves information about the desktop monitors connected to the system. You can filter the monitors by index, device ID, device name, connection status, or primary monitor status.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-DesktopMonitor
```

Get information for all monitors

Retrieves information for all connected desktop monitors.

### EXAMPLE 2
```powershell
Get-DesktopMonitor -Index 1
```

Get information for a specific monitor by index

Retrieves information for the monitor specified by the index.

### EXAMPLE 3
```powershell
Get-DesktopMonitor -ConnectedOnly
```

Get information for connected monitors only

Retrieves information for all connected monitors only.

### EXAMPLE 4
```powershell
Get-DesktopMonitor -PrimaryOnly
```

Get information for the primary monitor only

Retrieves information for the primary monitor only.

## PARAMETERS

### -ConnectedOnly
Get information for connected monitors only.

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
The device ID of the monitor to get information for.

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
The device name of the monitor to get information for.

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
The index of the monitor to get information for.

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
Get information for the primary monitor only.

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
