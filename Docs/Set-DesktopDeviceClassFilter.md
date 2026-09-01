---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Set-DesktopDeviceClassFilter
## SYNOPSIS
Replaces the upper or lower filter-service chain for an exact device setup class.

## SYNTAX
### __AllParameterSets
```powershell
Set-DesktopDeviceClassFilter [-ClassGuid] <guid> [-Kind] <DesktopDeviceClassFilterKind> [-Service] <string[]> [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Every named filter service must already exist. An empty Service list removes the selected filter property.

## EXAMPLES

### EXAMPLE 1
```powershell
Set-DesktopDeviceClassFilter -ClassGuid 'Value'
```


## PARAMETERS

### -ClassGuid
The exact device setup class identifier.

```yaml
Type: Guid
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Kind
Selects the Upper or Lower filter chain.

```yaml
Type: DesktopDeviceClassFilterKind
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Upper, Lower

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Service
The replacement filter-service names in load order. Supply an empty array to remove the property.

```yaml
Type: String[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Guid`

## OUTPUTS

- `DesktopManager.DesktopDeviceOperationResult`

## RELATED LINKS

- None

## NOTES

### Expert operation

An invalid filter chain can prevent every device in the class from starting.
