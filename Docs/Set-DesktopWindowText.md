---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Set-DesktopWindowText
## SYNOPSIS
Sets text in a desktop window.

## SYNTAX
### Paste (Default)
```powershell
Set-DesktopWindowText [-Name] <string> [-Text] <string> [-Paste] [-ClipboardRetryCount <int>] [-ClipboardRetryDelayMilliseconds <int>] [-ActivationRetryCount <int>] [-ActivationRetryDelayMilliseconds <int>] [-InputRetryCount <int>] [-NoActivate] [-RestoreFocus] [-PreserveClipboard] [-SafeMode] [-Verify] [-PassThru] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### Type
```powershell
Set-DesktopWindowText [-Name] <string> [-Text] <string> [-Type] [-Delay <int>] [-UseMessage] [-ForegroundInput] [-PhysicalKeys] [-HostedSession] [-Script] [-ScriptChunkSize <int>] [-ScriptLineDelayMilliseconds <int>] [-ClipboardRetryCount <int>] [-ClipboardRetryDelayMilliseconds <int>] [-ActivationRetryCount <int>] [-ActivationRetryDelayMilliseconds <int>] [-InputRetryCount <int>] [-NoActivate] [-RestoreFocus] [-PreserveClipboard] [-SafeMode] [-Verify] [-PassThru] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Pastes or types text into a desktop window.

## EXAMPLES

### EXAMPLE 1
```powershell
Set-DesktopWindowText -Name "Notepad" -Text "Hello"
```


### EXAMPLE 2
```powershell
Set-DesktopWindowText -Name "Notepad" -Text "Hello" -Type
```


## PARAMETERS

### -ActivationRetryCount
Number of activation retries.

```yaml
Type: Int32
Parameter Sets: Paste, Type
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -ActivationRetryDelayMilliseconds
Delay between activation retries in milliseconds.

```yaml
Type: Int32
Parameter Sets: Paste, Type
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -ClipboardRetryCount
Number of clipboard open retries.

```yaml
Type: Int32
Parameter Sets: Paste, Type
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -ClipboardRetryDelayMilliseconds
Delay between clipboard retries in milliseconds.

```yaml
Type: Int32
Parameter Sets: Paste, Type
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Delay
Delay in milliseconds between characters when typing.

```yaml
Type: Int32
Parameter Sets: Type
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -ForegroundInput
Require real foreground keyboard input and fail instead of falling back to background message typing.

```yaml
Type: SwitchParameter
Parameter Sets: Type
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -HostedSession
Use a hosted-session typing profile with a fixed US-style foreground scancode path and slower pacing defaults. The target surface must already own focus, and typing stops if focus drifts.

When the repo-owned hosted-session harness is exercised, related diagnostics are written under Artifacts\HostedSessionTyping as a raw JSON snapshot plus a companion summary file.

```yaml
Type: SwitchParameter
Parameter Sets: Type
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -InputRetryCount
Number of input retries.

```yaml
Type: Int32
Parameter Sets: Paste, Type
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Name
Window title to match. Supports wildcards.

```yaml
Type: String
Parameter Sets: Paste, Type
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -NoActivate
Do not activate the window before sending input.

```yaml
Type: SwitchParameter
Parameter Sets: Paste, Type
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -PassThru
Return a structured mutation result object for each matching window.

```yaml
Type: SwitchParameter
Parameter Sets: Paste, Type
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Paste
Use the clipboard paste method.

```yaml
Type: SwitchParameter
Parameter Sets: Paste
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -PhysicalKeys
Prefer layout-aware physical key presses over Unicode packets when typing in the foreground.

```yaml
Type: SwitchParameter
Parameter Sets: Type
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -PreserveClipboard
Preserve and restore clipboard text.

```yaml
Type: SwitchParameter
Parameter Sets: Paste, Type
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -RestoreFocus
Restore focus to the previously active window.

```yaml
Type: SwitchParameter
Parameter Sets: Paste, Type
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -SafeMode
Enable safe mode (no activation, preserve clipboard).

```yaml
Type: SwitchParameter
Parameter Sets: Paste, Type
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Script
Preserve multiline formatting and chunk long lines into smaller typed segments.

```yaml
Type: SwitchParameter
Parameter Sets: Type
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -ScriptChunkSize
Maximum number of characters to send in each script chunk.

```yaml
Type: Int32
Parameter Sets: Type
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -ScriptLineDelayMilliseconds
Delay in milliseconds after each scripted line break.

```yaml
Type: Int32
Parameter Sets: Type
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Text
Text to paste or type.

```yaml
Type: String
Parameter Sets: Paste, Type
Aliases: None
Possible values:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Type
Simulate typing the text.

```yaml
Type: SwitchParameter
Parameter Sets: Type
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -UseMessage
Use WM_CHAR messages instead of SendInput when typing.

```yaml
Type: SwitchParameter
Parameter Sets: Type
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Verify
Re-query the target window after the mutation and report the observed postcondition.

```yaml
Type: SwitchParameter
Parameter Sets: Paste, Type
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
