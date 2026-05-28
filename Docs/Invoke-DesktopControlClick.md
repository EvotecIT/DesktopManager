---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Invoke-DesktopControlClick
## SYNOPSIS
Clicks a specific window control.

## SYNTAX
### __AllParameterSets
```powershell
Invoke-DesktopControlClick [-Control] <WindowControlInfo> [-Button <MouseButton>] [-Verify] [-PassThru] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Invokes a click on a window control.

## EXAMPLES

### EXAMPLE 1
```powershell
Invoke-DesktopControlClick -Control $ctrl
```


## PARAMETERS

### -Button
Mouse button to use. Defaults to Left.

```yaml
Type: MouseButton
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Left, Right

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Control
Control to click.

```yaml
Type: WindowControlInfo
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
Return a structured mutation result object for the clicked control.

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

### -Verify
Re-query the control after the click and report the observed postcondition.

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
