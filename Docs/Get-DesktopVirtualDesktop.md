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
### __AllParameterSets
```powershell
Get-DesktopVirtualDesktop [-Handle] <IntPtr> [<CommonParameters>]
```

## DESCRIPTION
Gets supported virtual-desktop state for a top-level window.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-DesktopVirtualDesktop -Handle 'Value'
```


## PARAMETERS

### -Handle
The top-level window handle.

```yaml
Type: IntPtr
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
