---
name: desktopmanager-operator
description: Operate and validate Windows desktop state through DesktopManager using MCP first and CLI second. Use when Codex needs to inspect windows and monitors, capture screenshots, launch desktop applications, wait for windows to appear, inspect child controls, click controls, send text or keys to windows and controls, focus or move windows, apply named layouts, save or restore snapshots, clean up distractions, or prepare the desktop for coding or screen sharing.
---

# DesktopManager Operator

Use this skill to operate the Windows desktop through DesktopManager.

## Golden Path

1. Prefer MCP first:
   `desktopmanager mcp serve`
   - Add `--allow-mutations` only when the session really needs desktop or saved-state changes.
   - Add `--allow-process <pattern>` or `--deny-process <pattern>` when the session should stay scoped to specific desktop apps.
   - Add `--allow-foreground-input` only for sacrificial or tightly controlled sessions that may need focused fallback on zero-handle UIA text or key actions.
   - Add `--dry-run` when you want to preview mutating MCP calls without changing the desktop.
2. Start with inspection, not mutation.
   - Read resources first:
     `desktop://windows/active`
     `desktop://windows/visible`
     `desktop://monitors`
   - Use `get_active_window` when focus matters.
   - Use `window_exists` or `active_window_matches` when you need an explicit assertion first.
   - Use `screenshot_desktop` or `screenshot_window` when visual confirmation is needed.
   - Use `get_window_geometry` when you need exact outer-window and client-area bounds before a coordinate-based action.
   - When more than one window matches, switch to an exact handle before mutating anything.
3. Launch and wait when the target app is not ready yet.
   - Use `launch_process` to start the app.
   - Prefer `launch_and_wait_for_window` when the next step depends on a real launched window instead of a best-effort process start.
   - If launch correlation matters, set a short launch-time window wait instead of assuming the first matching app window is the new one.
   - If you know the expected launched window, pass a title or class filter and require a real match.
   - Use `wait_for_window` before moving, focusing, or capturing it.
4. Inspect controls before interacting.
   - Use `list_window_controls` to discover control handles, classes, text, automation ids, control types, and control bounds.
   - Use `diagnose_window_controls` when a modern app is not exposing the controls you expected. It will show whether Win32 or UIA discovery produced anything, whether foreground preparation helped, and what each probed UIA root returned.
- `diagnose_window_controls` can also take a saved control target name, so reusable target profiles and ad-hoc selectors share the same diagnostic path.
- `diagnose_window_controls` can also include a read-only action probe, which is the safest way to verify repeated UIA action caching without mutating the app.
- The control diagnostics payload now includes elapsed times, so you can compare cold versus warm-cache behavior directly instead of inferring it only from cache flags.
   - Use UIA-oriented selectors when modern apps do not expose useful child-window controls.
   - Use `control_exists` or `wait_for_control` when the control can appear asynchronously or when you want an explicit precondition before clicking.
   - Use `assert_control_value` when the workflow depends on the field content itself, not merely the presence of a matching control.
   - When available, prefer value, enabled, or focusable checks over brittle text-only guesses.
   - If UIA discovery is flaky on a background window, retry with the shared foreground hint before inventing wrapper-specific workarounds.
   - If the app still stays structurally opaque, switch to the shared coordinate-based fallback: capture the window, inspect its geometry, then use `click_window_point`, `drag_window_points`, or `scroll_window_point`.
   - When the same visual region matters across runs, save it as a named target area and reuse it from `screenshot_window` with `targetName` or `screenshot target`.
   - Prefer ratio-based client-area targeting when the same workflow must survive different window sizes.
   - If you will reuse the same fallback point more than once, save it as a named target instead of repeating raw ratios or pixels.
   - Use `type_window_text` for whole-window entry.
   - Use `send_window_keys` for whole-window Enter, Escape, or accelerator follow-up actions when the window is reliable but a modern control is not.
   - Use `click_control`, `set_control_text`, or `send_control_keys` for control-level work.
   - For classic handle-backed controls, prefer `set_control_text` or `send_control_keys` over foreground-dependent hacks because they now route directly to the control.
   - For zero-handle UIA controls in modern apps, foreground-based text or key fallback exists in the shared library too, but treat it as an explicit opt-in for sacrificial or tightly controlled windows.
   - When you do opt into foreground text fallback, the shared library revalidates the exact focused target and emits Unicode input directly without changing the clipboard.
