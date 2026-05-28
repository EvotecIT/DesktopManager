---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Set-DefaultAudioDevice
## SYNOPSIS
Sets the default audio device.

## SYNTAX
### __AllParameterSets
```powershell
Set-DefaultAudioDevice [-DeviceId] <string> [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Sets the default audio device.

Sets the system default audio playback device using Core Audio APIs.

## EXAMPLES

### EXAMPLE 1
```powershell
Set-DefaultAudioDevice -DeviceId 'Value'
```


## PARAMETERS

### -DeviceId
Identifier of the audio device.

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
