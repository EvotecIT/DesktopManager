---
title: "Capture desktop screenshots"
description: "Use DesktopManager to capture desktop screenshots into a script-local output folder."
layout: docs
---

This pattern is useful for troubleshooting display state or collecting visual evidence during automation.

It is adapted from `Examples/GetDesktopScreenshot.ps1`.

## Example

```powershell
Import-Module DesktopManager

$outputPath = Join-Path $PSScriptRoot 'Output'
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null

Invoke-DesktopScreenshot -Path (Join-Path $outputPath 'DesktopScreenshot.png')
Invoke-DesktopScreenshot -Path (Join-Path $outputPath 'PrimaryMonitor.png') -Index 0
Invoke-DesktopScreenshot -Path (Join-Path $outputPath 'Region.png') -Left 100 -Top 100 -Width 800 -Height 600
```

## What this demonstrates

- capturing full desktop output
- targeting a specific monitor by index
- capturing a defined screen region

## Source

- [GetDesktopScreenshot.ps1](https://github.com/EvotecIT/DesktopManager/blob/v2-speedygonzales/Examples/GetDesktopScreenshot.ps1)
