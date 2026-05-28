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
Get-DesktopWindow [[-Name] <string>] [-ProcessName <string>] [-ClassName <string>] [-Regex <regex>] [-ProcessId <int>] [-ActiveWindow] [-IncludeHidden] [-IncludeCloaked <bool>] [-IncludeOwned <bool>] [-IsVisible <bool>] [-State <WindowState>] [-IsTopMost <bool>] [-ZOrderMin <int>] [-ZOrderMax <int>] [<CommonParameters>]
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
Accept wildcard characters: True
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
Accept wildcard characters: True
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
Accept wildcard characters: True
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
Accept wildcard characters: True
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
Accept wildcard characters: True
```

### -IsTopMost
Filter windows by topmost state.

```yaml
Type: Nullable`1
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -IsVisible
Filter windows by visibility. Use $true for visible or $false for hidden.

```yaml
Type: Nullable`1
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
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
Accept wildcard characters: True
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
Accept wildcard characters: True
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
Accept wildcard characters: True
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
Accept wildcard characters: True
```

### -State
Filter windows by state (Normal, Minimize, Maximize).

```yaml
Type: Nullable`1
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -ZOrderMax
Maximum Z-order index (0 is top-most).

```yaml
Type: Nullable`1
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -ZOrderMin
Minimum Z-order index (0 is top-most).

```yaml
Type: Nullable`1
Parameter Sets: __AllParameterSets
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
