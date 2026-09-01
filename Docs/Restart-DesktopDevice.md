---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Restart-DesktopDevice
## SYNOPSIS
Restarts an exact Plug and Play device without rebooting Windows.

## SYNTAX
### __AllParameterSets
```powershell
Restart-DesktopDevice [-InstanceId] <string> [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Restarts an exact Plug and Play device without rebooting Windows.

## EXAMPLES

### EXAMPLE 1
```powershell
Restart-DesktopDevice -InstanceId 'Value'
```


## PARAMETERS

### -InstanceId
The exact device instance identifier.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.String`

## OUTPUTS

- `DesktopManager.DesktopDeviceOperationResult`

## RELATED LINKS

- None
