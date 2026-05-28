---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Unregister-DesktopHotkey
## SYNOPSIS
Unregisters a global desktop hotkey.

## SYNTAX
### __AllParameterSets
```powershell
Unregister-DesktopHotkey [-Id] <int> [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Unregisters a global desktop hotkey.

Removes a hotkey previously registered with Register-DesktopHotkey.

## EXAMPLES

### EXAMPLE 1
```powershell
Unregister-DesktopHotkey -Id 1
```


## PARAMETERS

### -Id
Identifier of the hotkey to remove.

```yaml
Type: Int32
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
