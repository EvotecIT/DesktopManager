---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Get-DesktopFocusedControl
## SYNOPSIS
Gets the focused control for a desktop window.

## SYNTAX
### ByName
```powershell
Get-DesktopFocusedControl [-Name] <string> [-MaxObservedTextLength <int>] [-ExpectedText <string>] [<CommonParameters>]
```

### ByHandle
```powershell
Get-DesktopFocusedControl -Handle <string> [-MaxObservedTextLength <int>] [-ExpectedText <string>] [<CommonParameters>]
```

### ActiveWindow
```powershell
Get-DesktopFocusedControl -ActiveWindow [-MaxObservedTextLength <int>] [-ExpectedText <string>] [<CommonParameters>]
```

## DESCRIPTION
Gets the focused control for a desktop window.

Returns focused-control metadata and a bounded plain-text value for a specific window selected by title, handle, or the current foreground window. Document editors that expose UI Automation TextPattern are read directly; password controls are never read.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-DesktopFocusedControl -ActiveWindow
```


### EXAMPLE 2
```powershell
Get-DesktopFocusedControl -Handle 0x123456
```


### EXAMPLE 3
```powershell
Get-DesktopFocusedControl -Name '*Outlook*' -MaxObservedTextLength 4096 -ExpectedText 'matthew'
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
Optional text to search for across the complete UI Automation document range even when the returned value is truncated.

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
Maximum number of focused-control value characters to return. The default is 2048.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `None`

## RELATED LINKS

- None
