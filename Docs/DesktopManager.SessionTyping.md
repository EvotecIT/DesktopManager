# Typing into remote and locked-down sessions

Some places don't take a paste: an RDP or Citrix session with clipboard redirection off, a VNC or Apple Remote Desktop session, a password field that blocks paste, or a console where scripts can't be copied in. `SessionTypingService` types the text as key presses instead.

## How it types

Remote clients forward keys, not characters. RDP and Citrix send scan codes to the remote machine, which turns them into characters with its own keyboard layout. So the text is planned as scan codes for the layout the remote side uses, including Shift, AltGr and dead keys, and pressed with real key timing.

| Profile | Keys sent | Pacing (key / modifier hold / line / pause) |
| ------- | --------- | ------------------------------------------- |
| `Local` | Unicode characters | none |
| `Rdp` (default) | scan codes for the chosen layout | 5 ms / 10 ms / 50 ms / 50 ms every 64 characters |
| `Citrix` | scan codes for the chosen layout | 10 ms / 15 ms / 100 ms / 100 ms every 32 characters |
| `Vnc` | scan codes for the local layout | 25 ms / 40 ms / 150 ms / 200 ms every 32 characters |

Without `KeyboardLayout`, the target window's own layout is used. Set it to the remote layout (a KLID such as `00000407` or a culture such as `de-DE`) when the remote machine uses a different one. Any pacing value can be overridden.

## Safety

- It waits up to `ForegroundTimeout` (15 seconds) for you to click into the target instead of taking focus. Only the `Local` profile can activate the window itself.
- It refuses an elevated target from a non-elevated process, because Windows would drop the keys silently.
- It waits for Ctrl, Alt, Shift and Windows keys you're still holding to be released, and handles Caps Lock as you choose: `Fail`, `TurnOff` (and turn it back on afterwards) or `Compensate`.
- It stops when the foreground window or the focused control changes, when you type or click yourself, or when cancelled, and releases every key it pressed.
- A character the layout can't type stops it before the first key by default (`UnmappedCharacters = Fail`).
- The result says how many characters were sent and why it stopped. The text is never logged.

```csharp
SessionTypingResult result = SessionTypingService.Type(window, password, new SessionTypingOptions {
    Profile = SessionTypingProfile.Citrix,
    KeyboardLayout = "de-DE"
});
if (!result.Completed) {
    Console.Error.WriteLine($"Stopped after {result.CharactersSent} characters: {result.AbortReason}");
}
```

## Scripts

Line breaks are pressed as Enter by default. Use `LineBreak = ShiftEnter` where Enter would run each line on its own, and `StripLeadingIndentation` for editors that indent new lines themselves.

## PowerShell and CLI

```powershell
Send-DesktopSessionText -Name '*Remote Desktop Manager*' -SecureString (Read-Host -AsSecureString)
Send-DesktopSessionText -Name '*Remote Desktop Manager*' -Text (Get-Content .\Setup.ps1 -Raw) -Layout de-DE -StripIndentation
Send-DesktopSessionText -Name '*Remote Desktop Manager*' -Credential $credential -Profile Vnc
```

`-Credential` types only the password. Secure strings are cleared from memory after typing.

```powershell
Get-Content .\setup.ps1 -Raw | desktopmanager window type-session --process Devolutions.RemoteDesktopManager --text-stdin --layout de-DE
```

`--text-stdin` and `--text-env <name>` keep the text off the command line and out of shell history. One trailing line break is dropped from standard input, so a piped password doesn't press Enter. `window type` accepts the same two options. Session typing is not offered over MCP, because a secret passed as a tool argument would end up in the conversation.

## Pasting privately

When the library pastes text for you, it marks the clipboard content so Windows keeps it out of clipboard history, the cloud clipboard and clipboard managers (`WindowInputOptions.PrivateClipboard`, on by default). With `PreserveClipboard`, everything the clipboard held before comes back afterwards, or it is emptied if it was empty. RDP fetches redirected clipboard content only when the remote side pastes, so use `ClipboardHoldMilliseconds` to keep the text available long enough.

## What still needs a live check

The planner and runner are tested against keyboard layout tables and a recorded input sink, not against real remote sessions. Before relying on a profile, try it in a VM or Windows Sandbox: AltGr and dead keys through RDP, modifier timing over VNC, a Citrix browser password field, and whether RDP clipboard redirection carries text marked private.
