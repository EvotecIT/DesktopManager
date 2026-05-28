---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Wait-DesktopWindowText
## SYNOPSIS
Waits until a desktop window exposes the requested observed text.

## SYNTAX
### ByName
```powershell
Wait-DesktopWindowText [-Name] <string> -ExpectedText <string> [-TimeoutMs <int>] [-IntervalMs <int>] [-MaxObservedTextLength <int>] [-RetryCount <int>] [-RetryDelayMilliseconds <int>] [<CommonParameters>]
```

### ByHandle
```powershell
Wait-DesktopWindowText -Handle <string> -ExpectedText <string> [-TimeoutMs <int>] [-IntervalMs <int>] [-MaxObservedTextLength <int>] [-RetryCount <int>] [-RetryDelayMilliseconds <int>] [<CommonParameters>]
```

### ActiveWindow
```powershell
Wait-DesktopWindowText -ActiveWindow -ExpectedText <string> [-TimeoutMs <int>] [-IntervalMs <int>] [-MaxObservedTextLength <int>] [-RetryCount <int>] [-RetryDelayMilliseconds <int>] [<CommonParameters>]
```

## DESCRIPTION
Waits until a desktop window exposes the requested observed text.

Polls the DesktopManager text observation pipeline until the selected window exposes text containing the requested value.

## EXAMPLES

### EXAMPLE 1
```powershell
Wait-DesktopWindowText -ActiveWindow -ExpectedText "Ready"
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

### -ExpectedText
Text to wait for.

```yaml
Type: String
Parameter Sets: ByName, ByHandle, ActiveWindow
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

### -MaxObservedTextLength
Maximum number of characters to return in the observed value.

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

### -RetryCount
Number of observation retries within each polling cycle.

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

### -RetryDelayMilliseconds
Delay in milliseconds between observation retries.

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
