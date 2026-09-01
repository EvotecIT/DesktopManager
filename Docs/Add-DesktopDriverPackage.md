---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Add-DesktopDriverPackage
## SYNOPSIS
Stages an INF package in the Driver Store and optionally installs it on matching devices.

## SYNTAX
### __AllParameterSets
```powershell
Add-DesktopDriverPackage [-InfPath] <string> [-Install] [-Force] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Stages an INF package in the Driver Store and optionally installs it on matching devices.

## EXAMPLES

### EXAMPLE 1
```powershell
PS> Add-DesktopDriverPackage -InfPath C:\Drivers\device.inf -Confirm
```

Adds the package to the Driver Store without selecting it for devices.

## PARAMETERS

### -Force
Forces the INF when Install is selected, including a lower-ranked driver.

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
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Install
Installs the package on matching present devices after staging.

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
