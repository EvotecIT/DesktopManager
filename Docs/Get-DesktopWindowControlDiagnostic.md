---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Get-DesktopWindowControlDiagnostic
## SYNOPSIS
Gets shared diagnostics for desktop window control discovery.

## SYNTAX
### ByName
```powershell
Get-DesktopWindowControlDiagnostic [-Name] <string> [-ClassName <string>] [-TextPattern <string>] [-ValuePattern <string>] [-Id <Int32>] [-AutomationId <string>] [-ControlType <string>] [-FrameworkId <string>] [-IsEnabled <Boolean>] [-IsKeyboardFocusable <Boolean>] [-UiAutomation] [-IncludeUiAutomation] [-EnsureForeground] [-ControlTargetName <string>] [-SampleLimit <int>] [-ActionProbe] [<CommonParameters>]
```

### ActiveWindow
```powershell
Get-DesktopWindowControlDiagnostic -ActiveWindow [-ClassName <string>] [-TextPattern <string>] [-ValuePattern <string>] [-Id <Int32>] [-AutomationId <string>] [-ControlType <string>] [-FrameworkId <string>] [-IsEnabled <Boolean>] [-IsKeyboardFocusable <Boolean>] [-UiAutomation] [-IncludeUiAutomation] [-EnsureForeground] [-ControlTargetName <string>] [-SampleLimit <int>] [-ActionProbe] [<CommonParameters>]
```

## DESCRIPTION
Collects Win32 and UI Automation discovery diagnostics for a desktop window.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-DesktopWindowControlDiagnostic -Name "*Codex*" -UiAutomation -EnsureForeground
```


### EXAMPLE 2
```powershell
Get-DesktopWindowControlDiagnostic -Name "*Codex*" -ControlTargetName "codex-sidebar-toggle" -ActionProbe
```


## PARAMETERS

### -ActionProbe
Include a read-only UI Automation action-resolution probe for the first matched UIA control.

```yaml
Type: SwitchParameter
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ActiveWindow
Use the current foreground window instead of matching by name.

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
Filter UI Automation controls by automation identifier. Supports wildcards.

```yaml
Type: String
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ClassName
Filter controls by class name. Supports wildcards.

```yaml
Type: String
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ControlTargetName
Use a saved control target definition instead of ad-hoc control selector parameters.

```yaml
Type: String
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ControlType
Filter UI Automation controls by control type. Supports wildcards.

```yaml
Type: String
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -EnsureForeground
Bring the target window to the foreground before UI Automation discovery.

```yaml
Type: SwitchParameter
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -FrameworkId
Filter UI Automation controls by framework identifier. Supports wildcards.

```yaml
Type: String
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Id
Filter controls by control identifier.

```yaml
Type: Int32
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -IncludeUiAutomation
Combine Win32 and UI Automation control discovery.

```yaml
Type: SwitchParameter
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -IsEnabled
Filter by whether the control is enabled.

```yaml
Type: Boolean
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -IsKeyboardFocusable
Filter by whether the control can receive keyboard focus.

```yaml
Type: Boolean
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
Title of the window to match. Supports wildcards.

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

### -SampleLimit
Maximum number of sample controls to include in each diagnostic result.

```yaml
Type: Int32
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TextPattern
Filter controls by visible text. Supports wildcards.

```yaml
Type: String
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -UiAutomation
Use UI Automation for control discovery.

```yaml
Type: SwitchParameter
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ValuePattern
Filter controls by their value. Supports wildcards.

```yaml
Type: String
Parameter Sets: ByName, ActiveWindow
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
