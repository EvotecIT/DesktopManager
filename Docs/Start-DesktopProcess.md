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
Start-DesktopProcess [-Path] <string> [-ArgumentList <string>] [-WorkingDirectory <string>] [-WaitForInputIdleMilliseconds <Int32>] [-WaitForWindowMilliseconds <Int32>] [-WaitForWindowIntervalMilliseconds <Int32>] [-WindowTitle <string>] [-WindowClassName <string>] [-RequireWindow] [-WhatIf] [-Confirm] [<CommonParameters>]
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
Accept wildcard characters: False
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
Accept wildcard characters: False
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
Accept wildcard characters: False
```

### -WaitForInputIdleMilliseconds
Optional time to wait for UI input idle in milliseconds.

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

### -WaitForWindowIntervalMilliseconds
Polling interval while waiting for a launched window.

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

### -WaitForWindowMilliseconds
Optional time to wait for a launched window in milliseconds.

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
Accept wildcard characters: False
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
Accept wildcard characters: False
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
