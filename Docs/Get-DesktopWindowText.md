---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Get-DesktopWindowText
## SYNOPSIS
Observes the best available text for a desktop window.

## SYNTAX
### ByName
```powershell
Get-DesktopWindowText [-Name] <string> [-ExpectedText <string>] [-MaxObservedTextLength <int>] [-RetryCount <int>] [-RetryDelayMilliseconds <int>] [<CommonParameters>]
```

### ByHandle
```powershell
Get-DesktopWindowText -Handle <string> [-ExpectedText <string>] [-MaxObservedTextLength <int>] [-RetryCount <int>] [-RetryDelayMilliseconds <int>] [<CommonParameters>]
```

### ActiveWindow
```powershell
Get-DesktopWindowText -ActiveWindow [-ExpectedText <string>] [-MaxObservedTextLength <int>] [-RetryCount <int>] [-RetryDelayMilliseconds <int>] [<CommonParameters>]
```

## DESCRIPTION
Observes the best available text for a desktop window.

Returns a text observation from the best available source for a selected window, such as the focused control value, control text, or window title.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-DesktopWindowText -ActiveWindow
```


### EXAMPLE 2
```powershell
Get-DesktopWindowText -Name "*Notepad*" -ExpectedText "hello"
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
Accept wildcard characters: False
```

### -ExpectedText
Optional text that should be preferred when present.

```yaml
Type: String
Parameter Sets: ByName, ByHandle, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
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
Accept wildcard characters: False
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
Accept wildcard characters: False
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
Accept wildcard characters: False
```

### -RetryCount
Number of observation retries.

```yaml
Type: Int32
Parameter Sets: ByName, ByHandle, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
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
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `None`

## RELATED LINKS

- None
