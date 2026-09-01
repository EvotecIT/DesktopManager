---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Set-DesktopTaskbarAutoHide
## SYNOPSIS
Sets the global Windows taskbar auto-hide state.

## SYNTAX
### __AllParameterSets
```powershell
Set-DesktopTaskbarAutoHide [-Enabled] <bool> [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Sets the global Windows taskbar auto-hide state.

## EXAMPLES

### EXAMPLE 1
```powershell
Set-DesktopTaskbarAutoHide -Enabled $true
```


## PARAMETERS

### -Enabled
The explicit auto-hide state.

```yaml
Type: Boolean
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
