---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Restore-DesktopWindowLayout
## SYNOPSIS
Restores window positions from a saved layout.

## SYNTAX
### __AllParameterSets
```powershell
Restore-DesktopWindowLayout [-Path] <string> [-Validate] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Restores desktop window layout.

## EXAMPLES

### EXAMPLE 1
```powershell
Restore-DesktopWindowLayout -Path 'C:\Path'
```


## PARAMETERS

### -Path
Path to the layout file.

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

### -Validate
Validate layout before applying.

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

- `None`

## RELATED LINKS

- None
