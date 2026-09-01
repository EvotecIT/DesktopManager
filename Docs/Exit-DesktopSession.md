---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Exit-DesktopSession
## SYNOPSIS
Signs out the current interactive Windows session.

## SYNTAX
### __AllParameterSets
```powershell
Exit-DesktopSession [-Force] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Signs out the current interactive Windows session.

## EXAMPLES

### EXAMPLE 1
```powershell
Exit-DesktopSession -Force
```


## PARAMETERS

### -Force
Forces applications to close during sign-out.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `None`

## RELATED LINKS

- None
