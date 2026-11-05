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
Move-DesktopWindowToVirtualDesktop [-Handle] <IntPtr> [-DesktopId] <guid> [-Experimental] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Moves a top-level window to a known virtual desktop.

The supported Windows API only moves windows that belong to the calling process; Windows refuses other processes' windows with access denied. Use -Experimental to move another process's window through the undocumented, version-gated Windows 11 shell contract.

## EXAMPLES

### EXAMPLE 1
```powershell
Move-DesktopWindowToVirtualDesktop -Handle $window.Handle -DesktopId $desktop.Id -Experimental
```

Move another application's window to a desktop

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
Accept wildcard characters: False
```

### -Experimental
When Windows refuses to move another process's window, move it through the experimental Windows 11 shell contract (builds 26100, 26200 and 26300). A window pinned to all desktops is unpinned; an application-wide pin is left alone and reported.

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
