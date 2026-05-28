---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Set-LogonWallpaper
## SYNOPSIS
Sets the logon (lock screen) wallpaper.

## SYNTAX
### __AllParameterSets
```powershell
Set-LogonWallpaper [-ImagePath] <string> [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Sets the logon wallpaper using native API when possible and falls back to registry.

## EXAMPLES

### EXAMPLE 1
```powershell
Set-LogonWallpaper -ImagePath 'C:\Path'
```


## PARAMETERS

### -ImagePath
Path to the image file.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `System.Object`

## RELATED LINKS

- None
