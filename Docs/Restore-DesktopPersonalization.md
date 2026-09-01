---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Restore-DesktopPersonalization
## SYNOPSIS
Restores a stored personalization snapshot.

## SYNTAX
### __AllParameterSets
```powershell
Restore-DesktopPersonalization [-Name] <string> [-SkipMachinePolicies] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Restores a stored personalization snapshot.

## EXAMPLES

### EXAMPLE 1
```powershell
Restore-DesktopPersonalization -Name 'Name'
```


## PARAMETERS

### -Name
The stored snapshot name.

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

### -SkipMachinePolicies
Skips machine-wide lock-screen and Spotlight policy values.

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
