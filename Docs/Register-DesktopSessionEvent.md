---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Register-DesktopSessionEvent
## SYNOPSIS
Registers for meaningful current-session changes.

## SYNTAX
### __AllParameterSets
```powershell
Register-DesktopSessionEvent [-Interval <timespan>] [-Action <scriptblock>] [-Duration <timespan>] [<CommonParameters>]
```

## DESCRIPTION
Registers for meaningful current-session changes.

## EXAMPLES

### EXAMPLE 1
```powershell
Register-DesktopSessionEvent -Action { }
```


## PARAMETERS

### -Action
Optional script block invoked for each change.

```yaml
Type: ScriptBlock
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
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
Accept wildcard characters: False
```

### -Interval
Polling interval used to observe session state.

```yaml
Type: TimeSpan
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
