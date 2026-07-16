---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Register-DesktopRadioEvent
## SYNOPSIS
Registers for supported Windows radio state changes.

## SYNTAX
### __AllParameterSets
```powershell
Register-DesktopRadioEvent [-Action <scriptblock>] [-Duration <timespan>] [<CommonParameters>]
```

## DESCRIPTION
Registers for supported Windows radio state changes.

## EXAMPLES

### EXAMPLE 1
```powershell
Register-DesktopRadioEvent -Action { }
```


## PARAMETERS

### -Action
Optional script block invoked for each state change.

```yaml
Type: ScriptBlock
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Duration
Optional duration before automatic unregistration.

```yaml
Type: TimeSpan
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
