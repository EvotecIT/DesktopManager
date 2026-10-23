---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Get-DesktopVirtualDesktop
## SYNOPSIS
Gets supported virtual-desktop state for a top-level window.

## SYNTAX
### Window (Default)
```powershell
Get-DesktopVirtualDesktop [-Handle] <IntPtr> [<CommonParameters>]
```

### List
```powershell
Get-DesktopVirtualDesktop -List -Experimental [<CommonParameters>]
```

## DESCRIPTION
Gets supported virtual-desktop state for a top-level window.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-DesktopVirtualDesktop -Handle 'Value'
```


### EXAMPLE 2
```powershell
Get-DesktopVirtualDesktop -List -Experimental
```


## PARAMETERS

### -Experimental
Acknowledge the undocumented, build-dependent shell contract used for desktop enumeration.

```yaml
Type: SwitchParameter
Parameter Sets: List
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Handle
The top-level window handle.

```yaml
Type: IntPtr
Parameter Sets: Window
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -List
List live virtual desktop identities, names and wallpapers through the experimental shell contract.

```yaml
Type: SwitchParameter
Parameter Sets: List
Aliases: None
Possible values:

Required: True
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

- `None`

## RELATED LINKS

- None
