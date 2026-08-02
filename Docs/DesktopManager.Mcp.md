# DesktopManager MCP and Operator Notes

DesktopManager exposes two automation surfaces:

- CLI:
  `desktopmanager ...`
- MCP over stdio:
  `desktopmanager mcp serve`

For agent-driven desktop automation, prefer MCP first and use CLI as fallback.

The MCP server starts in read-only inspection mode. Use `desktopmanager mcp serve --allow-mutations` when you intentionally want mutating tools. System-wide state changes also require `--allow-system-settings`; the undocumented global airplane-mode tools require `--allow-experimental`. Add `--allow-process <pattern>` or `--deny-process <pattern>` when a session should be constrained to specific apps, add `--allow-foreground-input` only when zero-handle UIA text or key actions may need focused fallback, and use `--dry-run` for mutation previews.

Device management remains inventory-only in MCP. `list_devices`, `get_device`, `list_device_drivers`, `list_driver_packages`, `list_device_classes`, and `list_device_containers` are always read-only; native device and driver mutations stay on the confirmation-aware CLI and PowerShell surfaces. See [DesktopManager.DeviceManagement.md](DesktopManager.DeviceManagement.md).

The stdio transport follows MCP revision `2025-06-18`: each UTF-8 JSON-RPC message is written on one line. JSON-RPC batches are not accepted by this protocol revision.

## Current MCP Tools

- `get_active_window`
- `list_windows`
- `get_window_geometry`
- `window_exists`
- `active_window_matches`
- `wait_for_window`
- `wait_for_window_visual_change`
- `read_window_text`
- `resolve_window_text`
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
- `list_named_visual_baselines`
- `get_named_visual_baseline`
- `save_visual_baseline`
- `assert_visual_baseline`
- `resolve_visual_baseline`
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
- `get_system_state`
- `get_audio_endpoints`
- `configure_audio_endpoint`
- `get_personalization`
- `apply_personalization`
- `get_taskbars`
- `configure_taskbar`
- `get_workstation_profiles`
- `save_workstation_profile`
- `apply_workstation_profile`
- `delete_workstation_profile`
- `list_radios`
- `set_radio_state`
- `get_airplane_mode`
- `set_airplane_mode`
- `get_window_virtual_desktop`
- `move_window_to_virtual_desktop`
- `invoke_system_action`
- `configure_keep_awake`

`list_radios` and `set_radio_state` use the supported Windows radio API. `get_airplane_mode` and `set_airplane_mode` are separately gated because they use an undocumented Windows shell contract. The virtual-desktop tools expose only the public window operations; they do not claim support for enumerating, creating, renaming, switching, or removing desktops.

## Current MCP Resources

- `desktop://monitors`
- `desktop://windows/active`
- `desktop://windows/visible`
- `desktop://layouts`
- `desktop://targets`
- `desktop://visual-baselines`
- `desktop://control-targets`
- `desktop://snapshot/current`

## Current MCP Prompts

- `prepare_for_coding`
- `prepare_for_screen_sharing`
- `clean_up_distractions`

## Recommended Agent Workflow

1. Start the server in the safest useful mode.
   - use plain `desktopmanager mcp serve` for inspection-only sessions
   - add `--allow-mutations` only when the workflow really needs state changes
   - add `--allow-system-settings` only when the workflow needs global audio, power/session, personalization, taskbar, workstation-profile, or radio changes
   - add `--allow-experimental` only when the workflow explicitly accepts the undocumented global airplane-mode contract
   - add `--allow-process` and `--deny-process` when the workflow should be limited to specific desktop apps
   - add `--allow-foreground-input` only for sacrificial or tightly controlled sessions that may need focused fallback in modern apps
   - add `--dry-run` when you want a preview of mutating requests without side effects
2. Inspect first.
   - Read `desktop://windows/visible`, `desktop://windows/active`, and `desktop://monitors`.
   - Use `get_active_window` when focus matters.
   - Use `window_exists` or `active_window_matches` when you want a structured assertion before acting.
   - Use `screenshot_desktop` or `screenshot_window` when visual confirmation is needed.
   - Use `wait_for_window_visual_change` when the next step depends on visible feedback from an opaque surface, not only on a control or text query.
   - Use `get_window_geometry` when you need outer-window and client-area bounds before a coordinate-based action.
   - When multiple windows match, prefer an exact `handle` over a broad process selector.
