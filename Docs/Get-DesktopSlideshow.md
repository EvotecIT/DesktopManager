---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version:
schema: 2.0.0
---

# Get-DesktopSlideshow

## SYNOPSIS
Gets the desktop wallpaper slideshow configuration and state.

## SYNTAX

```
Get-DesktopSlideshow [<CommonParameters>]
```

## DESCRIPTION
Returns the configured slideshow image paths, state flags, options, shuffle state and slideshow tick interval in milliseconds.

## EXAMPLES

### Example 1
```powershell
PS C:\> Get-DesktopSlideshow
```

Gets the current wallpaper slideshow state.

### Example 2
```powershell
PS C:\> Get-DesktopSlideshow | Select-Object IsRunning, ShuffleImages, SlideshowTick, ImagePaths
```

Shows the operational slideshow fields commonly used by automation.
