---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Set-DesktopWindowPlacement
## SYNOPSIS
Applies a reliable reusable placement operation to matching desktop windows.

## SYNTAX
### __AllParameterSets
```powershell
Set-DesktopWindowPlacement [-Name] <string> -Placement <WindowPlacementKind> [-MonitorTarget <WindowMonitorTargetKind>] [-MonitorIndex <int>] [-Left <int>] [-Top <int>] [-Width <int>] [-Height <int>] [-NoVerify] [-PassThru] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Applies reliable desktop window placement.

Uses the shared DesktopManager placement engine to move, resize, restore, or maximize matching windows with root-handle normalization, retry, and verification support.

## EXAMPLES

### EXAMPLE 1
```powershell
Set-DesktopWindowPlacement -Name "Remote Desktop Manager*" -Placement Maximize -MonitorIndex 1 -PassThru
```

Move a window to a monitor and maximize it

### EXAMPLE 2
```powershell
Set-DesktopWindowPlacement -Name "Visual Studio Code*" -Placement ExactRectangle -Left -3840 -Top 19 -Width 1920 -Height 2088
```

Move a window to an exact rectangle, including negative virtual-desktop coordinates

## PARAMETERS

### -Height
Exact height for ExactRectangle placement.

```yaml
Type: Nullable`1
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Left
Exact left coordinate for ExactRectangle placement. Negative virtual-desktop coordinates are supported.

```yaml
Type: Nullable`1
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -MonitorIndex
Explicit DesktopManager monitor index to target.

```yaml
Type: Nullable`1
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -MonitorTarget
The monitor target to use when MonitorIndex is not specified.

```yaml
Type: WindowMonitorTargetKind
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Current, TopLeft, TopRight, BottomLeft, BottomRight

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Name
The title of the window to place. Supports wildcards.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -NoVerify
Skip post-action geometry verification.

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

### -PassThru
Return the placement result, including observed final window state and diagnostic snapshots.

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

### -Placement
The placement to apply.

```yaml
Type: WindowPlacementKind
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Restore, Maximize, LeftHalf, RightHalf, ExactRectangle

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Top
Exact top coordinate for ExactRectangle placement. Negative virtual-desktop coordinates are supported.

```yaml
Type: Nullable`1
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Width
Exact width for ExactRectangle placement.

```yaml
Type: Nullable`1
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