3. Launch when needed.
   - Use `launch_process` for the app under test.
   - Prefer `launch_and_wait_for_window` when the next step depends on a real launched window, because it binds the wait to the launched process and returns both launch and wait results together.
   - Use `waitForWindowMs` when you want launch to spend a short time correlating the real app window before returning.
   - When you know what kind of window should appear, pass `windowTitle` or `windowClassName` and set `requireWindow=true`.
   - Use `wait_for_window` before trying to move, focus, or capture the window.
4. Inspect controls before trying to interact.
   - Use `list_window_controls` to discover target handles, classes, visible text, automation ids, control types, and control bounds.
   - Use `diagnose_window_controls` when a modern app is not exposing the controls you expect. It will tell you whether Win32 or UIA discovery produced results, whether foreground preparation succeeded, and what each probed UIA root returned.
- `diagnose_window_controls` also accepts a saved `targetName`, so reusable control targets and one-off selectors share the same diagnostic path.
- `diagnose_window_controls` also accepts `includeActionProbe`, which adds a read-only UIA action-resolution probe for the first matched UIA control.
- Diagnostic results now include elapsed times for the overall pass, and the optional action probe includes its own elapsed time too.
   - Use `control_exists` or `wait_for_control` when the target control can appear asynchronously or when you want a structured assertion before clicking.
   - Use `assert_control_value` when you need to prove that a field really contains the expected value, not just that a matching control exists.
   - When modern controls expose state through UI Automation, filter by current value, enabled state, or keyboard focusability instead of guessing from text alone.
   - If a UIA-heavy query is host-sensitive, opt into foreground assistance before falling back to brittle retries.
   - If structure discovery still fails, use `screenshot_window` plus `get_window_geometry`, then target `click_window_point`, `drag_window_points`, or `scroll_window_point`.
   - After a coordinate-based action on an opaque surface, prefer `wait_for_window_visual_change` over a blind sleep so the workflow can prove the app actually reacted.
   - For one-step mutate-and-verify flows, the same window/control mutation tools can now return built-in visual-change metrics when `waitForVisualChange=true` is supplied.
   - Prefer ratio-based targeting with `clientArea=true` when you want the action to scale with different window sizes.
   - When the same coordinate fallback will be reused, save it once with `save_window_target` and resolve or reuse it by name.
   - When a workflow needs a reusable visual checkpoint, save it once with `save_visual_baseline` and compare it later with `assert_visual_baseline` instead of carrying ad-hoc reference images outside DesktopManager.
   - When that same saved region should also become a reusable visual anchor, call `resolve_visual_baseline` to relocate it inside the current window or client area before a follow-up click or drag.
   - For the common click case, `click_window_point` can now take `visualBaselineName` directly so the same saved visual region becomes an actionable anchor, not only a read-only resolution step.
   - The same anchor model now extends to `drag_window_points` and `scroll_window_point`, so saved visual regions can drive richer pointer actions too.
   - Those anchor-driven mutation results now also report the resolved regions and action points they used, which is useful for retries, audits, and follow-up reasoning.
   - When structure discovery is weak but the UI text is visually clear, use `read_window_text` or `resolve_window_text` before falling back to blind coordinates.
   - `resolve_window_text` is the new generic text-anchor path for opaque surfaces: it returns visible label bounds and a ready-to-use action point without needing a saved target or app-specific adapter.
   - For the common click case, `click_window_point` can now take `ocrText` directly so the same OCR text-anchor path becomes actionable without an extra resolve step.
   - The same OCR text-anchor path now extends to `drag_window_points` and `scroll_window_point`, so visible text can drive richer pointer actions without pre-saved coordinates.
   - When the same control selector will be reused, save it once with `save_control_target` and resolve or reuse it by name.
   - Prefer `type_window_text` for whole-window text entry.
   - Prefer `send_window_keys` for whole-window Enter, Escape, or accelerator follow-up actions when a modern control becomes unreliable after text entry.
   - Prefer `click_control`, `set_control_text`, and `send_control_keys` for control-level interactions.
