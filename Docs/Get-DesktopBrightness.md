---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Get-DesktopBrightness
## SYNOPSIS
Gets the brightness for one or more desktop monitors.

## SYNTAX
### __AllParameterSets
```powershell
Get-DesktopBrightness [[-Index] <int>] [[-DeviceId] <string>] [[-DeviceName] <string>] [-ConnectedOnly] [-PrimaryOnly] [<CommonParameters>]
```

## DESCRIPTION
Gets the brightness for one or more desktop monitors.

Retrieves the current brightness level for one or more monitors. You can specify the monitor by index, device ID, or device name, or limit the query to connected or primary monitors.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-DesktopBrightness -ConnectedOnly
```


## PARAMETERS

### -ConnectedOnly
Get brightness for connected monitors only.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 3
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -DeviceId
The device ID of the monitor.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
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
Parameter Sets: __AllParameterSets
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
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -PrimaryOnly
Get brightness for the primary monitor only.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
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
