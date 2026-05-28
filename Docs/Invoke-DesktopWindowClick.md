---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Invoke-DesktopWindowClick
## SYNOPSIS
Clicks a point relative to a desktop window.

## SYNTAX
### ByName
```powershell
Invoke-DesktopWindowClick [-Name] <string> [-X <int>] [-Y <int>] [-XRatio <double>] [-YRatio <double>] [-TargetName <string>] [-Button <MouseButton>] [-Activate] [-ClientArea] [-Verify] [-VerificationTolerancePixels <int>] [-PassThru] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### ActiveWindow
```powershell
Invoke-DesktopWindowClick -ActiveWindow [-X <int>] [-Y <int>] [-XRatio <double>] [-YRatio <double>] [-TargetName <string>] [-Button <MouseButton>] [-Activate] [-ClientArea] [-Verify] [-VerificationTolerancePixels <int>] [-PassThru] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Clicks a point relative to a matching desktop window.

## EXAMPLES

### EXAMPLE 1
```powershell
Invoke-DesktopWindowClick -Name "*Notepad*" -X 200 -Y 200
```


## PARAMETERS

### -Activate
Activate the window before clicking.

```yaml
Type: SwitchParameter
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
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
Accept wildcard characters: True
```

### -Button
Mouse button to use for the click.

```yaml
Type: MouseButton
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values: Left, Right

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
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
Accept wildcard characters: True
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
Accept wildcard characters: True
```

### -PassThru
Return a structured mutation result object for the clicked window.

```yaml
Type: SwitchParameter
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -TargetName
Saved reusable target name to click instead of supplying coordinates directly.

```yaml
Type: String
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
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
Accept wildcard characters: True
```

### -Verify
Re-query the target window after the click and report the observed postcondition.

```yaml
Type: SwitchParameter
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -X
Horizontal coordinate relative to the window bounds.

```yaml
Type: Nullable`1
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -XRatio
Horizontal coordinate ratio from 0 to 1 relative to the target bounds.

```yaml
Type: Nullable`1
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Y
Vertical coordinate relative to the window bounds.

```yaml
Type: Nullable`1
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -YRatio
Vertical coordinate ratio from 0 to 1 relative to the target bounds.

```yaml
Type: Nullable`1
Parameter Sets: ByName, ActiveWindow
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
