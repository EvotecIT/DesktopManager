---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Get-DesktopDriverPackage
## SYNOPSIS
Gets third-party packages from the Windows Driver Store.

## SYNTAX
### __AllParameterSets
```powershell
Get-DesktopDriverPackage [[-PublishedInfName] <string>] [-ClassGuid <Guid>] [-IncludeFiles] [-IncludeDevices] [<CommonParameters>]
```

## DESCRIPTION
Gets third-party packages from the Windows Driver Store.

## EXAMPLES

### EXAMPLE 1
```powershell
PS> Get-DesktopDriverPackage -PublishedInfName oem42.inf -IncludeDevices
```

Returns the exact package and device instances currently using it.

## PARAMETERS

### -ClassGuid
An optional setup class identifier.

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

### -IncludeDevices
Includes device instances using each package.

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

### -IncludeFiles
Includes files in each package.

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

### -PublishedInfName
An exact published INF name such as oem42.inf.

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

- `DesktopManager.DesktopDriverPackageInfo`

## RELATED LINKS

- None
