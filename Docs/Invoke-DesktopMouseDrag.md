---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Invoke-DesktopMouseDrag
## SYNOPSIS
Drags the mouse cursor.

## SYNTAX
### __AllParameterSets
```powershell
Invoke-DesktopMouseDrag [-StartX] <int> [-StartY] <int> [-EndX] <int> [-EndY] <int> [-Button <MouseButton>] [-StepDelay <int>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Simulates dragging the mouse.

## EXAMPLES

### EXAMPLE 1
```powershell
Invoke-DesktopMouseDrag -Button Left -StartX 0 -StartY 0 -EndX 100 -EndY 100
```


## PARAMETERS

### -Button
Button to hold. Defaults to Left.

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

### -EndX
Ending X coordinate.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -EndY
Ending Y coordinate.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 3
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StartX
Starting X coordinate.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StartY
Starting Y coordinate.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StepDelay
Delay in milliseconds between steps.

```yaml
Type: Int32
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
