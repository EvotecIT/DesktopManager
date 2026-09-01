---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Invoke-DesktopMouseClick
## SYNOPSIS
Simulates a mouse click.

## SYNTAX
### __AllParameterSets
```powershell
Invoke-DesktopMouseClick [-Button <MouseButton>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Simulates a mouse click.

## EXAMPLES

### EXAMPLE 1
```powershell
Invoke-DesktopMouseClick -Button Left
```


## PARAMETERS

### -Button
Button to click. Defaults to Left.

```yaml
Type: MouseButton
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Left, Right

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
