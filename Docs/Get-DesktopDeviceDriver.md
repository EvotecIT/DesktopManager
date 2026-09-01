---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Get-DesktopDeviceDriver
## SYNOPSIS
Gets drivers Windows considers compatible with an exact device instance.

## SYNTAX
### __AllParameterSets
```powershell
Get-DesktopDeviceDriver [-InstanceId] <string> [<CommonParameters>]
```

## DESCRIPTION
Gets drivers Windows considers compatible with an exact device instance.

## EXAMPLES

### EXAMPLE 1
```powershell
PS> Get-DesktopDeviceDriver -InstanceId 'PCI\VEN_1234&DEV_5678\1'
```

Returns ranked compatible driver nodes without changing the selected driver.

## PARAMETERS

### -InstanceId
The exact device instance identifier.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.String`

## OUTPUTS

- `DesktopManager.DesktopDeviceDriverInfo`

## RELATED LINKS

- None
