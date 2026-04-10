# DesktopManager Windows Verification Plugin

DesktopManager now includes a repo-local Windows verification plugin scaffold for Windows verification workflows through MCP-capable desktop clients.

## Location

- plugin root:
  `plugins/desktopmanager-windows-verification`
- marketplace entry:
  `.agents/plugins/marketplace.json`

## What The Plugin Includes

- `.codex-plugin/plugin.json`
- `.mcp.json`
- `skills/windows-chat-verification/`
- `scripts/start-desktopmanager-mcp.ps1`
- `scripts/start-intelligencex-chat.ps1`
- `scripts/run-intelligencex-chat-smoke.ps1`

## Current Intent

The first public target is local Windows verification through `DesktopManager` MCP, especially:

- opening a desktop app
- inspecting windows and controls
- typing into a visible prompt area
- sending a message
- verifying the visible result with screenshots and control evidence

## Build Requirement

The MCP bootstrap script intentionally avoids building during MCP startup so stdio stays clean.

Build the CLI first:

```powershell
dotnet build .\Sources\DesktopManager.Cli\DesktopManager.Cli.csproj -c Debug
```

or

```powershell
.\Build\Build-Project.ps1 -Build:$false -BuildModule:$false
```

## Mutation Controls

DesktopManager stays inspect-first by default. Use environment variables before starting the MCP client session when the workflow needs interaction:

```powershell
$env:DESKTOPMANAGER_MCP_ALLOW_MUTATIONS = '1'
$env:DESKTOPMANAGER_MCP_ALLOW_PROCESS = 'IntelligenceX.Chat.App'
```

Only enable focused fallback input when the app requires it:

```powershell
$env:DESKTOPMANAGER_MCP_ALLOW_FOREGROUND_INPUT = '1'
```

## IntelligenceX

The plugin includes a helper script for a sibling `IntelligenceX` checkout:

```powershell
.\plugins\desktopmanager-windows-verification\scripts\start-intelligencex-chat.ps1
```

That gives the client a clean way to launch the app before using DesktopManager MCP for inspect, act, and verify loops.

When you want a repeatable baseline artifact bundle against the real app instead of an interactive session, run:

```powershell
.\plugins\desktopmanager-windows-verification\scripts\run-intelligencex-chat-smoke.ps1
```

Add `-ProbeForegroundTyping` when you want the smoke script to attempt a focused typing probe and save the resulting verification payload alongside the screenshots and control diagnostics.

For `IntelligenceX.Chat`, the preferred real-app verification path is now automation mode plus the app-owned control pipe, because the visible WebView composer is still not a dependable UIA surface. That flow adds stable `status`, `new_conversation`, `send_prompt`, `wait_for_idle`, `list_conversations`, and `get_transcript_tail` checks to the artifact bundle:

```powershell
.\plugins\desktopmanager-windows-verification\scripts\run-intelligencex-chat-smoke.ps1 -AutomationMode -ProbeAutomationContract
```

The smoke artifacts then include both DesktopManager evidence and app-owned contract evidence such as:

- `automation-status-before.json`
- `automation-send-prompt.json`
- `automation-wait-for-idle.json`
- `automation-status-after.json`
- `automation-conversations-after.json`
- `automation-transcript-tail.json`

If you want the app-owned route without the full smoke harness, use:

```powershell
.\plugins\desktopmanager-windows-verification\scripts\invoke-intelligencex-chat-automation.ps1 -Command status -LaunchIfNeeded
```

That helper script can launch `IntelligenceX.Chat` in automation mode if needed and then call:

- `ping`
- `status`
- `new_conversation`
- `send_prompt`
- `wait_for_idle`
- `get_transcript_tail`
- `list_conversations`
- `switch_conversation`
