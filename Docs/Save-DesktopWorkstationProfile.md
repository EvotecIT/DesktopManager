---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Save-DesktopWorkstationProfile
## SYNOPSIS
Captures and saves a named workstation profile.

## SYNTAX
### __AllParameterSets
```powershell
Save-DesktopWorkstationProfile [-Name] <string> [<CommonParameters>]
```

## DESCRIPTION
Stores display, personalization, taskbar, and active audio state together.

## EXAMPLES

### EXAMPLE 1
```powershell
Save-DesktopWorkstationProfile -Name 'Name'
```


## PARAMETERS

### -Name
The profile name.

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

- `DesktopManager.WorkstationProfile`

## RELATED LINKS

- None
