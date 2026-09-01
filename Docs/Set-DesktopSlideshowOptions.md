---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Set-DesktopSlideshowOptions
## SYNOPSIS
Sets desktop wallpaper slideshow options.

## SYNTAX
### __AllParameterSets
```powershell
Set-DesktopSlideshowOptions [-Shuffle] [-NoShuffle] [-SlideshowTick <uint>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Sets desktop wallpaper slideshow options.

Updates slideshow shuffle behavior and tick interval without replacing the slideshow images.

## EXAMPLES

### EXAMPLE 1
```powershell
Set-DesktopSlideshowOptions -NoShuffle
```


## PARAMETERS

### -NoShuffle
Disable randomized image order.

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

### -Shuffle
Enable randomized image order.

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

### -SlideshowTick
Slideshow tick interval in milliseconds.

```yaml
Type: UInt32
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
