---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Start-DesktopWindowKeepAlive
## SYNOPSIS
Starts sending keep-alive input to a window.

## SYNTAX
### __AllParameterSets
```powershell
Start-DesktopWindowKeepAlive [-Name] <string> [-Interval <timespan>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Starts sending keep-alive input to a window.

Periodically sends a harmless input message to the specified window so that the session stays active.

## EXAMPLES

### EXAMPLE 1
```powershell
Start-DesktopWindowKeepAlive -Name "*Notepad*"
```


## PARAMETERS

### -Interval
Interval between keep-alive messages.

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

### -Name
Window title to match. Supports wildcards.

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

- `None`

## RELATED LINKS

- None
