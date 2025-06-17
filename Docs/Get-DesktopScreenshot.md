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
Get-DesktopScreenshot [-Path <String>] [<CommonParameters>]
```

## DESCRIPTION
The **Get-DesktopScreenshot** cmdlet captures the current desktop image. When a path is specified, the screenshot is saved as a PNG file. Otherwise a System.Drawing.Bitmap object is returned.

## PARAMETERS
### -Path
Optional path where the screenshot will be saved.

```yaml
Type: String
Required: False
Position: 0
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

## RELATED LINKS
None
