# DesktopManager Windows Verification

`desktopmanager-windows-verification` is a repo-local DesktopManager plugin for real Windows desktop verification through `DesktopManager`.

It packages:

- one MCP server bootstrap for `DesktopManager`
- one bundled skill for Windows chat-app verification
- one helper script for starting `IntelligenceX.Chat` from a sibling repo
- one helper script for calling the `IntelligenceX.Chat` app-owned automation contract
- one repeatable smoke script for capturing real-app evidence against `IntelligenceX.Chat`

## What It Is For

Use this plugin when you want an MCP-capable client such as Codex or Claude to:

- inspect visible windows and controls
- capture desktop or window screenshots
- click, type, and send keys through DesktopManager
- verify a real app result instead of guessing from logs

The first intended public flow is verifying desktop chat apps such as `IntelligenceX.Chat`.

## Local Install Shape

This plugin is repo-local and listed in:

- `.agents/plugins/marketplace.json`

The plugin lives at:

- `plugins/desktopmanager-windows-verification`

## Prerequisite

Build `DesktopManager.Cli` before using the MCP bootstrap script, because the plugin intentionally avoids compiling during MCP startup.

Examples:

```powershell
dotnet build .\Sources\DesktopManager.Cli\DesktopManager.Cli.csproj -c Debug
```

or

```powershell
.\Build\Build-Project.ps1 -Build:$false -BuildModule:$false
```

## MCP Startup

The plugin points the client at:

- `./scripts/start-desktopmanager-mcp.ps1`

That script prefers an already built or published CLI host and then starts:

```text
DesktopManager mcp serve
```

It supports these environment variables:

- `DESKTOPMANAGER_REPO_ROOT`
- `DESKTOPMANAGER_MCP_EXE`
- `DESKTOPMANAGER_MCP_DLL`
- `DESKTOPMANAGER_MCP_ALLOW_MUTATIONS`
- `DESKTOPMANAGER_MCP_ALLOW_PROCESS`
- `DESKTOPMANAGER_MCP_DENY_PROCESS`
- `DESKTOPMANAGER_MCP_ALLOW_FOREGROUND_INPUT`
- `DESKTOPMANAGER_MCP_DRY_RUN`
- `DESKTOPMANAGER_MCP_DIAGNOSTIC`

Recommended starting point:

```powershell
$env:DESKTOPMANAGER_MCP_ALLOW_MUTATIONS = '1'
$env:DESKTOPMANAGER_MCP_ALLOW_PROCESS = 'IntelligenceX.Chat.App'
```

Only set `DESKTOPMANAGER_MCP_ALLOW_FOREGROUND_INPUT=1` when a modern edit surface truly needs focused fallback input.

## IntelligenceX Helper

If `IntelligenceX` is checked out as a sibling repo, you can launch the chat app with:

```powershell
.\plugins\desktopmanager-windows-verification\scripts\start-intelligencex-chat.ps1
```

You can also override the repo path:

```powershell
.\plugins\desktopmanager-windows-verification\scripts\start-intelligencex-chat.ps1 -IntelligenceXRepoRoot C:\Support\GitHub\IntelligenceX
```

If you want a repeatable artifact bundle for the real app, including window capture, control discovery, control diagnostics, and optional foreground typing evidence, run:

```powershell
.\plugins\desktopmanager-windows-verification\scripts\run-intelligencex-chat-smoke.ps1
```

Optional focused typing probe:

```powershell
.\plugins\desktopmanager-windows-verification\scripts\run-intelligencex-chat-smoke.ps1 -ProbeForegroundTyping
```

Preferred `IntelligenceX.Chat` verification path:

```powershell
.\plugins\desktopmanager-windows-verification\scripts\run-intelligencex-chat-smoke.ps1 -AutomationMode -ProbeAutomationContract
```

That mode uses the app-owned automation contract for `status`, `new_conversation`, `send_prompt`, `wait_for_idle`, `list_conversations`, and `get_transcript_tail`, while still saving DesktopManager screenshots and control evidence for the same run.

For direct contract calls outside the full smoke harness, use:

```powershell
.\plugins\desktopmanager-windows-verification\scripts\invoke-intelligencex-chat-automation.ps1 -Command status -LaunchIfNeeded
```

Example isolated verification flow:

```powershell
.\plugins\desktopmanager-windows-verification\scripts\invoke-intelligencex-chat-automation.ps1 -Command new_conversation -LaunchIfNeeded
.\plugins\desktopmanager-windows-verification\scripts\invoke-intelligencex-chat-automation.ps1 -Command send_prompt -Text "Reply with only the single word PONG."
.\plugins\desktopmanager-windows-verification\scripts\invoke-intelligencex-chat-automation.ps1 -Command wait_for_idle
.\plugins\desktopmanager-windows-verification\scripts\invoke-intelligencex-chat-automation.ps1 -Command get_transcript_tail -Count 2
```

## Next Public Steps

- add a second skill for general Windows smoke tests
- add a visual-verification skill focused on artifact capture
- add a repo-owned end-to-end scenario that can prove the real `IntelligenceX.Chat` composer is structurally actionable, not only visible
- keep expanding the app-owned automation contract while the visible WinUI/WebView surface remains only partially actionable
