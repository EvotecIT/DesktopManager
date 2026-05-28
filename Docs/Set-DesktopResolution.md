---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Set-DesktopResolution
## SYNOPSIS
Sets the resolution of a desktop monitor.

## SYNTAX
### Index (Default)
```powershell
Set-DesktopResolution [[-Index] <int>] [-Width] <int> [-Height] <int> [[-Orientation] <DisplayOrientation>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### DeviceID
```powershell
Set-DesktopResolution [[-DeviceId] <string>] [-Width] <int> [-Height] <int> [[-Orientation] <DisplayOrientation>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### DeviceName
```powershell
Set-DesktopResolution [[-DeviceName] <string>] [-Width] <int> [-Height] <int> [[-Orientation] <DisplayOrientation>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### PrimaryOnly
```powershell
Set-DesktopResolution [-PrimaryOnly] [-Width] <int> [-Height] <int> [[-Orientation] <DisplayOrientation>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Sets the resolution of a desktop monitor.

Allows changing the resolution and orientation of a monitor identified by index or device ID.

## EXAMPLES

### EXAMPLE 1
```powershell
Set-DesktopResolution -DeviceId 'Value'
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

### -Height
Resolution height.

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

### -Orientation
Optional display orientation.

```yaml
Type: Nullable`1
Parameter Sets: Index, DeviceID, DeviceName, PrimaryOnly
Aliases: None
Possible values:

Required: False
Position: 6
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -PrimaryOnly
Set resolution for the primary monitor only.

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

### -Width
Resolution width.

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
