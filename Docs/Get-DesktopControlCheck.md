---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Get-DesktopControlCheck
## SYNOPSIS
Gets the check state of a button control.

## SYNTAX
### __AllParameterSets
```powershell
Get-DesktopControlCheck [-Control] <WindowControlInfo> [<CommonParameters>]
```

## DESCRIPTION
Retrieves the check state of a control.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-DesktopControlCheck -Control $ctrl
```


## PARAMETERS

### -Control
Control to query.

```yaml
Type: WindowControlInfo
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
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
