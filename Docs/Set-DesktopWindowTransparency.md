---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Set-DesktopWindowTransparency
## SYNOPSIS
Sets the transparency level of a desktop window.

## SYNTAX
### __AllParameterSets
```powershell
Set-DesktopWindowTransparency [-Name] <string> [-Alpha] <byte> [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Sets the transparency level of a desktop window.

## EXAMPLES

### EXAMPLE 1
```powershell
Set-DesktopWindowTransparency -Name "*Notepad*" -Alpha 128
```

Make Notepad semi-transparent

## PARAMETERS

### -Alpha
Transparency alpha from 0 (transparent) to 255 (opaque).

```yaml
Type: Byte
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Name
Title of the window. Supports wildcards.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `System.Object`

## RELATED LINKS

- None
