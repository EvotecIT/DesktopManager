---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Set-TaskbarPosition
## SYNOPSIS
Moves or hides the taskbar for one or more monitors.

## SYNTAX
### Index (Default)
```powershell
Set-TaskbarPosition [[-Index] <int>] [-Position <TaskbarPosition>] [-Hide] [-Show] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### DeviceId
```powershell
Set-TaskbarPosition [[-DeviceId] <string>] [-Position <TaskbarPosition>] [-Hide] [-Show] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### DeviceName
```powershell
Set-TaskbarPosition [[-DeviceName] <string>] [-Position <TaskbarPosition>] [-Hide] [-Show] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### PrimaryOnly
```powershell
Set-TaskbarPosition [-PrimaryOnly] [-Position <TaskbarPosition>] [-Hide] [-Show] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### All
```powershell
Set-TaskbarPosition [-All] [-Position <TaskbarPosition>] [-Hide] [-Show] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Moves or hides the taskbar for one or more monitors.

Allows changing taskbar position or visibility on specific monitors.

## EXAMPLES

### EXAMPLE 1
```powershell
Set-TaskbarPosition -PrimaryOnly -Position Top
```


## PARAMETERS

### -All
Affects all monitors.

```yaml
Type: SwitchParameter
Parameter Sets: All
Aliases: None
Possible values:

Required: False
Position: 4
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -DeviceId
The device ID of the monitor.

```yaml
Type: String
Parameter Sets: DeviceId
Aliases: MonitorID
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -DeviceName
The device name of the monitor.

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

### -Hide
Hide the taskbar.

```yaml
Type: SwitchParameter
Parameter Sets: Index, DeviceId, DeviceName, PrimaryOnly, All
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Index
The index of the monitor.

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

### -Position
Desired taskbar position.

```yaml
Type: Nullable`1
Parameter Sets: Index, DeviceId, DeviceName, PrimaryOnly, All
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -PrimaryOnly
Affects the primary monitor only.

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

### -Show
Show the taskbar.

```yaml
Type: SwitchParameter
Parameter Sets: Index, DeviceId, DeviceName, PrimaryOnly, All
Aliases: None
Possible values:

Required: False
Position: named
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
