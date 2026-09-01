---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Invoke-DesktopScreenshot
## SYNOPSIS
Takes a screenshot of the desktop.

## SYNTAX
### __AllParameterSets
```powershell
Invoke-DesktopScreenshot [[-Path] <string>] [-Index <Int32>] [-DeviceId <string>] [-DeviceName <string>] [-PrimaryOnly] [-Left <Int32>] [-Top <Int32>] [-Width <Int32>] [-Height <Int32>] [<CommonParameters>]
```

## DESCRIPTION
Captures a screenshot of the desktop.

Captures the current desktop image. When a path is provided the image is saved as PNG; otherwise a Bitmap object is returned. The screenshot can target a specific monitor or any region.

## EXAMPLES

### EXAMPLE 1
```powershell
Invoke-DesktopScreenshot -Path 'C:\Path'
```


## PARAMETERS

### -DeviceId
Identifier of the monitor to capture.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: MonitorID
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DeviceName
Name of the monitor to capture.

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

### -Height
Height of the region to capture.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: Bottom
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Index
Index of the monitor to capture. Defaults to the entire virtual screen.

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

### -Left
Left coordinate of the region to capture.

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

### -Path
Optional path to save the screenshot as a PNG file.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PrimaryOnly
Capture the primary monitor only.

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

### -Top
Top coordinate of the region to capture.

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

### -Width
Width of the region to capture.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: Right
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
