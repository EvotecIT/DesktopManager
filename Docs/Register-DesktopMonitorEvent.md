---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Register-DesktopMonitorEvent
## SYNOPSIS
Registers for desktop monitor change events.

## SYNTAX
### __AllParameterSets
```powershell
Register-DesktopMonitorEvent [-Action <scriptblock>] [-Duration <timespan>] [<CommonParameters>]
```

## DESCRIPTION
Registers for desktop monitor change events.

Subscribes to display setting changes and returns the event subscription.

## EXAMPLES

### EXAMPLE 1
```powershell
Register-DesktopMonitorEvent -Duration (New-TimeSpan -Minutes 5)
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
Accept wildcard characters: False
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