5. Prefer named state over one-off moves.
   - Use `list_named_layouts` before manually moving windows.
   - Use `apply_named_layout` or `restore_saved_snapshot` when the desired setup already exists.
   - Use `assert_window_layout` when the next step depends on the saved layout actually being satisfied, not just available.
6. Make the smallest safe change.
   - Prefer `focus_window`, `snap_window`, or `minimize_windows`.
   - Save the current state before larger changes:
     `save_current_layout`
     `save_current_snapshot`
7. Explain what changed after mutating actions.
8. Use CLI only as fallback or verification.
9. When a mutation matters, ask for evidence.
   - Mutating MCP tools now accept `captureBefore`, `captureAfter`, and `artifactDirectory`.
   - Their structured results include `success`, `elapsedMilliseconds`, `safetyMode`, optional target name/kind, best-effort before/after screenshots, and artifact warnings.
   - Prefer `captureAfter` by default and add `captureBefore` when you need a stronger audit trail.
   - Remember that the MCP server is now read-only by default, so a failed mutating tool call may be a policy block rather than a selector failure.

## MCP Surface

Tools:

- `get_active_window`
- `list_windows`
- `get_window_geometry`
- `window_exists`
- `active_window_matches`
- `wait_for_window`
- `list_window_controls`
- `diagnose_window_controls`
- `control_exists`
- `assert_control_value`
- `wait_for_control`
- `move_window`
- `click_window_point`
- `drag_window_points`
- `scroll_window_point`
- `type_window_text`
- `send_window_keys`
- `focus_window`
- `minimize_windows`
- `snap_window`
- `list_monitors`
- `screenshot_desktop`
- `screenshot_window`
- `launch_process`
- `launch_and_wait_for_window`
- `list_named_targets`
- `get_named_target`
- `save_window_target`
- `resolve_window_target`
- `list_named_control_targets`
- `get_named_control_target`
- `save_control_target`
- `resolve_control_target`
- `click_control`
- `set_control_text`
- `send_control_keys`
- `list_named_layouts`
- `save_current_layout`
- `apply_named_layout`
- `assert_window_layout`
- `list_named_snapshots`
- `save_current_snapshot`
- `restore_saved_snapshot`
- `prepare_for_coding`
- `prepare_for_screen_sharing`
- `clean_up_distractions`

Resources:

- `desktop://monitors`
- `desktop://windows/active`
- `desktop://windows/visible`
- `desktop://layouts`
- `desktop://targets`
- `desktop://control-targets`
- `desktop://snapshot/current`

Prompts:

- `prepare_for_coding`
- `prepare_for_screen_sharing`
- `clean_up_distractions`

## CLI Fallbacks

