---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Get-DesktopWindow
## SYNOPSIS
Gets information about desktop windows.

## SYNTAX
### __AllParameterSets
```powershell
Get-DesktopWindow [[-Name] <string>] [-ProcessName <string>] [-ClassName <string>] [-Regex <regex>] [-ProcessId <int>] [-ActiveWindow] [-IncludeHidden] [-IncludeCloaked <bool>] [-IncludeOwned <bool>] [-IsVisible <Boolean>] [-State <WindowState>] [-IsTopMost <Boolean>] [-ZOrderMin <Int32>] [-ZOrderMax <Int32>] [<CommonParameters>]
```

## DESCRIPTION
Gets information about desktop windows.

Retrieves information about desktop windows. Supports filters for title, process, class, visibility, state, topmost, and Z-order.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-DesktopWindow
```

Get all visible windows

### EXAMPLE 2
```powershell
Get-DesktopWindow -Name "*Notepad*"
```

Get windows with "Notepad" in the title

## PARAMETERS

### -ActiveWindow
Return only the current foreground window.

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

### -ClassName
Filter windows by window class name. Supports wildcards.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -IncludeCloaked
Include DWM-cloaked windows in the results.

```yaml
Type: Boolean
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -IncludeHidden
Include hidden windows in the results.

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

### -IncludeOwned
Include owned windows in the results.

```yaml
Type: Boolean
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -IsTopMost
Filter windows by topmost state.

```yaml
Type: Boolean
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -IsVisible
Filter windows by visibility. Use $true for visible or $false for hidden.

```yaml
Type: Boolean
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
Filter windows by title. Supports wildcards.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProcessId
Filter windows by process ID.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProcessName
Filter windows by process name. Supports wildcards.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Regex
Filter window titles using a regular expression.

```yaml
Type: Regex
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -State
Filter windows by state (Normal, Minimize, Maximize).

```yaml
Type: WindowState
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Normal, Minimize, Maximize, Close

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ZOrderMax
Maximum Z-order index (0 is top-most).

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ZOrderMin
Minimum Z-order index (0 is top-most).

```yaml
Type: Int32
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

- `None`

## OUTPUTS

- `None`

## RELATED LINKS

- None
