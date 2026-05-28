---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Get-DesktopWindowTarget
## SYNOPSIS
Gets saved reusable window-relative targets, or resolves one against live windows.

## SYNTAX
### __AllParameterSets
```powershell
Get-DesktopWindowTarget [[-Name] <string>] [-Resolve] [-WindowName <string>] [-ActiveWindow] [-All] [<CommonParameters>]
```

## DESCRIPTION
Returns saved DesktopManager window targets.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-DesktopWindowTarget
```


### EXAMPLE 2
```powershell
Get-DesktopWindowTarget -Name "editor-center"
```


### EXAMPLE 3
```powershell
Get-DesktopWindowTarget -Name "editor-center" -Resolve -WindowName "*Notepad*"
```


## PARAMETERS

### -ActiveWindow
Use the current foreground window when resolving a target.

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

### -All
Return resolved points for all matching windows instead of only the first.

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

### -Name
Optional saved target name. When omitted, all saved target names are returned.

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
Accept wildcard characters: True
```

### -WindowName
Window title filter to use when resolving a target. Supports wildcards.

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
