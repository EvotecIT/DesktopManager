---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Set-DesktopWindowTarget
## SYNOPSIS
Saves or updates a reusable window-relative target.

## SYNTAX
### __AllParameterSets
```powershell
Set-DesktopWindowTarget [-Name] <string> [-Description <string>] [-X <Int32>] [-Y <Int32>] [-XRatio <Double>] [-YRatio <Double>] [-ClientArea] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Persists a named DesktopManager window target.

## EXAMPLES

### EXAMPLE 1
```powershell
Set-DesktopWindowTarget -Name "editor-center" -XRatio 0.5 -YRatio 0.5 -ClientArea
```


## PARAMETERS

### -ClientArea
Interpret the target relative to the window client area instead of the outer bounds.

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

### -Description
Optional target description.

```yaml
Type: String
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
Saved target name.

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

### -X
Horizontal coordinate relative to the target bounds.

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

### -XRatio
Horizontal coordinate ratio from 0 to 1 relative to the target bounds.

```yaml
Type: Double
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Y
Vertical coordinate relative to the target bounds.

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

### -YRatio
Vertical coordinate ratio from 0 to 1 relative to the target bounds.

```yaml
Type: Double
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
