---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version:
schema: 2.0.0
---

# Set-DesktopWallpaper

## SYNOPSIS
Sets the desktop wallpaper for one or more monitors.

## SYNTAX

### Index (Default)
```
Set-DesktopWallpaper [-Index <Int32>] [-WallpaperPath <String>] [-Position <DesktopWallpaperPosition>]
 [<CommonParameters>]
```

### MonitorID
```
Set-DesktopWallpaper [-MonitorID <String>] [-WallpaperPath <String>] [-Position <DesktopWallpaperPosition>]
 [<CommonParameters>]
```

### All
```
Set-DesktopWallpaper [-All] [-WallpaperPath <String>] [-Position <DesktopWallpaperPosition>]
 [<CommonParameters>]
```

## DESCRIPTION
Applies the specified image to the selected monitor. You can target a monitor by index or device identifier, or update all monitors at once. The wallpaper position can be set to Center, Fill, Fit, Stretch, Tile or Span.

## EXAMPLES

### Example 1
```powershell
PS C:\> Set-DesktopWallpaper -Index 0 -WallpaperPath "C:\Wallpapers\image.jpg" -Position Fill
```
Sets the wallpaper on the first monitor using the specified image.

## PARAMETERS

### -All
Apply the wallpaper to every monitor.

```yaml
Type: SwitchParameter
Parameter Sets: All
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Index
Index of the monitor returned by `Get-DesktopMonitor`.

```yaml
Type: Int32
Parameter Sets: Index
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MonitorID
Device identifier of the monitor.

```yaml
Type: String
Parameter Sets: MonitorID
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Position
Wallpaper placement mode.

```yaml
Type: DesktopWallpaperPosition
Parameter Sets: (All)
Aliases:
Accepted values: Center, Tile, Stretch, Fit, Fill, Span

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WallpaperPath
Path to the image file used as wallpaper.

```yaml
Type: String
Parameter Sets: (All)
Aliases: FilePath

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None

## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS
