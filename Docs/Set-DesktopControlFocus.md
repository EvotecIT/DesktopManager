---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Set-DesktopControlFocus
## SYNOPSIS
Focuses a desktop control.

## SYNTAX
### __AllParameterSets
```powershell
Set-DesktopControlFocus [-Control] <WindowControlInfo> [-EnsureForeground] [-PassThru] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Focuses a desktop control.

Sets focus to a previously resolved control and returns the observed post-mutation state when requested.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-DesktopWindowControl -ActiveWindow | Select-Object -First 1 | Set-DesktopControlFocus
```


## PARAMETERS

### -Control
Control to focus.

```yaml
Type: WindowControlInfo
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -EnsureForeground
Ensure the parent window becomes foreground before focusing the control.

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

### -PassThru
Return the updated control state.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `DesktopManager.WindowControlInfo`

## OUTPUTS

- `None`

## RELATED LINKS

- None
