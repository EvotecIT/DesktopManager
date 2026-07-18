---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Connect-DesktopWifiProfile
## SYNOPSIS
Connects an exact saved Windows Wi-Fi profile without scanning nearby networks.

## SYNTAX
### __AllParameterSets
```powershell
Connect-DesktopWifiProfile [-Name] <string> [-Timeout <timespan>] [-InterfaceId <guid>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The command waits for exclusive access and a Windows WLAN Auto Configuration completion notification. Cancelling or timing out stops the wait but does not cancel an attempt already accepted by Windows, so a later same-process call waits for that attempt to finish before starting another one. If Windows never reports completion, the library releases the retained notification handle after two minutes and requires restarting the hosting process before another connection attempt.

## EXAMPLES

### EXAMPLE 1
```powershell
PS> Connect-DesktopWifiProfile -Name 'Corporate WiFi'
```

Connects the exact saved profile when it exists on one wireless LAN interface.

## PARAMETERS

### -InterfaceId
Optional interface identifier used when the profile exists on multiple wireless LAN adapters.

```yaml
Type: Nullable`1
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Name
The case-sensitive saved Windows Wi-Fi profile name.

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

### -Timeout
How long to wait for exclusive access and a Windows connection completion notification. The default is 30 seconds and the maximum is 2147483647 milliseconds.

```yaml
Type: TimeSpan
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

- `DesktopManager.DesktopWifiConnectionResult`

## RELATED LINKS

- None
