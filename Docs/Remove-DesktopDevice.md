---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Remove-DesktopDevice
## SYNOPSIS
Uninstalls an exact Plug and Play device instance.

## SYNTAX
### __AllParameterSets
```powershell
Remove-DesktopDevice [-InstanceId] <string> [-DeviceOnly] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
By default the device subtree is removed. Use DeviceOnly to uninstall only the selected instance.

## EXAMPLES

### EXAMPLE 1
```powershell
Remove-DesktopDevice -DeviceOnly
```


## PARAMETERS

### -DeviceOnly
Uninstalls only the selected instance instead of its subtree.

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

- `DesktopManager.DesktopDeviceOperationResult`

## RELATED LINKS

- None
