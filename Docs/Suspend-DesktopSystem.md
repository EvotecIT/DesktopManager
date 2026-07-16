---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Suspend-DesktopSystem
## SYNOPSIS
Requests Windows sleep or hibernation.

## SYNTAX
### __AllParameterSets
```powershell
Suspend-DesktopSystem [-Hibernate] [-Force] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Requests Windows sleep or hibernation.

## EXAMPLES

### EXAMPLE 1
```powershell
Suspend-DesktopSystem -Force
```


## PARAMETERS

### -Force
Forces immediate suspension.

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

### -Hibernate
Requests hibernation instead of sleep.

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
