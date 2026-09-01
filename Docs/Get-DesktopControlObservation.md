---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Get-DesktopControlObservation
## SYNOPSIS
Gets provider-neutral semantic observations for desktop controls.

## SYNTAX
### ByName
```powershell
Get-DesktopControlObservation [-Name] <string> [-ClassName <string>] [-TextPattern <string>] [-ValuePattern <string>] [-Id <Int32>] [-AutomationId <string>] [-ControlType <string>] [-FrameworkId <string>] [-ExpectedText <string>] [-IgnoreCase] [-MaxTextLength <int>] [-IncludeTextRanges] [-RealizeVirtualizedItem] [-EnsureForeground] [-All] [-AllWindows] [<CommonParameters>]
```

### ByHandle
```powershell
Get-DesktopControlObservation -Handle <string> [-ClassName <string>] [-TextPattern <string>] [-ValuePattern <string>] [-Id <Int32>] [-AutomationId <string>] [-ControlType <string>] [-FrameworkId <string>] [-ExpectedText <string>] [-IgnoreCase] [-MaxTextLength <int>] [-IncludeTextRanges] [-RealizeVirtualizedItem] [-EnsureForeground] [-All] [-AllWindows] [<CommonParameters>]
```

### ActiveWindow
```powershell
Get-DesktopControlObservation -ActiveWindow [-ClassName <string>] [-TextPattern <string>] [-ValuePattern <string>] [-Id <Int32>] [-AutomationId <string>] [-ControlType <string>] [-FrameworkId <string>] [-ExpectedText <string>] [-IgnoreCase] [-MaxTextLength <int>] [-IncludeTextRanges] [-RealizeVirtualizedItem] [-EnsureForeground] [-All] [-AllWindows] [<CommonParameters>]
```

## DESCRIPTION
Gets identity, capabilities, text ranges, and semantic state from matching Win32 and UI Automation controls.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-DesktopControlObservation -ActiveWindow -ControlType Document -All
```


### EXAMPLE 2
```powershell
Get-DesktopControlObservation -Name '*Outlook*' -ControlType Document -ExpectedText 'project' -MaxTextLength 65536
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

### -All
Return every matching control.

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

### -AllWindows
Inspect every matching window.

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

### -EnsureForeground
Prepare the window before UI Automation discovery.

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

### -ExpectedText
Optional literal text to find in complete provider text.

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
Window handle in decimal or hexadecimal form.

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

### -Id
Native control identifier.

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

### -IgnoreCase
Ignore case while finding expected text.

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

### -IncludeTextRanges
Include selected ranges and caret context.

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

### -RealizeVirtualizedItem
Realize a virtualized item before observation.

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
