---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Get-DesktopAudioEndpoint
## SYNOPSIS
Gets Windows Core Audio endpoints.

## SYNTAX
### __AllParameterSets
```powershell
Get-DesktopAudioEndpoint [[-DeviceId] <string>] [-DataFlow <AudioDataFlow>] [-ActiveOnly] [<CommonParameters>]
```

## DESCRIPTION
Returns endpoint identity, direction, state, default roles, volume, and mute state.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-DesktopAudioEndpoint -ActiveOnly
```


## PARAMETERS

### -ActiveOnly
Returns active endpoints only.

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

### -DataFlow
Endpoint direction to include.

```yaml
Type: AudioDataFlow
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Render, Capture, All

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DeviceId
Optional endpoint identifier.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
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

- `DesktopManager.AudioEndpointInfo`

## RELATED LINKS

- None
