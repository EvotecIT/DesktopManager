---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Set-DesktopWindow
## SYNOPSIS
Sets the position, size and state of a desktop window.

## SYNTAX
### __AllParameterSets
```powershell
Set-DesktopWindow [-Name] <string> [-Left <int>] [-Top <int>] [-Width <int>] [-Height <int>] [-MonitorIndex <Int32>] [-State <WindowState>] [-TopMost] [-Activate] [-IncludeHidden] [-IncludeCloaked] [-Verify] [-VerificationTolerancePixels <int>] [-PassThru] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Sets the position, size and state of a desktop window.

Sets the position, size and state of a window on the desktop. You can identify the window by its title (supports wildcards).

Positions and sizes use physical desktop pixels, including from DPI-unaware PowerShell hosts. On a 150% display, a 900-pixel width remains 900 physical pixels rather than 1350. Hidden and DWM-cloaked windows are skipped unless explicitly included.

## EXAMPLES

### EXAMPLE 1
```powershell
Set-DesktopWindow -Name "Calculator" -Left 100 -Top 100
```

Move a specific window to coordinates (100,100)

### EXAMPLE 2
```powershell
Set-DesktopWindow -Name "Notepad" -Left 100 -Top 100 -Width 800 -Height 600
```

Set window position and size

### EXAMPLE 3
```powershell
Set-DesktopWindow -Name "Calculator" -State Minimize
```

Minimize a window

### EXAMPLE 4
```powershell
Set-DesktopWindow -Name "Notepad" -Left -1800 -Top 100
```

Move a window to a monitor left of the primary monitor

### EXAMPLE 5
```powershell
Set-DesktopWindow -Name "Notepad" -TopMost:$false
```

Clear a window's topmost state

## PARAMETERS

### -Activate
Activate the window.

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

### -Height
The height of the window. If not specified, current height is maintained.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -IncludeCloaked
Also change DWM-cloaked windows that match the name, including windows on other virtual desktops. Cloaked windows, such as suspended app frames, are skipped by default because changing them can make them appear.

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

### -IncludeHidden
Also change hidden windows that match the name. Hidden windows are skipped by default.

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

### -Left
The left position of the window. Negative values reach monitors left of the primary monitor.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MonitorIndex
Target monitor index to move the window to.

```yaml
Type: Int32
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
The title of the window to move. Supports wildcards.

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

### -PassThru
Return a structured mutation result object for each matching window.

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

### -State
The desired window state (Normal, Minimize, Maximize, or Close).

```yaml
Type: WindowState
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Normal, Minimize, Maximize, Close

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Top
The top position of the window. Negative values reach monitors above the primary monitor.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TopMost
Set the window as top-most. Use -TopMost:$false to clear it.

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

### -VerificationTolerancePixels
Geometry verification tolerance in pixels.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Verify
Re-query the mutated window and report the observed postcondition instead of relying only on mutation success.

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

### -Width
The width of the window. If not specified, current width is maintained.

```yaml
Type: Int32
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
