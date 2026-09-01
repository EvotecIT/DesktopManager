---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Start-DesktopSlideshow
## SYNOPSIS
Starts a desktop wallpaper slideshow.

## SYNTAX
### __AllParameterSets
```powershell
Start-DesktopSlideshow [-ImagePath] <string[]> [-Shuffle] [-SlideshowTick <uint>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Starts a desktop wallpaper slideshow.

Begins a slideshow using the provided image paths for all monitors.

## EXAMPLES

### EXAMPLE 1
```powershell
Start-DesktopSlideshow -ImagePath 'C:\Wallpapers\img1.jpg','C:\Wallpapers\img2.jpg'
```


## PARAMETERS

### -ImagePath
Paths to images used for the slideshow.

```yaml
Type: String[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Shuffle
Enables randomized image order for the slideshow.

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
