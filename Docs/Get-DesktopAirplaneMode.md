---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Get-DesktopAirplaneMode
## SYNOPSIS
Gets global airplane mode through an undocumented experimental Windows COM contract.

## SYNTAX
### __AllParameterSets
```powershell
Get-DesktopAirplaneMode -Experimental [<CommonParameters>]
```

## DESCRIPTION
Gets global airplane mode through an undocumented experimental Windows COM contract.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-DesktopAirplaneMode -Experimental
```


## PARAMETERS

### -Experimental
Acknowledges that the global airplane-mode contract is experimental.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
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

- `DesktopManager.AirplaneModeState`

## RELATED LINKS

- None
