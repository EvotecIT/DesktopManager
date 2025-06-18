---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version:
schema: 2.0.0
---

# Invoke-DesktopScreenshot

## SYNOPSIS
Capture a screenshot of the desktop, a specific monitor or a selected region.

## SYNTAX
```powershell
Invoke-DesktopScreenshot [-Path <String>] [-Index <Int32>] [-DeviceId <String>] [-DeviceName <String>] [-PrimaryOnly] [-Left <Int32>] [-Top <Int32>] [-Width <Int32>] [-Height <Int32>] [<CommonParameters>]
```

## DESCRIPTION
The **Invoke-DesktopScreenshot** cmdlet captures the current desktop image. When a path is specified, the screenshot is saved as a PNG file. Otherwise a `System.Drawing.Bitmap` object is returned. You can target a single monitor by index, device ID or device name, limit capture to the primary monitor, or capture an arbitrary region of the desktop.

## PARAMETERS
### -Path
Optional path where the screenshot will be saved.

```yaml
Type: String
Required: False
Position: 0
```

### -Index
Index of the monitor to capture.

```yaml
Type: Int32
Required: False
Position: Named
```

### -DeviceId
Device identifier of the monitor to capture.

```yaml
Type: String
Required: False
Position: Named
```

### -DeviceName
Device name of the monitor to capture.

```yaml
Type: String
Required: False
Position: Named
```

### -PrimaryOnly
Capture only the primary monitor.

```yaml
Type: SwitchParameter
Required: False
Position: Named
```

### -Left
Left coordinate of the region to capture.

```yaml
Type: Int32
Required: False
Position: Named
```

### -Top
Top coordinate of the region to capture.

```yaml
Type: Int32
Required: False
Position: Named
```

### -Width
Width of the region to capture.

```yaml
Type: Int32
Required: False
Position: Named
```

### -Height
Height of the region to capture.

```yaml
Type: Int32
Required: False
Position: Named
```

## EXAMPLES
### Example 1
```powershell
Invoke-DesktopScreenshot -Path C:\temp\screen.png
```
Captures all monitors and saves the image to *C:\temp\screen.png*.

### Example 2
```powershell
Invoke-DesktopScreenshot | Out-Null
```
Captures the desktop and returns a Bitmap object.

### Example 3
```powershell
Invoke-DesktopScreenshot -Index 1 -Path C:\temp\monitor1.png
```
Captures monitor with index 1 and saves it.

### Example 4
```powershell
Invoke-DesktopScreenshot -Left 0 -Top 0 -Width 200 -Height 200 -Path C:\temp\region.png
```
Captures the specified region of the desktop.

## RELATED LINKS
None
