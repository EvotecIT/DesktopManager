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
Set-DesktopPosition [[-Index] <int>] [-Left] <int> [-Top] <int> [-Right] <int> [-Bottom] <int> [-WhatIf] [-Confirm] [<CommonParameters>]
```

### DeviceID
```powershell
Set-DesktopPosition [[-DeviceId] <string>] [-Left] <int> [-Top] <int> [-Right] <int> [-Bottom] <int> [-WhatIf] [-Confirm] [<CommonParameters>]
```

### DeviceName
```powershell
Set-DesktopPosition [[-DeviceName] <string>] [-Left] <int> [-Top] <int> [-Right] <int> [-Bottom] <int> [-WhatIf] [-Confirm] [<CommonParameters>]
```

### PrimaryOnly
```powershell
Set-DesktopPosition [-PrimaryOnly] [-Left] <int> [-Top] <int> [-Right] <int> [-Bottom] <int> [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Sets the position of the desktop for one or more monitors.

Sets the position of the desktop for one or more monitors. You can specify the monitor by index, device ID, or device name. You can also set the position for all monitors or only the primary monitor.

## EXAMPLES

### EXAMPLE 1
```powershell
Set the position for a specific monitor by index

            Set-DesktopPosition -Index 1 -Left 0 -Top 0 -Right 1920 -Bottom 1080
```


### EXAMPLE 2
```powershell
Set the position for the primary monitor only

            Set-DesktopPosition -PrimaryOnly -Left 0 -Top 0 -Right 1920 -Bottom 1080
```


## PARAMETERS

### -Bottom
The bottom position of the monitor.

```yaml
Type: Int32
Parameter Sets: Index, DeviceID, DeviceName, PrimaryOnly
Aliases: None
Possible values:

Required: True
Position: 7
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

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
Accept wildcard characters: True
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
Accept wildcard characters: True
```

### -Index
The index of the monitor to set the position for.

```yaml
Type: Nullable`1
Parameter Sets: Index
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
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
Accept wildcard characters: True
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
Accept wildcard characters: True
```

### -Right
The right position of the monitor.

```yaml
Type: Int32
Parameter Sets: Index, DeviceID, DeviceName, PrimaryOnly
Aliases: None
Possible values:

Required: True
Position: 6
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
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
Accept wildcard characters: True
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `System.Object`

## RELATED LINKS

- None
