---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Stop-DesktopWindowKeepAlive
## SYNOPSIS
Stops keep-alive messages for a window.

## SYNTAX
### __AllParameterSets
```powershell
Stop-DesktopWindowKeepAlive [[-Name] <string>] [-All] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Stops keep-alive messages for a window.

Stops sending periodic input messages previously started with Start-DesktopWindowKeepAlive.

## EXAMPLES

### EXAMPLE 1
```powershell
Stop-DesktopWindowKeepAlive -Name "*Notepad*"
```


## PARAMETERS

### -All
Stop all keep-alive sessions.

```yaml
Type: SwitchParameter
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

Required: False
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