- Saved control targets are especially useful for modern Chromium-style apps because they preserve the same capability-aware selector profile across runs.
- The same saved control target can now drive read-only discovery too, not just actions, so agents can `list`, `exists`, and `wait` against one reusable selector profile.
   - For classic handle-backed controls, `set_control_text` and `send_control_keys` now route directly to the target control instead of depending on foreground focus.
   - For zero-handle UIA controls in modern apps, foreground-based text or key fallback is now shared too, but should be treated as an explicit opt-in because it can affect the live focused app.
   - When foreground text fallback is enabled for those controls, the shared library revalidates the exact focused target and emits Unicode input directly without changing the clipboard.
5. Prefer named state when available.
   - Use `list_named_layouts` before moving windows one by one.
   - Use `apply_named_layout` or `restore_saved_snapshot` when a saved state exists.
   - Use `assert_window_layout` when the workflow depends on the layout being correct before continuing, not just on a layout name existing.
6. Make reversible changes.
   - Prefer `focus_window`, `snap_window`, and `minimize_windows` over destructive actions.
   - Save the current layout or snapshot before larger rearrangements.
7. Explain intent.
   - Say what will change before applying a layout or minimizing multiple windows.
8. Avoid assumptions.
   - Match windows by title, process, class, pid, or handle.
   - Start with specific selectors when possible.
   - Remember that `activeWindow` means the current foreground window, which may be the agent host if it has focus.
9. Ask mutating tools for evidence when it matters.
   - mutating MCP tools now accept `captureBefore`, `captureAfter`, and `artifactDirectory`.
   - their structured results now include `success`, `elapsedMilliseconds`, `safetyMode`, optional resolved target name/kind, best-effort before/after screenshots, and artifact warnings.
   - prefer `captureAfter=true` for lightweight confirmation and add `captureBefore=true` when you need a stronger audit trail.
10. Prefer shared workflows over prompt-only orchestration when they fit.
   - `prepare_for_coding`, `prepare_for_screen_sharing`, and `clean_up_distractions` now exist as MCP tools, not only as prompts.
   - use them when you want a structured result with layout/focus/minimize details instead of free-form narration.
   - when a workflow is given an explicit window selector, its structured result can include `ResolvedWindow`, but focus and target resolution are still best-effort and may fall back to explanatory `Notes`.

## CLI Fallback Patterns

Use the CLI when MCP is unavailable or when validating the same operation outside the MCP transport.