```text
desktopmanager window list
desktopmanager window exists --title "Codex"
desktopmanager window active-matches --title "Codex"
desktopmanager window wait --process notepad --timeout-ms 5000
desktopmanager window geometry --handle 0xFF1802 --json
desktopmanager target save editor-center --x-ratio 0.5 --y-ratio 0.5 --client-area
desktopmanager target resolve editor-center --handle 0xFF1802 --json
desktopmanager control-target save edge-address --control-type Edit --background-text --uia
desktopmanager control-target resolve edge-address --process msedge --json
desktopmanager window click --handle 0xFF1802 --x 200 --y 200
desktopmanager window click --handle 0xFF1802 --x-ratio 0.5 --y-ratio 0.5 --client-area
desktopmanager window click --handle 0xFF1802 --target editor-center
desktopmanager window drag --handle 0xFF1802 --start-x 200 --start-y 200 --end-x 400 --end-y 220 --client-area
desktopmanager window drag --handle 0xFF1802 --start-x-ratio 0.2 --start-y-ratio 0.2 --end-x-ratio 0.6 --end-y-ratio 0.2 --client-area
desktopmanager window drag --handle 0xFF1802 --start-target editor-center --end-target editor-right
desktopmanager window scroll --handle 0xFF1802 --x 200 --y 200 --delta -120 --client-area
desktopmanager window scroll --handle 0xFF1802 --x-ratio 0.5 --y-ratio 0.5 --delta -120 --client-area
desktopmanager window scroll --handle 0xFF1802 --target editor-center --delta -120
desktopmanager window type --process notepad --text "Hello world"
desktopmanager window keys --process msedge --keys VK_RETURN
desktopmanager control list --window-process notepad
desktopmanager control diagnose --window-title "*Codex*" --uia --ensure-foreground --sample-limit 5 --json
desktopmanager control diagnose --window-title "Codex" --target codex-sidebar-toggle --sample-limit 5 --json
desktopmanager control exists --window-active --uia --control-type Button --text-pattern "Hide sidebar"
desktopmanager control wait --window-active --uia --control-type Button --text-pattern "Show sidebar" --timeout-ms 5000
desktopmanager control exists --window-active --uia --control-type Button --text-pattern "Hide sidebar" --enabled --focusable
desktopmanager control wait --window-handle 0x5BB15E4 --uia --control-type Button --text-pattern "Hide sidebar" --enabled --focusable --ensure-foreground --timeout-ms 5000
desktopmanager control list --window-active --uia --control-type Button
desktopmanager control list --window-title "Codex" --target codex-sidebar-toggle --json
desktopmanager control exists --window-title "Codex" --target codex-sidebar-toggle --json
desktopmanager control wait --window-title "Codex" --target codex-sidebar-toggle --timeout-ms 1000 --interval-ms 100 --json
desktopmanager control click --window-title "Codex" --target codex-sidebar-toggle
desktopmanager control click --window-process notepad --class RichEditD2DPT
desktopmanager control set-text --window-process notepad --class RichEditD2DPT --text "Hello world"
desktopmanager control send-keys --window-process notepad --class RichEditD2DPT --keys VK_CONTROL,VK_A
desktopmanager process start notepad.exe --wait-for-input-idle-ms 1000
desktopmanager process start-and-wait notepad.exe --window-title "*Notepad*" --timeout-ms 5000 --json
desktopmanager screenshot desktop
desktopmanager screenshot window --process notepad
desktopmanager screenshot target edge-editor-pane --process msedge --json
desktopmanager window move --title "Visual Studio Code" --x 0 --y 0 --width 1920 --height 1400
desktopmanager window focus --process code
desktopmanager window snap --title "Visual Studio Code" --position left
desktopmanager monitor list
desktopmanager layout list
desktopmanager layout save coding
desktopmanager layout apply coding
desktopmanager layout assert coding --position-tolerance-px 50 --size-tolerance-px 50 --json
desktopmanager snapshot save before-meeting
desktopmanager snapshot restore before-meeting
```

## Decision Rules

