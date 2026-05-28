---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Invoke-DesktopMouseMove
## SYNOPSIS
Moves the mouse cursor.

## SYNTAX
### __AllParameterSets
```powershell
Invoke-DesktopMouseMove [-X] <int> [-Y] <int> [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Moves the mouse cursor to specific coordinates.

## EXAMPLES

### EXAMPLE 1
```powershell
Invoke-DesktopMouseMove -X 100 -Y 100
```


## PARAMETERS

### -X
X coordinate in pixels.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Y
Y coordinate in pixels.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `System.Object`

## RELATED LINKS

- None
