---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Wait-DesktopWindow
## SYNOPSIS
Waits for a desktop window to appear.

## SYNTAX
### __AllParameterSets
```powershell
Wait-DesktopWindow [-Name] <string> [-TimeoutMs <int>] [<CommonParameters>]
```

## DESCRIPTION
Waits for a desktop window to appear.

Polls for a window matching the specified title. Supports wildcards. Throws if the timeout is exceeded.

## EXAMPLES

### EXAMPLE 1
```powershell
Wait-DesktopWindow -Name "*Notepad*" -TimeoutMs 10000
```

Wait up to 10 seconds for Notepad

## PARAMETERS

### -Name
Title of the window to wait for. Supports wildcards.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TimeoutMs
Timeout in milliseconds. Zero waits indefinitely.

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

- `None`

## OUTPUTS

- `None`

## RELATED LINKS

- None
