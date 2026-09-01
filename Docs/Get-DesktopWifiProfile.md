---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Get-DesktopWifiProfile
## SYNOPSIS
Gets saved Windows Wi-Fi profiles without scanning nearby networks.

## SYNTAX
### __AllParameterSets
```powershell
Get-DesktopWifiProfile [-InterfaceId <Guid>] [<CommonParameters>]
```

## DESCRIPTION
Profile XML and credentials are never returned.

## EXAMPLES

### EXAMPLE 1
```powershell
PS> Get-DesktopWifiProfile
```

Returns profiles already stored by Windows on every wireless LAN interface.

## PARAMETERS

### -InterfaceId
Optional exact wireless LAN interface identifier.

```yaml
Type: Guid
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

- `None`

## OUTPUTS

- `DesktopManager.DesktopWifiProfileInfo`

## RELATED LINKS

- None
