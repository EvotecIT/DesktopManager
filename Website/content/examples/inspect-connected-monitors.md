---
title: "Inspect connected monitors"
description: "Use DesktopManager to read monitor information from the current Windows session."
layout: docs
---

This pattern is useful before changing display layout, brightness, or window placement.

It is adapted from `Examples/GetDesktopMonitors.ps1`.

## Example

```powershell
Import-Module DesktopManager

$allMonitors = Get-DesktopMonitor
$allMonitors | Format-Table

$connected = Get-DesktopMonitor -ConnectedOnly
$connected | Format-Table

$primary = Get-DesktopMonitor -PrimaryOnly
$primary | Format-List *
```

## What this demonstrates

- reading monitor state before making changes
- narrowing to connected or primary monitors
- reviewing monitor details in table or list form

## Source

- [GetDesktopMonitors.ps1](https://github.com/EvotecIT/DesktopManager/blob/master/Examples/GetDesktopMonitors.ps1)
