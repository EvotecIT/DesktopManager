---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Set-DesktopWindowVisibility
## SYNOPSIS
Shows or hides a desktop window.

## SYNTAX
### Show (Default)
```powershell
Set-DesktopWindowVisibility [-Name] <string> -Show [-WhatIf] [-Confirm] [<CommonParameters>]
```

### Hide
```powershell
Set-DesktopWindowVisibility [-Name] <string> -Hide [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Shows or hides a desktop window.

## EXAMPLES

### EXAMPLE 1
```powershell
Set-DesktopWindowVisibility -Name "*Notepad*" -Hide
```

Hide all Notepad windows

### EXAMPLE 2
```powershell
Set-DesktopWindowVisibility -Name "*Notepad*" -Show
```

Show all Notepad windows

## PARAMETERS

### -Hide
Hide the window.

```yaml
Type: SwitchParameter
Parameter Sets: Hide
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
Window title to match. Supports wildcards.

```yaml
Type: String
Parameter Sets: Show, Hide
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Show
Show the window.

```yaml
Type: SwitchParameter
Parameter Sets: Show
Aliases: None
Possible values:

Required: True
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
