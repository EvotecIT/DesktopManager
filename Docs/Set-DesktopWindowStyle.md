---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Set-DesktopWindowStyle
## SYNOPSIS
Modifies style flags on a desktop window.

## SYNTAX
### __AllParameterSets
```powershell
Set-DesktopWindowStyle [-Name] <string> [-Style <WindowStyleFlags>] [-ExStyle <WindowExStyleFlags>] [-Disable] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Adds or removes style flags on a desktop window.

## EXAMPLES

### EXAMPLE 1
```powershell
Set-DesktopWindowStyle -Name "*Notepad*" -Style MaximizeBox
```

Enable the maximize box on Notepad

## PARAMETERS

### -Disable
Remove the specified flags instead of adding them.

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

### -ExStyle
Extended style flags to change.

```yaml
Type: WindowExStyleFlags
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: TopMost, Layered

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
Window title to modify. Supports wildcards.

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

### -Style
Standard style flags to change.

```yaml
Type: WindowStyleFlags
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: MaximizeBox, MinimizeBox, ThickFrame, SysMenu

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
