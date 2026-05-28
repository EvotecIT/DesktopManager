---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Get-DesktopFocusedControl
## SYNOPSIS
Gets the focused control for a desktop window.

## SYNTAX
### ByName
```powershell
Get-DesktopFocusedControl [-Name] <string> [<CommonParameters>]
```

### ByHandle
```powershell
Get-DesktopFocusedControl -Handle <string> [<CommonParameters>]
```

### ActiveWindow
```powershell
Get-DesktopFocusedControl -ActiveWindow [<CommonParameters>]
```

## DESCRIPTION
Gets the focused control for a desktop window.

Returns focused-control metadata for a specific window selected by title, handle, or the current foreground window.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-DesktopFocusedControl -ActiveWindow
```


### EXAMPLE 2
```powershell
Get-DesktopFocusedControl -Handle 0x123456
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
Accept wildcard characters: True
```

### -Handle
Window handle in decimal or hexadecimal format.

```yaml
Type: String
Parameter Sets: ByHandle
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Name
Title of the window to inspect. Supports wildcards.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `System.Object`

## RELATED LINKS

- None
