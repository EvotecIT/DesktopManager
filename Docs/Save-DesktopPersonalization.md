---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Save-DesktopPersonalization
## SYNOPSIS
Captures and saves current personalization state.

## SYNTAX
### __AllParameterSets
```powershell
Save-DesktopPersonalization [-Name] <string> [<CommonParameters>]
```

## DESCRIPTION
Captures and saves current personalization state.

## EXAMPLES

### EXAMPLE 1
```powershell
Save-DesktopPersonalization -Name 'Name'
```


## PARAMETERS

### -Name
The snapshot name.

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

- `DesktopManager.PersonalizationSnapshot`

## RELATED LINKS

- None
