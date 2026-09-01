---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Set-DesktopPosition
## SYNOPSIS
Sets the position of the desktop for one or more monitors.

## SYNTAX
### Index (Default)
```powershell
Set-DesktopPosition [[-Index] <Int32>] [-Left] <int> [-Top] <int> [-WhatIf] [-Confirm] [<CommonParameters>]
```

### DeviceID
```powershell
Set-DesktopPosition [[-DeviceId] <string>] [-Left] <int> [-Top] <int> [-WhatIf] [-Confirm] [<CommonParameters>]
```

### DeviceName
```powershell
Set-DesktopPosition [[-DeviceName] <string>] [-Left] <int> [-Top] <int> [-WhatIf] [-Confirm] [<CommonParameters>]
```

### PrimaryOnly
```powershell
Set-DesktopPosition [-PrimaryOnly] [-Left] <int> [-Top] <int> [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Sets the position of the desktop for one or more monitors.

Sets the position of the desktop for one or more monitors. You can specify the monitor by index, device ID, or device name. You can also set the position for all monitors or only the primary monitor.

## EXAMPLES

### EXAMPLE 1
```powershell
Set the position for a specific monitor by index

            Set-DesktopPosition -Index 1 -Left 0 -Top 0
```


### EXAMPLE 2
```powershell
Set the position for the primary monitor only

            Set-DesktopPosition -PrimaryOnly -Left 0 -Top 0
```


## PARAMETERS

### -DeviceId
The device ID of the monitor to set the position for.

```yaml
Type: String
Parameter Sets: DeviceID
Aliases: MonitorID
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DeviceName
The device name of the monitor to set the position for.

```yaml
Type: String
Parameter Sets: DeviceName
Aliases: None
Possible values:

Required: False
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Index
The index of the monitor to set the position for.

```yaml
Type: Int32
Parameter Sets: Index
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Left
The left position of the monitor.

```yaml
Type: Int32
Parameter Sets: Index, DeviceID, DeviceName, PrimaryOnly
Aliases: None
Possible values:

Required: True
Position: 4
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PrimaryOnly
Set the position for the primary monitor only.

```yaml
Type: SwitchParameter
Parameter Sets: PrimaryOnly
Aliases: None
Possible values:

Required: False
Position: 3
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Top
The top position of the monitor.

```yaml
Type: Int32
Parameter Sets: Index, DeviceID, DeviceName, PrimaryOnly
Aliases: None
Possible values:

Required: True
Position: 5
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
