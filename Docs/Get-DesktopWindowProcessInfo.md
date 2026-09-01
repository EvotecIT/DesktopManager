---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Get-DesktopWindowProcessInfo
## SYNOPSIS
Gets process information for a desktop window.

## SYNTAX
### __AllParameterSets
```powershell
Get-DesktopWindowProcessInfo [-InputObject] <WindowInfo> [-Owner] [<CommonParameters>]
```

## DESCRIPTION
Gets process information for a desktop window.

Retrieves process metadata for a window, including process ID, thread ID, name, path, and elevation.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-DesktopWindow -Name "*Notepad*" | Get-DesktopWindowProcessInfo
```

Get process info for a window

### EXAMPLE 2
```powershell
Get-DesktopWindow -Name "*Notepad*" | Get-DesktopWindowProcessInfo -Owner
```

Get owner process info for a window

## PARAMETERS

### -InputObject
Window to query.

```yaml
Type: WindowInfo
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Owner
Return the owner window's process info instead of the window's own process.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `DesktopManager.WindowInfo`

## OUTPUTS

- `None`

## RELATED LINKS

- None
