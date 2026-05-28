---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Step-DesktopSlideshow
## SYNOPSIS
Steps the desktop wallpaper slideshow.

## SYNTAX
### __AllParameterSets
```powershell
Step-DesktopSlideshow [-Direction] <DesktopSlideshowDirection> [<CommonParameters>]
```

## DESCRIPTION
Steps the desktop wallpaper slideshow.

Moves the wallpaper slideshow forward or backward on all monitors.

## EXAMPLES

### EXAMPLE 1
```powershell
Step-DesktopSlideshow -Direction 'Value'
```


## PARAMETERS

### -Direction
Direction to advance the slideshow.

```yaml
Type: DesktopSlideshowDirection
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Forward, Backward

Required: True
Position: 0
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
