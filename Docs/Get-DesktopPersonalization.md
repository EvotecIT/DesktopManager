---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Get-DesktopPersonalization
## SYNOPSIS
Gets current or stored personalization state.

## SYNTAX
### __AllParameterSets
```powershell
Get-DesktopPersonalization [[-Name] <string>] [-List] [<CommonParameters>]
```

## DESCRIPTION
Gets current or stored personalization state.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-DesktopPersonalization -Name 'Name'
```


## PARAMETERS

### -List
Lists stored snapshot names.

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

### -Name
Optional stored snapshot name.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
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

- `DesktopManager.PersonalizationSnapshot`

## RELATED LINKS

- None
