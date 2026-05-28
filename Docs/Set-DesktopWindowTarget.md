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
Set-DesktopWindowTarget [-Name] <string> [-Description <string>] [-X <int>] [-Y <int>] [-XRatio <double>] [-YRatio <double>] [-ClientArea] [-WhatIf] [-Confirm] [<CommonParameters>]
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
Accept wildcard characters: True
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
Accept wildcard characters: True
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
Accept wildcard characters: True
```

### -X
Horizontal coordinate relative to the target bounds.

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

### -XRatio
Horizontal coordinate ratio from 0 to 1 relative to the target bounds.

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

### -Y
Vertical coordinate relative to the target bounds.

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

### -YRatio
Vertical coordinate ratio from 0 to 1 relative to the target bounds.

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
