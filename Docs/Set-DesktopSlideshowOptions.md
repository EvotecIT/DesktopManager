---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version:
schema: 2.0.0
---

# Set-DesktopSlideshowOptions

## SYNOPSIS
Sets desktop wallpaper slideshow options.

## SYNTAX

```
Set-DesktopSlideshowOptions [-Shuffle] [-NoShuffle] [-SlideshowTick <UInt32>] [<CommonParameters>]
```

## DESCRIPTION
Updates slideshow shuffle behavior and the slideshow tick interval without replacing the configured slideshow images.

Use `Start-DesktopSlideshow` when you need to replace the slideshow image set.

## EXAMPLES

### Example 1
```powershell
PS C:\> Set-DesktopSlideshowOptions -Shuffle -SlideshowTick 300000
```

Enables randomized image order and sets the slideshow tick interval to 300000 milliseconds.

### Example 2
```powershell
PS C:\> Set-DesktopSlideshowOptions -NoShuffle
```

Disables randomized image order while preserving the current slideshow tick interval.

## PARAMETERS

### -NoShuffle
Disable randomized image order.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Shuffle
Enable randomized image order.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -SlideshowTick
Slideshow tick interval in milliseconds.

```yaml
Type: UInt32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: 0
Accept pipeline input: False
Accept wildcard characters: False
```
