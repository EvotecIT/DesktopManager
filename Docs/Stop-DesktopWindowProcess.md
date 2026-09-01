---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Stop-DesktopWindowProcess
## SYNOPSIS
Stops the process that owns a desktop window.

## SYNTAX
### __AllParameterSets
```powershell
Stop-DesktopWindowProcess [-InputObject] <WindowInfo> [-EntireProcessTree] [-WaitForExitMilliseconds <int>] [-PassThru] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Stops the process that owns a desktop window.

Terminates the process associated with a resolved desktop window and optionally returns the termination result from the DesktopManager automation core.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-DesktopWindow -ActiveWindow | Stop-DesktopWindowProcess
```


## PARAMETERS

### -EntireProcessTree
Whether to terminate the full process tree.

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

### -InputObject
Window whose owning process should be terminated.

```yaml
Type: WindowInfo
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -PassThru
Return the termination result.

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

### -WaitForExitMilliseconds
How long to wait for the process to exit in milliseconds.

```yaml
Type: Int32
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

- `DesktopManager.WindowInfo`

## OUTPUTS

- `None`

## RELATED LINKS

- None
