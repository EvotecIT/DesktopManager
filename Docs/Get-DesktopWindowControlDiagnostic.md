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
Get-DesktopWindowControlDiagnostic [-Name] <string> [-ClassName <string>] [-TextPattern <string>] [-ValuePattern <string>] [-Id <int>] [-AutomationId <string>] [-ControlType <string>] [-FrameworkId <string>] [-IsEnabled <bool>] [-IsKeyboardFocusable <bool>] [-UiAutomation] [-IncludeUiAutomation] [-EnsureForeground] [-ControlTargetName <string>] [-SampleLimit <int>] [-ActionProbe] [<CommonParameters>]
```

### ActiveWindow
```powershell
Get-DesktopWindowControlDiagnostic -ActiveWindow [-ClassName <string>] [-TextPattern <string>] [-ValuePattern <string>] [-Id <int>] [-AutomationId <string>] [-ControlType <string>] [-FrameworkId <string>] [-IsEnabled <bool>] [-IsKeyboardFocusable <bool>] [-UiAutomation] [-IncludeUiAutomation] [-EnsureForeground] [-ControlTargetName <string>] [-SampleLimit <int>] [-ActionProbe] [<CommonParameters>]
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
Accept wildcard characters: True
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
Accept wildcard characters: True
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
Accept wildcard characters: True
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
Accept wildcard characters: True
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
Accept wildcard characters: True
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
Accept wildcard characters: True
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
Accept wildcard characters: True
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
Accept wildcard characters: True
```

### -Id
Filter controls by control identifier.

```yaml
Type: Nullable`1
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
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
Accept wildcard characters: True
```

### -IsEnabled
Filter by whether the control is enabled.

```yaml
Type: Nullable`1
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -IsKeyboardFocusable
Filter by whether the control can receive keyboard focus.

```yaml
Type: Nullable`1
Parameter Sets: ByName, ActiveWindow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
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
Accept wildcard characters: True
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
Accept wildcard characters: True
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
Accept wildcard characters: True
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
Accept wildcard characters: True
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
