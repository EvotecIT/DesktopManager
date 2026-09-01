---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Get-DesktopControlTarget
## SYNOPSIS
Gets saved reusable control targets, or resolves one against live windows.

## SYNTAX
### __AllParameterSets
```powershell
Get-DesktopControlTarget [[-Name] <string>] [-Resolve] [-WindowName <string>] [-ActiveWindow] [-AllWindows] [-AllControls] [<CommonParameters>]
```

## DESCRIPTION
Returns saved DesktopManager control targets.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-DesktopControlTarget
```


### EXAMPLE 2
```powershell
Get-DesktopControlTarget -Name "edge-address"
```


### EXAMPLE 3
```powershell
Get-DesktopControlTarget -Name "edge-address" -Resolve -WindowName "*Edge*"
```


## PARAMETERS

### -ActiveWindow
Use the current foreground window when resolving the target.

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

### -AllControls
Return all matching controls instead of only the first resolved control.

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

### -AllWindows
Return resolved controls for all matching windows.

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

### -Name
Optional saved control target name. When omitted, all saved target names are returned.

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

### -Resolve
Resolve the saved target against one or more live windows instead of returning only the saved definition.

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

### -WindowName
Window title filter to use when resolving the target. Supports wildcards.

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
