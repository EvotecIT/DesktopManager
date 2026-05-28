---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Wait-DesktopFocusedControl
## SYNOPSIS
Waits until a desktop window exposes a focused control.

## SYNTAX
### ByName
```powershell
Wait-DesktopFocusedControl [-Name] <string> [-TimeoutMs <int>] [-IntervalMs <int>] [<CommonParameters>]
```

### ByHandle
```powershell
Wait-DesktopFocusedControl -Handle <string> [-TimeoutMs <int>] [-IntervalMs <int>] [<CommonParameters>]
```

### ActiveWindow
```powershell
Wait-DesktopFocusedControl -ActiveWindow [-TimeoutMs <int>] [-IntervalMs <int>] [<CommonParameters>]
```

## DESCRIPTION
Waits until a desktop window exposes a focused control.

Polls DesktopManager focused-control observation until the selected window exposes a focused child control.

## EXAMPLES

### EXAMPLE 1
```powershell
Wait-DesktopFocusedControl -ActiveWindow -TimeoutMs 5000
```


### EXAMPLE 2
```powershell
Wait-DesktopFocusedControl -Handle 0x123456 -TimeoutMs 5000
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
