---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Invoke-DesktopWindowScroll
## SYNOPSIS
Scrolls at a point relative to a desktop window.

## SYNTAX
### ByName
```powershell
Invoke-DesktopWindowScroll [-Name] <string> -Delta <int> [-X <Int32>] [-Y <Int32>] [-XRatio <Double>] [-YRatio <Double>] [-TargetName <string>] [-Activate] [-ClientArea] [-Verify] [-VerificationTolerancePixels <int>] [-PassThru] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### ActiveWindow
```powershell
Invoke-DesktopWindowScroll -ActiveWindow -Delta <int> [-X <Int32>] [-Y <Int32>] [-XRatio <Double>] [-YRatio <Double>] [-TargetName <string>] [-Activate] [-ClientArea] [-Verify] [-VerificationTolerancePixels <int>] [-PassThru] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Simulates mouse-wheel scrolling at a point relative to a matching desktop window.

## EXAMPLES

### EXAMPLE 1
```powershell
Invoke-DesktopWindowScroll -Name "*Notepad*" -X 200 -Y 200 -Delta -120 -ClientArea
```


## PARAMETERS

### -Activate
Activate the window before scrolling.

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

### -Delta
Scroll delta. Positive values scroll up.

```yaml
Type: Int32
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: True
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
Return a structured mutation result object for the scrolled window.

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

### -TargetName
Saved reusable target name to scroll at instead of supplying coordinates directly.

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
Re-query the target window after the scroll action and report the observed postcondition.

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

### -X
Horizontal coordinate relative to the target window.

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

### -XRatio
Horizontal coordinate ratio from 0 to 1 relative to the target bounds.

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

### -Y
Vertical coordinate relative to the target window.

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

### -YRatio
Vertical coordinate ratio from 0 to 1 relative to the target bounds.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `None`

## RELATED LINKS

- None
