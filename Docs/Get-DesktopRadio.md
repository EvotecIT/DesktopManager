---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Get-DesktopRadio
## SYNOPSIS
Gets radios through the supported Windows radio API.

## SYNTAX
### __AllParameterSets
```powershell
Get-DesktopRadio [-Kind <DesktopRadioKind>] [<CommonParameters>]
```

## DESCRIPTION
Gets radios through the supported Windows radio API.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-DesktopRadio -Kind 'Value'
```


## PARAMETERS

### -Kind
Optional radio technology filter.

```yaml
Type: Nullable`1
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

- `DesktopManager.DesktopRadioInfo`

## RELATED LINKS

- None
