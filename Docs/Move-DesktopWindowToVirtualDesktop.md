---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Move-DesktopWindowToVirtualDesktop
## SYNOPSIS
Moves a top-level window to a known virtual desktop.

## SYNTAX
### __AllParameterSets
```powershell
Move-DesktopWindowToVirtualDesktop [-Handle] <IntPtr> [-DesktopId] <guid> [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Moves a top-level window to a known virtual desktop.

## EXAMPLES

### EXAMPLE 1
```powershell
Move-DesktopWindowToVirtualDesktop -DesktopId 'Value'
```


## PARAMETERS

### -DesktopId
A desktop identifier obtained from a top-level window.

```yaml
Type: Guid
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Handle
The top-level window handle.

```yaml
Type: IntPtr
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
