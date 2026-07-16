---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Start-DesktopKeepAwake
## SYNOPSIS
Prevents selected Windows idle power behaviors for a bounded duration.

## SYNTAX
### __AllParameterSets
```powershell
Start-DesktopKeepAwake [-Duration] <timespan> [-Display] [-AwayMode] [<CommonParameters>]
```

## DESCRIPTION
Prevents selected Windows idle power behaviors for a bounded duration.

## EXAMPLES

### EXAMPLE 1
```powershell
Start-DesktopKeepAwake -AwayMode
```


## PARAMETERS

### -AwayMode
Also requests away mode.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Display
Also prevents the display from turning off.

```yaml
Type: SwitchParameter
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
How long the keep-awake lease should remain active.

```yaml
Type: TimeSpan
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

- `System.Object`

## RELATED LINKS

- None
