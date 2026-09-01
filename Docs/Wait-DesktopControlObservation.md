---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Wait-DesktopControlObservation
## SYNOPSIS
Waits for semantic desktop-control state.

## SYNTAX
### ByName
```powershell
Wait-DesktopControlObservation [-Name] <string> [-ClassName <string>] [-TextPattern <string>] [-ValuePattern <string>] [-AutomationId <string>] [-ControlType <string>] [-FrameworkId <string>] [-ExpectedText <string>] [-IgnoreCase] [-IsTextComplete <Boolean>] [-IsTextTruncated <Boolean>] [-IsEnabled <Boolean>] [-IsFocused <Boolean>] [-IsChecked <Boolean>] [-IsSelected <Boolean>] [-ExpandCollapseState <string>] [-MinimumRangeValue <Double>] [-MaximumRangeValue <Double>] [-MaxTextLength <int>] [-TimeoutMs <int>] [-IntervalMs <int>] [<CommonParameters>]
```

### ByHandle
```powershell
Wait-DesktopControlObservation -Handle <string> [-ClassName <string>] [-TextPattern <string>] [-ValuePattern <string>] [-AutomationId <string>] [-ControlType <string>] [-FrameworkId <string>] [-ExpectedText <string>] [-IgnoreCase] [-IsTextComplete <Boolean>] [-IsTextTruncated <Boolean>] [-IsEnabled <Boolean>] [-IsFocused <Boolean>] [-IsChecked <Boolean>] [-IsSelected <Boolean>] [-ExpandCollapseState <string>] [-MinimumRangeValue <Double>] [-MaximumRangeValue <Double>] [-MaxTextLength <int>] [-TimeoutMs <int>] [-IntervalMs <int>] [<CommonParameters>]
```

### ActiveWindow
```powershell
Wait-DesktopControlObservation -ActiveWindow [-ClassName <string>] [-TextPattern <string>] [-ValuePattern <string>] [-AutomationId <string>] [-ControlType <string>] [-FrameworkId <string>] [-ExpectedText <string>] [-IgnoreCase] [-IsTextComplete <Boolean>] [-IsTextTruncated <Boolean>] [-IsEnabled <Boolean>] [-IsFocused <Boolean>] [-IsChecked <Boolean>] [-IsSelected <Boolean>] [-ExpandCollapseState <string>] [-MinimumRangeValue <Double>] [-MaximumRangeValue <Double>] [-MaxTextLength <int>] [-TimeoutMs <int>] [-IntervalMs <int>] [<CommonParameters>]
```

## DESCRIPTION
Waits on UI Automation events with bounded polling fallback until a matching control reaches the requested state.

## EXAMPLES

### EXAMPLE 1
```powershell
Wait-DesktopControlObservation -ActiveWindow -ControlType Document -ExpectedText 'Ready' -TimeoutMs 10000
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

### -AutomationId
UI Automation identifier pattern.

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

### -ClassName
Control class pattern.

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

### -ControlType
UI Automation control type pattern.

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

### -ExpandCollapseState
Required expand or collapse state.

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

### -ExpectedText
Literal text required in the observation.

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

### -FrameworkId
UI framework identifier pattern.

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
Window handle.

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

### -IgnoreCase
Ignore case while matching expected text.

```yaml
Type: SwitchParameter
Parameter Sets: ByName, ByHandle, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -IntervalMs
Maximum polling fallback interval.

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

### -IsChecked
Required checked state.

```yaml
Type: Boolean
Parameter Sets: ByName, ByHandle, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -IsEnabled
Required enabled state.

```yaml
Type: Boolean
Parameter Sets: ByName, ByHandle, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -IsFocused
Required focused state.

```yaml
Type: Boolean
Parameter Sets: ByName, ByHandle, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -IsSelected
Required selected state.

```yaml
Type: Boolean
Parameter Sets: ByName, ByHandle, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -IsTextComplete
Required complete-text state.

```yaml
Type: Boolean
Parameter Sets: ByName, ByHandle, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -IsTextTruncated
Required text-truncation state.

```yaml
Type: Boolean
Parameter Sets: ByName, ByHandle, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaximumRangeValue
Maximum acceptable numeric range value.

```yaml
Type: Double
Parameter Sets: ByName, ByHandle, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxTextLength
Maximum observed text length.

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

### -MinimumRangeValue
Minimum acceptable numeric range value.

```yaml
Type: Double
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
Window title pattern.

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

### -TextPattern
Control text pattern.

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
Accept wildcard characters: False
```

### -ValuePattern
Control value pattern.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `None`

## RELATED LINKS

- None
