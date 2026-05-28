---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Set-DesktopDpiScaling
## SYNOPSIS
Sets DPI scaling for one or more desktop monitors.

## SYNTAX
### Index (Default)
```powershell
Set-DesktopDpiScaling [[-Index] <int>] [-Scaling] <int> [-WhatIf] [-Confirm] [<CommonParameters>]
```

### DeviceID
```powershell
Set-DesktopDpiScaling [[-DeviceId] <string>] [-Scaling] <int> [-WhatIf] [-Confirm] [<CommonParameters>]
```

### DeviceName
```powershell
Set-DesktopDpiScaling [[-DeviceName] <string>] [-Scaling] <int> [-WhatIf] [-Confirm] [<CommonParameters>]
```

### PrimaryOnly
```powershell
Set-DesktopDpiScaling [-PrimaryOnly] [-Scaling] <int> [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Sets DPI scaling for one or more desktop monitors.

Changes the DPI scaling level for monitors. You can target monitors by index, device ID or name, or limit the action to the primary monitor.

## EXAMPLES

### EXAMPLE 1
```powershell
Set-DesktopDpiScaling -DeviceId 'Value'
```


## PARAMETERS

### -DeviceId
The device ID of the monitor.

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

### -PrimaryOnly
Set scaling for the primary monitor only.

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

### -Scaling
Scaling percentage to set.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `System.Object`

## RELATED LINKS

- None
