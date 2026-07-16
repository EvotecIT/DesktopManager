---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Set-DesktopAudioEndpoint
## SYNOPSIS
Sets master volume or mute for a Windows audio endpoint.

## SYNTAX
### __AllParameterSets
```powershell
Set-DesktopAudioEndpoint [-DeviceId] <string> [-Volume <float>] [-Muted <bool>] [-PassThru] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Applies only explicitly bound settings and can return the resulting endpoint.

## EXAMPLES

### EXAMPLE 1
```powershell
Set-DesktopAudioEndpoint -DeviceId 'Value'
```


## PARAMETERS

### -DeviceId
The stable Windows endpoint identifier.

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

### -Muted
The explicit master mute state.

```yaml
Type: Boolean
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -PassThru
Returns the resulting endpoint snapshot.

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

### -Volume
Master volume from 0 through 100.

```yaml
Type: Single
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

- `DesktopManager.AudioEndpointInfo`

## RELATED LINKS

- None
