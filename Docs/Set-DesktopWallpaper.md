---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Set-DesktopWallpaper
## SYNOPSIS
Sets the desktop wallpaper for one or more monitors.

## SYNTAX
### Index (Default)
```powershell
Set-DesktopWallpaper [[-Index] <Int32>] [-ConnectedOnly] [-PrimaryOnly] [[-WallpaperPosition] <DesktopWallpaperPosition>] [[-WallpaperPath] <string>] [-AllUsers] [-ExcludeDefaultUserProfile] [-Url <string>] [-ImageData <Stream>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### DeviceId
```powershell
Set-DesktopWallpaper [[-DeviceId] <string>] [[-WallpaperPosition] <DesktopWallpaperPosition>] [[-WallpaperPath] <string>] [-AllUsers] [-ExcludeDefaultUserProfile] [-Url <string>] [-ImageData <Stream>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### DeviceName
```powershell
Set-DesktopWallpaper [[-DeviceName] <string>] [[-WallpaperPosition] <DesktopWallpaperPosition>] [[-WallpaperPath] <string>] [-AllUsers] [-ExcludeDefaultUserProfile] [-Url <string>] [-ImageData <Stream>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### All
```powershell
Set-DesktopWallpaper [-All] [[-WallpaperPosition] <DesktopWallpaperPosition>] [[-WallpaperPath] <string>] [-AllUsers] [-ExcludeDefaultUserProfile] [-Url <string>] [-ImageData <Stream>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Sets the desktop wallpaper for one or more monitors.

Sets the desktop wallpaper for one or more monitors. You can specify the monitor by index, device ID, or device name. You can also set the wallpaper for all monitors or only the primary monitor. Optionally, you can specify the wallpaper position.

## EXAMPLES

### EXAMPLE 1
```powershell
Set-DesktopWallpaper -All -WallpaperPath "C:\Path\To\Wallpaper.jpg"
```

Set the wallpaper for all monitors

### EXAMPLE 2
```powershell
Set-DesktopWallpaper -Index 1 -WallpaperPath "C:\Path\To\Wallpaper.jpg"
```

Set the wallpaper for a specific monitor by index

### EXAMPLE 3
```powershell
Set-DesktopWallpaper -PrimaryOnly -WallpaperPath "C:\Path\To\Wallpaper.jpg"
```

Set the wallpaper for the primary monitor only

## PARAMETERS

### -All
Set the wallpaper for all monitors.

```yaml
Type: SwitchParameter
Parameter Sets: All
Aliases: None
Possible values:

Required: False
Position: 5
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -AllUsers
Apply the wallpaper for all user profiles.

```yaml
Type: SwitchParameter
Parameter Sets: Index, DeviceId, DeviceName, All
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ConnectedOnly
Set the wallpaper for connected monitors only.

```yaml
Type: SwitchParameter
Parameter Sets: Index
Aliases: None
Possible values:

Required: False
Position: 3
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DeviceId
The device ID of the monitor to set the wallpaper for.

```yaml
Type: String
Parameter Sets: DeviceId
Aliases: MonitorID
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DeviceName
The device name of the monitor to set the wallpaper for.

```yaml
Type: String
Parameter Sets: DeviceName
Aliases: None
Possible values:

Required: False
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExcludeDefaultUserProfile
Exclude the default user profile when applying to all users.

```yaml
Type: SwitchParameter
Parameter Sets: Index, DeviceId, DeviceName, All
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ImageData
Image data stream to use as wallpaper.

```yaml
Type: Stream
Parameter Sets: Index, DeviceId, DeviceName, All
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Index
The index of the monitor to set the wallpaper for.

```yaml
Type: Int32
Parameter Sets: Index
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PrimaryOnly
Set the wallpaper for the primary monitor only.

```yaml
Type: SwitchParameter
Parameter Sets: Index
Aliases: None
Possible values:

Required: False
Position: 4
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Url
URL of the wallpaper image.

```yaml
Type: String
Parameter Sets: Index, DeviceId, DeviceName, All
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WallpaperPath
The file path of the wallpaper image.

```yaml
Type: String
Parameter Sets: Index, DeviceId, DeviceName, All
Aliases: FilePath, Path
Possible values:

Required: False
Position: 7
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WallpaperPosition
The position of the wallpaper on the monitor.

```yaml
Type: DesktopWallpaperPosition
Parameter Sets: Index, DeviceId, DeviceName, All
Aliases: Position
Possible values: Center, Tile, Stretch, Fit, Fill, Span

Required: False
Position: 6
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