- Prefer reading resources before calling mutating tools.
- Prefer screenshot tools when the task needs visual validation rather than only structural window data.
- Prefer `launch_process` plus `wait_for_window` over blind retries.
- Prefer `launch_and_wait_for_window` when a workflow needs one shared launch result, one shared waited-window result, and optional mutation evidence.
- Remember that `--dry-run` is the safest way to validate mutating MCP payloads and orchestration without touching the desktop.
- Remember that process allow/deny filters only work reliably when the mutating tool declares an explicit process scope, so prefer `processName` over title-only selectors when those filters are active.
- Prefer `list_window_controls` before guessing a control handle.
- Prefer `diagnose_window_controls` when Chromium-style apps or background windows are not returning expected controls, because it now shows per-root UIA probe results instead of only aggregate counts.
- Remember that `diagnose_window_controls` now also shows whether a preferred UIA root was reused inside the current process, which is most useful in MCP sessions or in-process waits.
- Remember that `diagnose_window_controls` now also shows whether cached UIA root controls were reused, which helps explain why repeated MCP reads can speed up after the first heavy Chromium-style pass.
- Prefer `click_window_point`, `drag_window_points`, or `scroll_window_point` over inventing wrapper-specific mouse hacks when screenshots give you a reliable target and the app exposes no usable controls.
- Prefer `send_window_keys` over ad-hoc foreground hacks when the whole window is the stable target and you only need a commit or accelerator key.
- Prefer client-area coordinates for browser/editor content, and outer-window coordinates when you intentionally want chrome like tabs, sidebars, or title-bar buttons.
- Prefer ratio-based coordinates when you expect the target window size to vary between runs or machines.
- Prefer named targets when the same coordinate fallback will be reused across multiple actions or sessions.
- Remember that named targets can now also describe reusable areas, so prefer them for screenshot-assisted visual verification instead of hand-rolled crop coordinates.
- Prefer named control targets when the same control selector or capability profile will be reused across multiple modern-app interactions.
- Remember that saved control targets improve consistency, but the underlying UIA discovery cost is still real on Chromium-style apps, so `wait` may not return instantly even when the control already exists.
- Remember that preferred-root reuse is process-local. A long-running MCP server can benefit from it, while separate one-shot CLI invocations start fresh.
- Remember that the short-lived UIA control cache is also process-local, so long-running MCP sessions benefit much more than one-shot CLI calls.
- Remember that repeated UIA actions in the same long-lived process now try a cached exact-match lookup before a broader root walk, so stable modern-app targets should get cheaper to interact with over time.
- Remember that shared control waits now prefer already-seen matching window handles inside the same process before broad rediscovery, so long-lived MCP sessions should behave better on stable modern-app windows.
- Prefer window-level typing when control-level targeting is uncertain.
- Prefer named layouts and snapshots over repeated manual window placement.
- Prefer `assert_window_layout` before multi-step workflows when layout correctness is a precondition, especially after a best-effort apply or a manual rearrangement.
- Prefer minimizing distracting windows over closing them.
- Remember that broad layout, snapshot-restore, and workflow mutations are intentionally blocked when MCP process filters are active, because their target app set is wider than one declared process.
- Use specific selectors when possible:
  title, process, class, pid, or handle.
- Prefer `handle` over `process` when multiple windows from the same app are open.
- Remember that `activeWindow` means the current foreground window and may resolve to Codex or the terminal if they have focus.
- Be careful with `all`; verify the target set first.
- Remember that snapshots are windows-only for now.
- Remember that whole-window typing now falls back away from raw `SendInput` when the target window does not actually own foreground focus.
- Remember that handle-backed control text and key actions now use shared direct-to-control routing, so they are a better background-safe option than trying to focus the app first.
- Remember that control listings now include shared capability flags for background-safe click, text, keys, and foreground fallback, so inspect those before enabling risky focused-input behavior.
- Remember that explicit foreground fallback now also requires MCP server opt-in through `--allow-foreground-input`, not only per-tool request opt-in.
- Remember that UIA control actions now reuse the same shared fallback-root search strategy as UIA discovery, so a discovered modern-app control is less likely to fail later due to a different action search path.
- Remember that UIA selectors and actions now run through the shared library, but verifying selectors in the current host is still smart before relying on them unattended.
- Remember that mutating MCP tools can now return best-effort before/after screenshot artifacts and safety metadata, so prefer that shared evidence path over inventing wrapper-specific verification blobs.
- Prefer the shared workflow tools for coding prep, screen-sharing prep, and cleanup when they fit, because they now return structured results and keep orchestration inside DesktopManager instead of one-off prompts.
- When a workflow uses an explicit window selector, treat `ResolvedWindow` and `FocusedWindow` as best-effort evidence rather than guaranteed output, and fall back to the workflow `Notes` plus screenshot artifacts when Windows blocks the normal focus path.

## Reference Files

- `Docs/DesktopManager.Cli.md`
- `Docs/DesktopManager.Mcp.md`
- `Sources/DesktopManager.Cli/McpCatalog.cs`
