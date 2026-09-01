---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Invoke-DesktopDeviceScan
## SYNOPSIS
Requests Plug and Play re-enumeration for the machine or one device subtree.

## SYNTAX
### __AllParameterSets
```powershell
Invoke-DesktopDeviceScan [[-InstanceId] <string>] [-Asynchronous] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Requests Plug and Play re-enumeration for the machine or one device subtree.

## EXAMPLES

### EXAMPLE 1
```powershell
Invoke-DesktopDeviceScan -Asynchronous
```


## PARAMETERS

### -Asynchronous
Returns after Windows accepts the asynchronous scan request.

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
An optional exact device instance identifier. When omitted, scans from the machine root.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
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
