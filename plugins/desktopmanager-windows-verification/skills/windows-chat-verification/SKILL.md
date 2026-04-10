---
name: windows-chat-verification
description: Verify real Windows chat-app workflows through DesktopManager MCP. Use when an MCP-capable assistant should open a desktop chat app, inspect its window and controls, type into the prompt area, send a message, and verify the visible result with screenshots and structural or app-owned evidence.
---

# Windows Chat Verification

Use this skill when the task is to verify a real Windows chat application through DesktopManager, especially IntelligenceX Chat and similar WebView2 or Chromium-style shells.

## Golden Path

1. Make sure the DesktopManager MCP server is available through this plugin.
   - The plugin starts `DesktopManager` through `./scripts/start-desktopmanager-mcp.ps1`.
   - Build `DesktopManager.Cli` first if the repo has not produced a CLI host yet.
   - Keep the default server read-only unless the verification needs clicks, typing, or layout changes.
2. Enable only the minimum mutation scope you need.
   - Set `DESKTOPMANAGER_MCP_ALLOW_MUTATIONS=1` before starting the MCP client session when the workflow needs live interaction.
   - Use `DESKTOPMANAGER_MCP_ALLOW_PROCESS=IntelligenceX.Chat.App;DesktopManager.TestApp` or another narrow process list when you can.
   - Set `DESKTOPMANAGER_MCP_ALLOW_FOREGROUND_INPUT=1` only when a zero-handle UIA editor really needs focused fallback text or key input.
3. Start with inspection, not clicks.
   - Read `desktop://windows/visible`, `desktop://windows/active`, and `desktop://monitors`.
   - Use `list_windows`, `get_active_window`, and `window_exists` before assuming the target app is ready.
   - Capture `screenshot_window` or `screenshot_desktop` when visual proof matters.
4. Launch or attach to the target chat app.
   - For IntelligenceX Chat in a sibling repo, use `./scripts/start-intelligencex-chat.ps1` if available.
   - Otherwise use `launch_process` or `launch_and_wait_for_window`.
   - Prefer `wait_for_window` over blind retries.
5. Inspect controls before typing.
   - Use `list_window_controls` first.
   - For WebView2 or Chromium-style surfaces, use `diagnose_window_controls` when expected edit controls are missing or unstable.
   - Prefer saved control targets when the same prompt area or send button will be reused.
6. Interact with the smallest safe target.
   - Prefer `click_control`, `set_control_text`, and `send_control_keys` when the app exposes usable controls.
   - If the chat editor is a zero-handle UIA surface, use explicit foreground fallback only when the server was started with `--allow-foreground-input`.
   - If structure still fails, switch to screenshot-assisted targeting with `get_window_geometry`, `save_window_target`, and `click_window_point`.
   - For `IntelligenceX.Chat`, prefer the app-owned automation contract through `./scripts/invoke-intelligencex-chat-automation.ps1` before spending time on coordinate fallbacks.
7. Verify the result from more than one angle.
   - Capture an after screenshot.
   - Re-read the relevant controls or window text when possible.
   - Confirm the visible transcript or status change instead of trusting the input acknowledgement alone.
   - When `IntelligenceX.Chat` runs in automation mode, confirm `wait_for_idle`, `get_transcript_tail`, or `list_conversations` from the app-owned pipe.

## IntelligenceX Notes

- IntelligenceX Chat currently uses a WebView2 shell with a composer textarea and send button in the web UI.
- In the sibling repo, the current shell template exposes `textarea#prompt` and `button#btnSend`.
- Treat that as helpful context, not a guarantee that UI Automation will expose the same shape on every run.
- `IntelligenceX.Chat` now also exposes an opt-in app-owned automation pipe in automation mode. That is the preferred verification path when the visible WebView surface is opaque to UIA.
- For IntelligenceX verification, prefer this flow:
  1. launch the app in automation mode or attach to an existing automation-mode instance
  2. capture the real window with DesktopManager so you still have visual evidence
  3. call `./scripts/invoke-intelligencex-chat-automation.ps1 -Command new_conversation -LaunchIfNeeded`
  4. call `./scripts/invoke-intelligencex-chat-automation.ps1 -Command send_prompt -Text "Reply with only the single word PONG."`
  5. call `./scripts/invoke-intelligencex-chat-automation.ps1 -Command wait_for_idle`
  6. call `./scripts/invoke-intelligencex-chat-automation.ps1 -Command get_transcript_tail -Count 2`
  7. confirm the latest assistant item matches the expected reply
  8. keep DesktopManager screenshots and window/control diagnostics alongside the app-owned verification

## Decision Rules

- Prefer inspection-first MCP workflows over ad-hoc shell automation.
- Prefer `launch_and_wait_for_window` when the very next step depends on the real app window.
- Prefer UIA or control-aware actions before coordinate clicks.
- Prefer `diagnose_window_controls` before escalating to foreground fallback on modern apps.
- Prefer screenshot-assisted window targets over raw pixel guesses when structure is weak.
- Prefer `captureAfter` for mutating actions that matter.
- Prefer app-owned contracts over brittle coordinate replay when the target app explicitly provides one.
- If the server blocks mutating tools, explain that the MCP session is still in read-only mode instead of pretending the selector failed.

## Useful Environment Variables

- `DESKTOPMANAGER_REPO_ROOT`
- `DESKTOPMANAGER_MCP_EXE`
- `DESKTOPMANAGER_MCP_DLL`
- `DESKTOPMANAGER_MCP_ALLOW_MUTATIONS`
- `DESKTOPMANAGER_MCP_ALLOW_PROCESS`
- `DESKTOPMANAGER_MCP_DENY_PROCESS`
- `DESKTOPMANAGER_MCP_ALLOW_FOREGROUND_INPUT`
- `DESKTOPMANAGER_MCP_DRY_RUN`
- `INTELLIGENCEX_REPO_ROOT`
- `IXCHAT_AUTOMATION_MODE`

## Reference Files

- `Docs/DesktopManager.Mcp.md`
- `.agents/skills/desktopmanager-operator/SKILL.md`
- `plugins/desktopmanager-windows-verification/scripts/start-desktopmanager-mcp.ps1`
- `plugins/desktopmanager-windows-verification/scripts/start-intelligencex-chat.ps1`
- `plugins/desktopmanager-windows-verification/scripts/invoke-intelligencex-chat-automation.ps1`
