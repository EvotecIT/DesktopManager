---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Start-DesktopProcessAndWait
## SYNOPSIS
Starts a desktop process and waits for a correlated final window.

## SYNTAX
### __AllParameterSets
```powershell
Start-DesktopProcessAndWait [-Path] <string> [-ArgumentList <string>] [-WorkingDirectory <string>] [-WaitForInputIdleMilliseconds <int>] [-LaunchWaitForWindowMilliseconds <int>] [-LaunchWaitForWindowIntervalMilliseconds <int>] [-LaunchWindowTitle <string>] [-LaunchWindowClassName <string>] [-WindowTitle <string>] [-WindowClassName <string>] [-IncludeHidden] [-IncludeEmptyTitles] [-All] [-FollowProcessFamily] [-TimeoutMilliseconds <int>] [-IntervalMilliseconds <int>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Starts a desktop process and waits for a correlated final window.

Launches a desktop process, correlates the initial launch window when possible, and then waits for a final matching window using DesktopManager core workflow logic.

## EXAMPLES

### EXAMPLE 1
```powershell
Start-DesktopProcessAndWait -Path notepad.exe -TimeoutMilliseconds 5000
```


## PARAMETERS

### -All
Return all matching windows instead of the first match.

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

### -ArgumentList
Optional argument string passed to the launched process.

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

### -FollowProcessFamily
Allow the final wait to follow the launched app's same-name process family when no resolved process window is available.

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

### -IncludeEmptyTitles
Include windows with empty titles during the final wait.

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

### -IncludeHidden
Include hidden windows during the final wait.

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

### -IntervalMilliseconds
Polling interval used during the final wait in milliseconds.

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

### -LaunchWaitForWindowIntervalMilliseconds
Polling interval while correlating the launch-time window.

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

### -LaunchWaitForWindowMilliseconds
Optional launch-time window correlation wait in milliseconds.

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

### -LaunchWindowClassName
Optional launch-time window class filter.

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

### -LaunchWindowTitle
Optional launch-time window title filter.

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

### -Path
Executable path or shell command to launch.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -TimeoutMilliseconds
Maximum final wait time in milliseconds.

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

### -WaitForInputIdleMilliseconds
Optional time to wait for UI input idle in milliseconds.

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

### -WindowClassName
Optional final window class filter.

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

### -WindowTitle
Optional final window title filter.

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

### -WorkingDirectory
Optional working directory for the launched process.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `System.Object`

## RELATED LINKS

- None
