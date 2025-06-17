---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version:
schema: 2.0.0
---

# Get-DesktopScreenshot

## SYNOPSIS
Capture a screenshot of the entire desktop.

## SYNTAX
```powershell
Get-DesktopScreenshot [-Path <String>] [-Monitor <Int32>] [-Region <Rectangle>] [<CommonParameters>]
```

## DESCRIPTION
The **Get-DesktopScreenshot** cmdlet captures the current desktop image. When a path is specified, the screenshot is saved as a PNG file. Otherwise a System.Drawing.Bitmap object is returned. You can capture a specific monitor or any region on the desktop.

## PARAMETERS
### -Path
Optional path where the screenshot will be saved.

```yaml
Type: String
Required: False
Position: 0
```

### -Monitor
Index of the monitor to capture. If omitted the entire virtual screen is captured.

```yaml
Type: Int32
Required: False
Position: Named
```

### -Region
Rectangle representing the region to capture in screen coordinates.

```yaml
Type: Rectangle
Required: False
Position: Named
```

## EXAMPLES
### Example 1
```powershell
Get-DesktopScreenshot -Path C:\temp\screen.png
```
Captures the desktop and saves the image to *C:\temp\screen.png*.

### Example 2
```powershell
Get-DesktopScreenshot | Out-Null
```
Captures the desktop and returns a Bitmap object.

### Example 3
```powershell
Get-DesktopScreenshot -Monitor 1 -Path C:\temp\monitor1.png
```
Captures monitor with index 1 and saves it.

### Example 4
```powershell
$rect = New-Object System.Drawing.Rectangle 0,0,200,200
Get-DesktopScreenshot -Region $rect -Path C:\temp\region.png
```
Captures the specified region of the desktop.

## RELATED LINKS
None
