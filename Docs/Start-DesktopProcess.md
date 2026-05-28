---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Start-DesktopProcess
## SYNOPSIS
Starts a desktop application or process.

## SYNTAX
### __AllParameterSets
```powershell
Start-DesktopProcess [-Path] <string> [-ArgumentList <string>] [-WorkingDirectory <string>] [-WaitForInputIdleMilliseconds <int>] [-WaitForWindowMilliseconds <int>] [-WaitForWindowIntervalMilliseconds <int>] [-WindowTitle <string>] [-WindowClassName <string>] [-RequireWindow] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Starts a desktop application or process.

Launches a desktop process and returns the launch metadata, including the launched process identifier and any correlated main window.

## EXAMPLES

### EXAMPLE 1
```powershell
Start-DesktopProcess -Path notepad.exe
```


### EXAMPLE 2
```powershell
Start-DesktopProcess -Path notepad.exe -RequireWindow -WaitForWindowMilliseconds 3000
```


## PARAMETERS

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

### -RequireWindow
Require a user-facing launched window before returning.

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

### -WaitForWindowIntervalMilliseconds
Polling interval while waiting for a launched window.

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

### -WaitForWindowMilliseconds
Optional time to wait for a launched window in milliseconds.

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
Optional launched-window class filter.

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
Optional launched-window title filter.

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
