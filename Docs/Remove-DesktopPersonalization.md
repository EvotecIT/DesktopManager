---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Remove-DesktopPersonalization
## SYNOPSIS
Removes a stored personalization snapshot.

## SYNTAX
### __AllParameterSets
```powershell
Remove-DesktopPersonalization [-Name] <string> [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Removes a stored personalization snapshot.

## EXAMPLES

### EXAMPLE 1
```powershell
Remove-DesktopPersonalization -Name 'Name'
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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `None`

## RELATED LINKS

- None
