---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Get-DesktopControlState
## SYNOPSIS
Gets the observable state for a desktop control.

## SYNTAX
### __AllParameterSets
```powershell
Get-DesktopControlState [-Control] <WindowControlInfo> [<CommonParameters>]
```

## DESCRIPTION
Gets the observable state for a desktop control.

Returns the current enabled, visible, focused, and capability state for a previously resolved window control.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-DesktopWindowControl -ActiveWindow | Select-Object -First 1 | Get-DesktopControlState
```


## PARAMETERS

### -Control
Control to inspect.

```yaml
Type: WindowControlInfo
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: True
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `DesktopManager.WindowControlInfo`

## OUTPUTS

- `System.Object`

## RELATED LINKS

- None
