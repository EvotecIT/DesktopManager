---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Set-DesktopControlText
## SYNOPSIS
Safely edits text on a specific window control or prior semantic observation.

## SYNTAX
### ByControl
```powershell
Set-DesktopControlText [-Control] <WindowControlInfo> [-Text] <string> [-Mode <DesktopTextEditMode>] [-ExpectedFingerprint <string>] [-ExpectedEditContextFingerprint <string>] [-EnsureForeground] [-AllowForegroundInput] [-Verify] [-NoVerify] [-PassThru] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### ByObservation
```powershell
Set-DesktopControlText [-Observation] <DesktopControlObservation> [-Text] <string> [-Mode <DesktopTextEditMode>] [-ExpectedFingerprint <string>] [-ExpectedEditContextFingerprint <string>] [-EnsureForeground] [-AllowForegroundInput] [-Verify] [-NoVerify] [-PassThru] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Uses provider-safe setters first and explicitly gated foreground input for selection or caret edits.

## EXAMPLES

### EXAMPLE 1
```powershell
Set-DesktopControlText -Control $ctrl -Text "Hello world"
```


## PARAMETERS

### -AllowForegroundInput
Explicitly allow focused foreground input fallback for zero-handle UI Automation controls.

```yaml
Type: SwitchParameter
Parameter Sets: ByControl, ByObservation
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Control
Control to update.

```yaml
Type: WindowControlInfo
Parameter Sets: ByControl
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -EnsureForeground
Bring the parent window to the foreground before UI Automation text fallback.

```yaml
Type: SwitchParameter
Parameter Sets: ByControl, ByObservation
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExpectedEditContextFingerprint
Optional selection/caret context fingerprint that must still match before a range edit.

```yaml
Type: String
Parameter Sets: ByControl, ByObservation
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExpectedFingerprint
Optional complete-content fingerprint that must still match before the edit is applied.

```yaml
Type: String
Parameter Sets: ByControl, ByObservation
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Mode
Replace the complete document, replace the current selection, or insert at the current caret.

```yaml
Type: DesktopTextEditMode
Parameter Sets: ByControl, ByObservation
Aliases: None
Possible values: ReplaceDocument, ReplaceSelection, InsertAtCaret

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoVerify
Skip the default exact post-edit text verification.

```yaml
Type: SwitchParameter
Parameter Sets: ByControl, ByObservation
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Observation
Prior generic observation identifying the live control. Its complete-text fingerprint is used as the default concurrency precondition.

```yaml
Type: DesktopControlObservation
Parameter Sets: ByObservation
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PassThru
Return a structured mutation result object for the targeted control.

```yaml
Type: SwitchParameter
Parameter Sets: ByControl, ByObservation
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Text
Text to apply to the control.

```yaml
Type: String
Parameter Sets: ByControl, ByObservation
Aliases: None
Possible values:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Verify
Return the structured verification result. Verification is enabled by default.

```yaml
Type: SwitchParameter
Parameter Sets: ByControl, ByObservation
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
