---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Wait-DesktopWindowControl
## SYNOPSIS
Waits for a desktop window control to appear.

## SYNTAX
### ByName
```powershell
Wait-DesktopWindowControl [-Name] <string> [-ClassName <string>] [-TextPattern <string>] [-ValuePattern <string>] [-Id <int>] [-AutomationId <string>] [-ControlType <string>] [-FrameworkId <string>] [-ControlTargetName <string>] [-Enabled] [-Disabled] [-Focusable] [-NotFocusable] [-BackgroundClick] [-BackgroundText] [-BackgroundKeys] [-ForegroundFallback] [-UiAutomation] [-IncludeUiAutomation] [-EnsureForeground] [-TimeoutMs <int>] [-IntervalMs <int>] [-All] [<CommonParameters>]
```

### ActiveWindow
```powershell
Wait-DesktopWindowControl -ActiveWindow [-ClassName <string>] [-TextPattern <string>] [-ValuePattern <string>] [-Id <int>] [-AutomationId <string>] [-ControlType <string>] [-FrameworkId <string>] [-ControlTargetName <string>] [-Enabled] [-Disabled] [-Focusable] [-NotFocusable] [-BackgroundClick] [-BackgroundText] [-BackgroundKeys] [-ForegroundFallback] [-UiAutomation] [-IncludeUiAutomation] [-EnsureForeground] [-TimeoutMs <int>] [-IntervalMs <int>] [-All] [<CommonParameters>]
```

## DESCRIPTION
Waits for a matching Win32 or UI Automation control to appear.

## EXAMPLES

### EXAMPLE 1
```powershell
Wait-DesktopWindowControl -Name "*Notepad*" -ClassName "RichEditD2DPT" -TimeoutMs 5000
```


### EXAMPLE 2
```powershell
Wait-DesktopWindowControl -ActiveWindow -UiAutomation -ControlType Button -TextPattern "Show sidebar" -TimeoutMs 5000
```


## PARAMETERS

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

### -All
Return all matching controls instead of only the first one.

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

### -BackgroundClick
Require controls that support background-safe click or invoke actions.

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

### -BackgroundKeys
Require controls that support background-safe key delivery.

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

### -BackgroundText
Require controls that support background-safe text updates.

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
Optional saved control target name to resolve instead of ad-hoc selectors.

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

### -Disabled
Require the control to be disabled.

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

### -Enabled
Require the control to be enabled.

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

### -Focusable
Require the control to accept keyboard focus.

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

### -ForegroundFallback
Require controls that support explicit foreground input fallback.

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

### -IntervalMs
Polling interval in milliseconds.

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

### -NotFocusable
Require the control to not accept keyboard focus.

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

### -TimeoutMs
Timeout in milliseconds. Zero waits indefinitely.

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
Filter controls by current value. Supports wildcards.

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
