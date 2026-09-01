---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# New-DesktopRootDevice
## SYNOPSIS
Creates a ROOT-enumerated device and installs an INF package for it.

## SYNTAX
### __AllParameterSets
```powershell
New-DesktopRootDevice [-InfPath] <string> [-HardwareId] <string> [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Creates a ROOT-enumerated device and installs an INF package for it.

## EXAMPLES

### EXAMPLE 1
```powershell
New-DesktopRootDevice -InfPath 'C:\Path'
```


## PARAMETERS

### -HardwareId
The exact ROOT hardware identifier.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -InfPath
The path to the package INF file.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `DesktopManager.DesktopDeviceOperationResult`

## RELATED LINKS

- None

## NOTES

### Expert operation

Use only with an INF designed for the supplied ROOT hardware identifier.
