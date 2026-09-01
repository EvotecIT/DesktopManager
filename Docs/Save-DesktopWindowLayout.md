---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Save-DesktopWindowLayout
## SYNOPSIS
Saves the current desktop window layout to a file.

## SYNTAX
### __AllParameterSets
```powershell
Save-DesktopWindowLayout [-Path] <string> [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Saves the current desktop window layout.

## EXAMPLES

### EXAMPLE 1
```powershell
Save-DesktopWindowLayout -Path 'C:\Path'
```


## PARAMETERS

### -Path
Path where the layout should be stored.

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

- `None`

## RELATED LINKS

- None
