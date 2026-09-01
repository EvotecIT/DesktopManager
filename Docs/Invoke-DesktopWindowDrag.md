---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Invoke-DesktopWindowDrag
## SYNOPSIS
Drags between two points relative to a desktop window.

## SYNTAX
### ByName
```powershell
Invoke-DesktopWindowDrag [-Name] <string> [-StartX <Int32>] [-StartY <Int32>] [-EndX <Int32>] [-EndY <Int32>] [-StartXRatio <Double>] [-StartYRatio <Double>] [-EndXRatio <Double>] [-EndYRatio <Double>] [-StartTargetName <string>] [-EndTargetName <string>] [-Button <MouseButton>] [-StepDelayMilliseconds <int>] [-Activate] [-ClientArea] [-Verify] [-VerificationTolerancePixels <int>] [-PassThru] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### ActiveWindow
```powershell
Invoke-DesktopWindowDrag -ActiveWindow [-StartX <Int32>] [-StartY <Int32>] [-EndX <Int32>] [-EndY <Int32>] [-StartXRatio <Double>] [-StartYRatio <Double>] [-EndXRatio <Double>] [-EndYRatio <Double>] [-StartTargetName <string>] [-EndTargetName <string>] [-Button <MouseButton>] [-StepDelayMilliseconds <int>] [-Activate] [-ClientArea] [-Verify] [-VerificationTolerancePixels <int>] [-PassThru] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Simulates dragging between two points relative to a matching desktop window.

## EXAMPLES

### EXAMPLE 1
```powershell
Invoke-DesktopWindowDrag -Name "*Notepad*" -StartX 200 -StartY 200 -EndX 500 -EndY 200 -ClientArea
```


## PARAMETERS

### -Activate
Activate the window before dragging.

```yaml
Type: SwitchParameter
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ActiveWindow
Use the current foreground window instead of matching by name.

```yaml
Type: SwitchParameter
Parameter Sets: ActiveWindow
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Button
Mouse button to hold during the drag.

```yaml
Type: MouseButton
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values: Left, Right

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ClientArea
Interpret the supplied coordinates relative to the client area instead of the outer window bounds.

```yaml
Type: SwitchParameter
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -EndTargetName
Saved reusable ending target name to drag to instead of supplying ending coordinates directly.

```yaml
Type: String
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -EndX
Horizontal ending coordinate relative to the target window.

```yaml
Type: Int32
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -EndXRatio
Horizontal ending coordinate ratio from 0 to 1 relative to the target bounds.

```yaml
Type: Double
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -EndY
Vertical ending coordinate relative to the target window.

```yaml
Type: Int32
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -EndYRatio
Vertical ending coordinate ratio from 0 to 1 relative to the target bounds.

```yaml
Type: Double
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
Title of the window to match. Supports wildcards.

```yaml
Type: String
Parameter Sets: ByName
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PassThru
Return a structured mutation result object for the dragged window.

```yaml
Type: SwitchParameter
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StartTargetName
Saved reusable starting target name to drag from instead of supplying starting coordinates directly.

```yaml
Type: String
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StartX
Horizontal starting coordinate relative to the target window.

```yaml
Type: Int32
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StartXRatio
Horizontal starting coordinate ratio from 0 to 1 relative to the target bounds.

```yaml
Type: Double
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StartY
Vertical starting coordinate relative to the target window.

```yaml
Type: Int32
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StartYRatio
Vertical starting coordinate ratio from 0 to 1 relative to the target bounds.

```yaml
Type: Double
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StepDelayMilliseconds
Delay in milliseconds between drag steps.

```yaml
Type: Int32
Parameter Sets: ByName, ActiveWindow
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
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Verify
Re-query the target window after the drag and report the observed postcondition.

```yaml
Type: SwitchParameter
Parameter Sets: ByName, ActiveWindow
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
