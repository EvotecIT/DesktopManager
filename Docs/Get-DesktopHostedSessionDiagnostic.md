---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Get-DesktopHostedSessionDiagnostic
## SYNOPSIS
Gets the latest hosted-session diagnostic artifact or a specific hosted-session artifact.

## SYNTAX
### __AllParameterSets
```powershell
Get-DesktopHostedSessionDiagnostic [[-ArtifactPath] <string>] [-ArtifactDirectory <string>] [-RepositoryRoot <string>] [-SummaryOnly] [<CommonParameters>]
```

## DESCRIPTION
Reads hosted-session typing diagnostics from the DesktopManager artifact folder.

Prefers the companion summary file when one exists and falls back to the JSON diagnostic artifact otherwise.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-DesktopHostedSessionDiagnostic
```


### EXAMPLE 2
```powershell
Get-DesktopHostedSessionDiagnostic -SummaryOnly
```


### EXAMPLE 3
```powershell
Get-DesktopHostedSessionDiagnostic -RepositoryRoot C:\Support\GitHub\DesktopManager
```


### EXAMPLE 4
```powershell
Get-DesktopHostedSessionDiagnostic -ArtifactPath C:\Support\GitHub\DesktopManager\Artifacts\HostedSessionTyping\sample.json
```


## PARAMETERS

### -ArtifactDirectory
Directory containing hosted-session JSON artifacts and summary companion files.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -ArtifactPath
Specific hosted-session JSON artifact to read.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -RepositoryRoot
Repository root used to resolve Artifacts\HostedSessionTyping when ArtifactPath is not supplied.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -SummaryOnly
Returns only the resolved summary text instead of the structured diagnostic record.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `System.Object`

## RELATED LINKS

- None
