---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Set-DesktopPersonalization
## SYNOPSIS
Applies a typed personalization settings object.

## SYNTAX
### __AllParameterSets
```powershell
Set-DesktopPersonalization [-InputObject] <PersonalizationSettings> [-PassThru] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Applies a typed personalization settings object.

## EXAMPLES

### EXAMPLE 1
```powershell
Set-DesktopPersonalization -InputObject 'Value'
```


## PARAMETERS

### -InputObject
The typed settings to apply.

```yaml
Type: PersonalizationSettings
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -PassThru
Returns the resulting snapshot.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `DesktopManager.PersonalizationSettings`

## OUTPUTS

- `None`

## RELATED LINKS

- None
