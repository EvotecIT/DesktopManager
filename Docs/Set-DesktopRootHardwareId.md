---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Set-DesktopRootHardwareId
## SYNOPSIS
Replaces the hardware identifier list of an exact ROOT-enumerated device.

## SYNTAX
### __AllParameterSets
```powershell
Set-DesktopRootHardwareId [-InstanceId] <string> [-HardwareId] <string[]> [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Replaces the hardware identifier list of an exact ROOT-enumerated device.

## EXAMPLES

### EXAMPLE 1
```powershell
Set-DesktopRootHardwareId -HardwareId @('Value')
```


## PARAMETERS

### -HardwareId
One or more exact hardware identifiers in replacement order.

```yaml
Type: String[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -InstanceId
The exact ROOT device instance identifier.

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

## NOTES

### Expert operation

Changing identifiers can change driver matching after the next scan.
