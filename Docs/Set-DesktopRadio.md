---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Set-DesktopRadio
## SYNOPSIS
Sets an explicit state through the supported Windows radio API.

## SYNTAX
### __AllParameterSets
```powershell
Set-DesktopRadio [-Kind] <DesktopRadioKind> [-State] <DesktopRadioState> [-Name <string>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Sets an explicit state through the supported Windows radio API.

## EXAMPLES

### EXAMPLE 1
```powershell
Set-DesktopRadio -Name 'Name'
```


## PARAMETERS

### -Kind
The radio technology to select.

```yaml
Type: DesktopRadioKind
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Other, WiFi, MobileBroadband, Bluetooth, FM

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
Optional exact Windows-provided radio name.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -State
The explicit On or Off state.

```yaml
Type: DesktopRadioState
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Unknown, On, Off, Disabled

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `DesktopManager.DesktopRadioSetResult`

## RELATED LINKS

- None
