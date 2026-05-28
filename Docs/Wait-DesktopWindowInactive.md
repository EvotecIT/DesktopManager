---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Wait-DesktopWindowInactive
## SYNOPSIS
Waits for a desktop window to become inactive.

## SYNTAX
### ByName
```powershell
Wait-DesktopWindowInactive [-Name] <string> [-TimeoutMs <int>] [-IntervalMs <int>] [<CommonParameters>]
```

### ByHandle
```powershell
Wait-DesktopWindowInactive -Handle <string> [-TimeoutMs <int>] [-IntervalMs <int>] [<CommonParameters>]
```

### ActiveWindow
```powershell
Wait-DesktopWindowInactive -ActiveWindow [-TimeoutMs <int>] [-IntervalMs <int>] [<CommonParameters>]
```

## DESCRIPTION
Waits for a desktop window to become inactive.

Tracks the selected window until it no longer owns the foreground focus.

## EXAMPLES

### EXAMPLE 1
```powershell
Wait-DesktopWindowInactive -ActiveWindow -TimeoutMs 5000
```


## PARAMETERS

### -ActiveWindow
Use the current foreground window.

```yaml
Type: SwitchParameter
Parameter Sets: ActiveWindow
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Handle
Window handle in decimal or hexadecimal format.

```yaml
Type: String
Parameter Sets: ByHandle
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -IntervalMs
Polling interval in milliseconds.

```yaml
Type: Int32
Parameter Sets: ByName, ByHandle, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Name
Title of the window to inspect. Supports wildcards.

```yaml
Type: String
Parameter Sets: ByName
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -TimeoutMs
Timeout in milliseconds. Zero waits indefinitely.

```yaml
Type: Int32
Parameter Sets: ByName, ByHandle, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
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