```text
desktopmanager window list
desktopmanager window exists --title "Codex"
desktopmanager window active-matches --title "Codex"
desktopmanager window wait --process notepad --timeout-ms 5000
desktopmanager window wait-visual-change --handle 0xFF1802 --client-area --timeout-ms 5000 --json
desktopmanager window geometry --handle 0xFF1802 --json
desktopmanager window list --process notepad --json
desktopmanager target save editor-center --x-ratio 0.5 --y-ratio 0.5 --client-area
desktopmanager target resolve editor-center --handle 0xFF1802 --json
desktopmanager visual save editor-clean --handle 0xFF1802 --client-area --json
desktopmanager visual resolve editor-clean --handle 0xFF1802 --client-area --max-average-difference 10 --json
desktopmanager window click --handle 0xFF1802 --visual-baseline editor-clean --client-area --baseline-max-average-difference 10 --json
desktopmanager window scroll --handle 0xFF1802 --visual-baseline editor-clean --delta -120 --client-area --baseline-max-average-difference 10 --json
desktopmanager visual assert editor-clean --handle 0xFF1802 --client-area --max-changed-ratio 0.01 --json
desktopmanager control-target save edge-address --control-type Edit --background-text --uia
desktopmanager control-target resolve edge-address --process msedge --json
desktopmanager control list --window-title "Codex" --target codex-sidebar-toggle --json
desktopmanager control exists --window-title "Codex" --target codex-sidebar-toggle --json
desktopmanager control wait --window-title "Codex" --target codex-sidebar-toggle --timeout-ms 1000 --interval-ms 100 --json
desktopmanager window click --handle 0xFF1802 --x 200 --y 200
desktopmanager window click --handle 0xFF1802 --x-ratio 0.5 --y-ratio 0.5 --client-area
desktopmanager window click --handle 0xFF1802 --target editor-center
desktopmanager window drag --handle 0xFF1802 --start-x 200 --start-y 200 --end-x 400 --end-y 220 --client-area
desktopmanager window drag --handle 0xFF1802 --start-x-ratio 0.2 --start-y-ratio 0.2 --end-x-ratio 0.6 --end-y-ratio 0.2 --client-area
desktopmanager window drag --handle 0xFF1802 --start-target editor-center --end-target editor-right
desktopmanager window scroll --handle 0xFF1802 --x 200 --y 200 --delta -120 --client-area
desktopmanager window scroll --handle 0xFF1802 --x-ratio 0.5 --y-ratio 0.5 --delta -120 --client-area
desktopmanager window scroll --handle 0xFF1802 --target editor-center --delta -120
desktopmanager window type --handle 0x30A263C --text "Hello world"
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
desktopmanager control click --window-title "Codex" --target codex-sidebar-toggle
desktopmanager control click --window-process notepad --class RichEditD2DPT
desktopmanager control set-text --window-process notepad --class RichEditD2DPT --text "Hello world"
desktopmanager control send-keys --window-process notepad --class RichEditD2DPT --keys VK_CONTROL,VK_A
desktopmanager process start notepad.exe --wait-for-input-idle-ms 1000
desktopmanager process start-and-wait notepad.exe --window-title "*Notepad*" --timeout-ms 5000 --json
desktopmanager screenshot desktop
desktopmanager screenshot window --process notepad
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
desktopmanager mcp serve
desktopmanager mcp serve --allow-mutations
desktopmanager mcp serve --allow-mutations --allow-process notepad
desktopmanager mcp serve --allow-mutations --deny-process teams
desktopmanager mcp serve --allow-mutations --allow-foreground-input
desktopmanager mcp serve --dry-run
```

## Screenshot-Assisted Target Flow

When control structure is flaky, prefer this flow:

1. Capture the target window with `screenshot_window`.
2. Read `get_window_geometry` if you need exact client-area bounds.
3. Save a reusable point or area with `save_window_target`.
4. Reuse it from `resolve_window_target`, `screenshot_window` with `targetName`, `click_window_point`, `drag_window_points`, or `scroll_window_point`.
5. When the action should visibly change the same surface, call `wait_for_window_visual_change` against the whole window, client area, or saved target region before assuming success.

That keeps screenshot-assisted targeting in the shared core instead of forcing the agent to re-invent one-off coordinates every run.

## Safety Notes

