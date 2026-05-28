---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Set-DesktopControlVisibility
## SYNOPSIS
Shows or hides a desktop control.

## SYNTAX
### __AllParameterSets
```powershell
Set-DesktopControlVisibility [-Control] <WindowControlInfo> -Visible <bool> [-PassThru] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Shows or hides a desktop control.

Updates the visibility state of a previously resolved Win32-backed control.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-DesktopWindowControl -ActiveWindow | Select-Object -First 1 | Set-DesktopControlVisibility -Visible:$false -PassThru
```


## PARAMETERS

### -Control
Control to update.

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
Accept wildcard characters: True
```

### -Visible
True to show the control; false to hide it.

```yaml
Type: Boolean
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
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
