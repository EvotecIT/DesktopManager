---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Invoke-DesktopWindowScreenshot
## SYNOPSIS
Takes a screenshot of a window or control.

## SYNTAX
### Window
```powershell
Invoke-DesktopWindowScreenshot [-Window] <WindowInfo> [[-Path] <string>] [<CommonParameters>]
```

### Control
```powershell
Invoke-DesktopWindowScreenshot [-Control] <WindowControlInfo> [[-Path] <string>] [<CommonParameters>]
```

## DESCRIPTION
Captures a screenshot of a window or control.

## EXAMPLES

### EXAMPLE 1
```powershell
$wnd = Get-DesktopWindow -Name "*Notepad*" | Select-Object -First 1
            Invoke-DesktopWindowScreenshot -Window $wnd -Path "window.png"
```


## PARAMETERS

### -Control
Control to capture.

```yaml
Type: WindowControlInfo
Parameter Sets: Control
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Path
Optional path to save the PNG image.

```yaml
Type: String
Parameter Sets: Window, Control
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Window
Window to capture.

```yaml
Type: WindowInfo
Parameter Sets: Window
Aliases: None
Possible values:

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
