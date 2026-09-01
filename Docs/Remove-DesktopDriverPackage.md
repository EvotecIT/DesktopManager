---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Remove-DesktopDriverPackage
## SYNOPSIS
Removes an exact published third-party package from the Driver Store.

## SYNTAX
### __AllParameterSets
```powershell
Remove-DesktopDriverPackage [-PublishedInfName] <string> [-UninstallDevices] [-Force] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Removes an exact published third-party package from the Driver Store.

## EXAMPLES

### EXAMPLE 1
```powershell
Remove-DesktopDriverPackage -Force
```


## PARAMETERS

### -Force
Forces direct Driver Store deletion when UninstallDevices is not set. With UninstallDevices, native package uninstall reassigns affected devices before removing the package, so Force is accepted but redundant.

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

### -PublishedInfName
The exact published INF name, such as oem42.inf.

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

### -UninstallDevices
Uninstalls the package from devices before deleting it.

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

- `System.String`

## OUTPUTS

- `DesktopManager.DesktopDeviceOperationResult`

## RELATED LINKS

- None