- DesktopManager currently focuses on non-destructive window and layout operations.
- Screenshots are written to PNG files and returned as file paths.
- Window screenshots prefer native window rendering and fall back to screen capture when that is unavailable.
- Visible-change waits reuse that same capture path, so opaque WinUI/WebView surfaces can be verified by pixel change even when their inner controls are not structurally exposed.
- Snapshots are windows-only for now.
- Control discovery supports both child-window selectors and UIA-oriented selectors.
- Control assertions and waits are available on the same shared selector model as list/click/set-text.
- Saved layouts now also support structured assertion through `assert_window_layout`, so agents can verify geometry/state expectations before assuming the desktop is ready.
- Control diagnostics are available on the same shared selector model and help explain Win32 versus UIA discovery gaps.
- Control diagnostics now include per-root UIA probe details, which makes Chromium-style discovery failures much easier to explain and debug.
- Control diagnostics now also show whether a preferred UIA root was learned and reused inside the current long-lived process, which is most relevant for MCP sessions and in-process waits.
- Control diagnostics now also show whether cached UIA root controls were reused, which helps explain why repeated MCP reads can get cheaper after the first expensive Chromium-style discovery pass.
- Control listings now include shared bounds metadata, which makes structural discovery easier to line up with screenshots and window geometry.
- Window-relative clicking, dragging, and scrolling are available as shared fallbacks for apps that do not expose stable controls.
- Coordinate fallbacks can target the client area, which is usually more reliable for browser/editor content than raw outer-window coordinates.
- Ratio-based coordinate fallbacks are available, which makes screenshot-assisted targeting more portable across different window sizes.
- Named window targets are available on the same shared geometry model, which makes repeated coordinate fallbacks much less brittle.
- Named window targets can now also describe reusable areas, not just points, so agents can capture and verify stable visual regions through the shared core.
- Window screenshots now return geometry metadata in JSON so agents can align the image with client-area coordinates.
- Window typing now avoids raw `SendInput` when Windows refuses to foreground the target window, which reduces the chance of text leaking into whatever app currently owns focus.
- Window-level key sending is available as a shared MCP tool, which gives agents a cleaner way to commit or dismiss modern UI flows after whole-window text entry.
- Process launch can now wait for a launched window and prefers windows from the launched process or newer post-launch handles instead of blindly reusing an older matching window.
- Process launch can also validate the launched window by title/class and require a real match before returning.
- `launch_and_wait_for_window` adds a higher-level shared result for unattended launch flows, so MCP callers do not need to stitch `launch_process` and `wait_for_window` together by hand.
- The shared control selector model now supports value, enabled, and keyboard-focusable checks.
- The shared control selector model now also exposes background-safe capability metadata for click, text, keys, and explicit foreground fallback.
- The shared control selector model also supports an opt-in foreground hint for UIA discovery when a target window needs focus.
- Handle-backed control text and key actions now use shared direct message routing, which is safer for background automation than stealing focus first.
- UIA control actions now reuse the same shared fallback-root search strategy as UIA discovery, which reduces action mismatches when controls live under Chromium-style child roots.
- Zero-handle UIA controls now also support shared foreground-based text and key fallback paths, but they should only be enabled intentionally for sacrificial or tightly controlled windows.
- Shared zero-handle UIA text fallback revalidates the exact focused target and emits Unicode input directly without changing the clipboard or the explicit opt-in safety boundary.
- Mutating MCP tools can now return best-effort before/after screenshot artifacts plus safety/timing metadata, which makes it easier to verify what actually happened without building custom wrappers around each action.
- The MCP server now enforces its safety posture instead of documenting it only: default read-only inspection, explicit mutation opt-in through `--allow-mutations`, explicit risky foreground-input opt-in through `--allow-foreground-input`, and side-effect-free mutation previews through `--dry-run`.
- MCP process filters constrain live mutations to allowed or denied process patterns. Global desktop mutations such as monitor, wallpaper, slideshow, taskbar, background-color, clipboard, audio, power/session, personalization, workstation-profile application, and radio changes are blocked while process filters are active because those operations cannot be made process-local.
- `--allow-system-settings` does not imply `--allow-mutations`; both are required before MCP can change global desktop state.
- `--allow-experimental` is independent of the normal supported radio surface. It is required even to read the undocumented global airplane-mode contract so an agent cannot mistake it for the supported API.
- Higher-level workflow tools now exist for coding prep, screen-sharing prep, and distraction cleanup, so agents do not have to reassemble those routines from prompts alone.
- Saved control targets still pay the underlying UIA discovery cost on modern apps, so target-based `wait` is reusable and safer, but not necessarily cheap.
- Preferred-root reuse is process-local. It helps a long-running MCP server more than separate one-shot CLI invocations.
- The short-lived shared UIA control cache is also process-local, so long-running MCP sessions benefit far more than one-shot CLI calls.
- Repeated UIA actions in the same long-lived process now also try a cached exact-match lookup before a broader root walk, which should reduce repeated click/set-text/send-keys overhead for stable modern-app targets.
- Shared control waits now also prefer already-seen matching window handles inside the same process before they fall back to broad rediscovery, which matters most for long-lived MCP sessions on stable app windows.
- Prefer minimizing distractions instead of closing applications.
- When targeting multiple windows, verify selectors carefully before using `all`.
- Monitor metadata is intended to align with the desktop-coordinate bounds used by monitor screenshots.
