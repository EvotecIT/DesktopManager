---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Get-DesktopDeviceContainer
## SYNOPSIS
Gets Windows device containers assembled from Plug and Play device instances.

## SYNTAX
### __AllParameterSets
```powershell
Get-DesktopDeviceContainer [-Present] [-Problem] [<CommonParameters>]
```

## DESCRIPTION
Gets Windows device containers assembled from Plug and Play device instances.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-DesktopDeviceContainer -Present
```


## PARAMETERS

### -Present
Returns containers with at least one present device.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Problem
Returns containers containing a device problem.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `DesktopManager.DesktopDeviceContainerInfo`

## RELATED LINKS

- None
