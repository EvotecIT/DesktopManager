---
external help file: DesktopManager-help.xml
Module Name: DesktopManager
online version: https://github.com/EvotecIT/DesktopManager
schema: 2.0.0
---
# Send-DesktopSessionText
## SYNOPSIS
Types text, a password or a script into a remote or locked-down session.

## SYNTAX
### Name (Default)
```powershell
Send-DesktopSessionText [-Name] <string> [[-Text] <string>] [-SecureString <securestring>] [-Credential <pscredential>] [-Profile <SessionTypingProfile>] [-Layout <string>] [-LineBreak <SessionLineBreakMode>] [-StripIndentation] [-UnmappedCharacters <UnmappedCharacterPolicy>] [-CapsLock <CapsLockPolicy>] [-ForegroundTimeoutSeconds <int>] [-Activate] [-KeyDelayMilliseconds <Int32>] [-ModifierHoldMilliseconds <Int32>] [-LineDelayMilliseconds <Int32>] [-ChunkSize <Int32>] [-ChunkPauseMilliseconds <Int32>] [-AllowFocusChange] [-IgnoreUserInput] [-PassThru] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### Window
```powershell
Send-DesktopSessionText [[-Text] <string>] -Window <WindowInfo> [-SecureString <securestring>] [-Credential <pscredential>] [-Profile <SessionTypingProfile>] [-Layout <string>] [-LineBreak <SessionLineBreakMode>] [-StripIndentation] [-UnmappedCharacters <UnmappedCharacterPolicy>] [-CapsLock <CapsLockPolicy>] [-ForegroundTimeoutSeconds <int>] [-Activate] [-KeyDelayMilliseconds <Int32>] [-ModifierHoldMilliseconds <Int32>] [-LineDelayMilliseconds <Int32>] [-ChunkSize <Int32>] [-ChunkPauseMilliseconds <Int32>] [-AllowFocusChange] [-IgnoreUserInput] [-PassThru] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Types text into an RDP, Citrix or VNC session, or a password field, where pasting is blocked.

Each character is sent as the key presses that produce it on the session's keyboard layout, so the remote side sees ordinary typing. The cmdlet never takes the focus: it waits until you click into the target window, then types. Typing stops when another window or control takes the focus, when you press a key, click or scroll, or when you press Ctrl+C, and every key it pressed is released.

Pass secrets with -SecureString or -Credential so they never appear on the command line or in history. The text is never written to output, verbose messages or errors.

## EXAMPLES

### EXAMPLE 1
```powershell
Send-DesktopSessionText -Name '*Remote Desktop Manager*' -SecureString (Read-Host -AsSecureString)
```

After the command starts, click into the session; typing begins once its window is in the foreground.

### EXAMPLE 2
```powershell
Send-DesktopSessionText -Name '*Remote Desktop Manager*' -Text (Get-Content .\Setup.ps1 -Raw) -Layout de-DE -StripIndentation
```


### EXAMPLE 3
```powershell
Send-DesktopSessionText -Name '*Remote Desktop Manager*' -Credential $credential -Profile Vnc
```

Only the password of the credential is typed.

## PARAMETERS

### -Activate
Bring the target window to the foreground first. Only allowed with the Local profile.

```yaml
Type: SwitchParameter
Parameter Sets: Name, Window
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -AllowFocusChange
Keep typing when the focused control inside the target window changes.

```yaml
Type: SwitchParameter
Parameter Sets: Name, Window
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -CapsLock
What to do when Caps Lock is on: Fail (default), TurnOff while typing, or Compensate.

```yaml
Type: CapsLockPolicy
Parameter Sets: Name, Window
Aliases: None
Possible values: Fail, TurnOff, Compensate

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ChunkPauseMilliseconds
Pause after each chunk in milliseconds. Defaults to the profile's value.

```yaml
Type: Int32
Parameter Sets: Name, Window
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ChunkSize
Number of characters typed before each pause. Zero types without pausing. Defaults to the profile's value.

```yaml
Type: Int32
Parameter Sets: Name, Window
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Credential
Credential whose password is typed. The user name is not typed.

```yaml
Type: PSCredential
Parameter Sets: Name, Window
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ForegroundTimeoutSeconds
Seconds to wait for you to click into the target window. Zero requires it to be in the foreground already.

```yaml
Type: Int32
Parameter Sets: Name, Window
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -IgnoreUserInput
Keep typing when you press a key, click or scroll.

```yaml
Type: SwitchParameter
Parameter Sets: Name, Window
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -KeyDelayMilliseconds
Delay after each character in milliseconds. Defaults to the profile's value.

```yaml
Type: Int32
Parameter Sets: Name, Window
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Layout
Keyboard layout of the remote session, as a layout identifier such as 00000407 or a culture name such as de-DE. Defaults to the layout of the target window. Ignored by the Local profile.

```yaml
Type: String
Parameter Sets: Name, Window
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -LineBreak
How line breaks are typed: Enter (default), ShiftEnter, or None to leave them out.

```yaml
Type: SessionLineBreakMode
Parameter Sets: Name, Window
Aliases: None
Possible values: Enter, ShiftEnter, None

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -LineDelayMilliseconds
Delay after each line break in milliseconds. Defaults to the profile's value.

```yaml
Type: Int32
Parameter Sets: Name, Window
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ModifierHoldMilliseconds
How long Shift or AltGr is held around the key it modifies, in milliseconds. Defaults to the profile's value.

```yaml
Type: Int32
Parameter Sets: Name, Window
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
Title of the window to type into, such as the remote desktop client. Supports wildcards and must match exactly one visible window.

```yaml
Type: String
Parameter Sets: Name
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PassThru
Return the typing result, which reports how many characters were typed and never contains the text.

```yaml
Type: SwitchParameter
Parameter Sets: Name, Window
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Profile
Kind of session: Rdp (default), Citrix, Vnc for VNC and Apple Remote Desktop viewers, or Local for an ordinary local window.

```yaml
Type: SessionTypingProfile
Parameter Sets: Name, Window
Aliases: None
Possible values: Local, Rdp, Citrix, Vnc

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -SecureString
Secret text to type, such as a password.

```yaml
Type: SecureString
Parameter Sets: Name, Window
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StripIndentation
Leave out leading spaces and tabs of every line, for editors that indent on their own.

```yaml
Type: SwitchParameter
Parameter Sets: Name, Window
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Text
Text to type. For secrets use -SecureString or -Credential instead.

```yaml
Type: String
Parameter Sets: Name, Window
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -UnmappedCharacters
What to do with characters the layout cannot produce: Fail (default), Unicode to send them as Unicode packets, or Skip.

```yaml
Type: UnmappedCharacterPolicy
Parameter Sets: Name, Window
Aliases: None
Possible values: Fail, Unicode, Skip

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Window
Window to type into, for example from Get-DesktopWindow.

```yaml
Type: WindowInfo
Parameter Sets: Window
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `DesktopManager.WindowInfo`

## OUTPUTS

- `DesktopManager.SessionTypingResult`

## RELATED LINKS

- None
