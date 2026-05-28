---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Set-DesktopWindowSnap
## SYNOPSIS
Snaps a desktop window to a predefined position.

## SYNTAX
### __AllParameterSets
```powershell
Set-DesktopWindowSnap [-Name] <string> [-Position] <SnapPosition> [-Verify] [-VerificationTolerancePixels <int>] [-PassThru] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Snaps a desktop window to a predefined position.

## EXAMPLES

### EXAMPLE 1
```powershell
Set-DesktopWindowSnap -Name "*Notepad*" -Position Left
```

Snap Notepad to the left half of the screen

## PARAMETERS

### -Name
The window title to snap. Supports wildcards.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -PassThru
Return a structured mutation result object for each matching window.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Position
The snap position.

```yaml
Type: SnapPosition
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Left, Right, TopLeft, TopRight, BottomLeft, BottomRight

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -VerificationTolerancePixels
Geometry verification tolerance in pixels for post-snap checks.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Verify
Re-query the snapped window and report the observed postcondition.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
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
