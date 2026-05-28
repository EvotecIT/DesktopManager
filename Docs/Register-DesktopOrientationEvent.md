---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Register-DesktopOrientationEvent
## SYNOPSIS
Registers for desktop orientation change events.

## SYNTAX
### __AllParameterSets
```powershell
Register-DesktopOrientationEvent [-Action <scriptblock>] [-Duration <timespan>] [<CommonParameters>]
```

## DESCRIPTION
Registers for desktop orientation change events.

Subscribes to orientation changes and returns the event subscription.

## EXAMPLES

### EXAMPLE 1
```powershell
Register-DesktopOrientationEvent -Duration (New-TimeSpan -Minutes 5)
```


## PARAMETERS

### -Action
The script block to run when the event is raised.

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
The duration to monitor before automatically unregistering.

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
