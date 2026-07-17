# DesktopManager CLI MVP

The repository now includes a `DesktopManager.Cli` project that exposes a small, noun-based command tree over the existing `DesktopManager` C# library.

## Goals

- Keep the CLI aligned with the current C# surface area.
- Provide a stable foundation for MCP hosting.
- Reuse existing window, monitor, and layout APIs rather than duplicating desktop logic.

## Command groups

```text
desktopmanager window list
desktopmanager window geometry
desktopmanager window exists
desktopmanager window active-matches
desktopmanager window wait
desktopmanager window wait-visual-change
desktopmanager window type
desktopmanager window keys
desktopmanager window move
desktopmanager window click
desktopmanager window drag
desktopmanager window scroll
desktopmanager window focus
desktopmanager window minimize
desktopmanager window snap

desktopmanager control list
desktopmanager control diagnose
desktopmanager control exists
desktopmanager control wait
desktopmanager control click
desktopmanager control set-text
desktopmanager control send-keys

desktopmanager monitor list

desktopmanager workstation save --name office
desktopmanager workstation list
desktopmanager workstation show --name office
desktopmanager workstation apply --name office
desktopmanager workstation apply --name office --include-machine-policies
desktopmanager workstation delete --name office

desktopmanager audio list --flow render --active
desktopmanager audio set-default --id <endpoint-id> --role multimedia
desktopmanager audio set-volume --id <endpoint-id> --volume 50
desktopmanager audio set-mute --id <endpoint-id> --on

desktopmanager system power
desktopmanager system session
desktopmanager system lock
desktopmanager system keep-awake --seconds 3600 --display
desktopmanager system suspend --hibernate --confirm
desktopmanager system sign-out --confirm

desktopmanager personalization capture --name before-theme-change
desktopmanager personalization list
desktopmanager personalization restore --name before-theme-change --skip-machine-policies
desktopmanager personalization apply --file settings.json

desktopmanager taskbar list
desktopmanager taskbar set --monitor-index 0 --position bottom --show
desktopmanager taskbar set-auto-hide --on

desktopmanager radio list
desktopmanager radio set --kind wifi --state on
desktopmanager radio airplane get --experimental

desktopmanager wifi interfaces
desktopmanager wifi profiles
desktopmanager wifi connect --profile "Corporate WiFi"

desktopmanager virtual-desktop current --handle <window-handle>
desktopmanager virtual-desktop id --handle <window-handle>
desktopmanager virtual-desktop move --handle <window-handle> --desktop-id <guid>

desktopmanager desktop slideshow
desktopmanager desktop start-slideshow
desktopmanager desktop set-slideshow-options
desktopmanager desktop advance-slideshow
desktopmanager desktop stop-slideshow

desktopmanager process start
desktopmanager process start-and-wait

desktopmanager screenshot desktop
desktopmanager screenshot window
desktopmanager screenshot target

desktopmanager target save
desktopmanager target get
desktopmanager target list
desktopmanager target resolve

desktopmanager visual save
desktopmanager visual get
desktopmanager visual list
desktopmanager visual assert
desktopmanager visual resolve

desktopmanager control-target save
desktopmanager control-target get
desktopmanager control-target list
desktopmanager control-target resolve

desktopmanager layout save
desktopmanager layout apply
desktopmanager layout assert
desktopmanager layout list

desktopmanager snapshot save
desktopmanager snapshot restore
desktopmanager snapshot list

desktopmanager diagnostic hosted-session

desktopmanager workflow prepare-coding
desktopmanager workflow prepare-screen-sharing
desktopmanager workflow clean-up-distractions

desktopmanager mcp serve
desktopmanager mcp serve --allow-mutations
desktopmanager mcp serve --allow-mutations --allow-system-settings
desktopmanager mcp serve --allow-experimental
desktopmanager mcp serve --allow-mutations --allow-process notepad
desktopmanager mcp serve --dry-run
```

## Current behavior

- `layout` stores named JSON files under `%AppData%\DesktopManager\layouts`.
- `snapshot` stores named JSON files under `%AppData%\DesktopManager\snapshots`.
- `screenshot` stores generated PNG files under `%AppData%\DesktopManager\captures` when `--output` is not provided.
- `target` stores reusable JSON target definitions under `%AppData%\DesktopManager\targets`.
- `visual` stores reusable JSON metadata plus PNG baseline images under `%AppData%\DesktopManager\visual-baselines`.
- `control-target` stores reusable JSON control selector definitions under `%AppData%\DesktopManager\control-targets`.
- `workstation` stores cohesive display, audio, personalization, and taskbar profiles under `%AppData%\DesktopManager\workstation-profiles`.
- `personalization` stores current-user snapshots under `%AppData%\DesktopManager\personalization` and can still read older snapshots from the legacy machine-wide directory.
- `audio` enumerates render and capture endpoints through Windows Core Audio. Default roles can be changed independently instead of always replacing console, multimedia, and communications together.
- `system` separates read-only power/session inspection from explicit lock, keep-awake, suspend, and sign-out actions. Suspend and sign-out require `--confirm`.
- `radio list` and `radio set` use the supported Windows radio API. Global airplane mode is separate and requires `--experimental` because its Windows shell COM contract is undocumented.
- `wifi interfaces` and `wifi profiles` list only Windows interfaces and already-saved profiles. `wifi connect` connects an exact saved profile and waits for ACM completion without scanning nearby networks, reading BSSIDs, exposing profile XML or credentials, or querying location-sensitive current-connection details.
- `virtual-desktop` exposes only the public window operations: current-desktop check, desktop ID lookup, and moving a window to a known desktop ID.
- `monitor list` reports the desktop-coordinate bounds used by monitor screenshots.
- `desktop slideshow` reports configured wallpaper slideshow images, state flags, options, shuffle state, and slideshow tick interval.
- `desktop start-slideshow` can replace the slideshow image set and optionally apply shuffle and tick settings in the same call.
- `desktop set-slideshow-options` updates shuffle and tick settings without replacing the slideshow images.
- snapshots currently reuse the window layout format and are therefore windows-only for now.
- `process start` launches a desktop application and can optionally wait for input idle and for a launched window to appear.
- `process start` can now also validate the launched window by title or class and optionally require that a real matching window be found before returning.
- `process start-and-wait` now packages the safer unattended launch flow: start the app, bind the follow-up wait to the launched process, return the resolved window result, and optionally capture before/after evidence.
- `process start-and-wait --follow-process-family` is an explicit opt-in for apps that surface their visible window from a same-name helper or broker process after launch-time correlation finishes.
- `window wait` polls for a matching window and returns when one appears.
- `window wait-visual-change` polls a matching window, client area, or saved target region until the pixels materially change, which makes opaque modern-app flows verifiable without depending on structural UIA exposure.
- `visual save` captures a reusable baseline for a whole window, the client area, or a saved target region so later runs can assert that the same UI still looks right.
- `visual assert` compares a live window, client area, or saved target region against a stored baseline and returns sampled change metrics instead of forcing agents to hand-roll image diffs.
- `visual resolve` searches a live window or client area for a previously saved baseline image and returns the best match coordinates, which makes saved visual regions reusable as anchors instead of screenshot-only artifacts.
- `visual read-text` runs Windows OCR over a live window, client area, or saved target region and returns recognized text plus line and word bounds.
- `visual resolve-text` turns that OCR output into a reusable text anchor by returning the best visible match coordinates for a query string.
- `window click --ocr-text <text>` turns that same OCR text-anchor path into a direct action, so visible labels can drive clicks without pre-saved targets or app-specific adapters.
- `window drag --start-ocr-text <text> --end-ocr-text <text>` and `window scroll --ocr-text <text>` extend that same OCR text-anchor path to richer pointer actions, so visually obvious surfaces can stay generic even when structure is weak.
- `window click --visual-baseline <name>` turns that saved visual region into a direct action path, so agents can click a previously seen button or panel without first hand-copying resolved coordinates.
- `window drag --start-visual-baseline <name> --end-visual-baseline <name>` and `window scroll --visual-baseline <name>` extend that same anchor model to richer pointer actions.
- anchor-driven window mutations now return the resolved region and action point they actually used, which makes debugging and agent retries much more concrete.
- `window exists` and `window active-matches` provide non-mutating verification commands.
- `control exists` and `control wait` provide the same inspect-first verification model for controls.
- `control assert-value` adds a stronger reusable assertion when a workflow depends on the resolved field content, not just control presence.
- `control diagnose` explains which discovery path was used, how many Win32 and UIA controls were actually found, and what each probed UIA root returned.
- `control diagnose` can now also take `--target <name>`, so saved control targets and ad-hoc selectors share the same diagnostics path.
- `control diagnose --action-probe` adds a read-only UIA action-resolution probe for the first matched UIA control, so you can verify cached action-match reuse without clicking anything.
- `control diagnose` now includes elapsed times for the overall diagnostic pass, and `--action-probe` adds a separate elapsed time for the read-only action-resolution probe.
- `control` works with child window controls and can also use UI Automation-oriented selectors.
- `control list` now returns shared control bounds metadata, which makes control discovery more actionable for follow-up clicks or diagnostics.
- `control list` also returns shared capability metadata so you can tell whether a control supports background-safe click, text, or key actions before invoking it.
- control selectors can now match `value`, `enabled`, and `focusable` state through the shared library.
- control selectors can now also match capability flags such as `background-click`, `background-text`, `background-keys`, and `foreground-fallback`.
- `--ensure-foreground` provides a shared opt-in reliability hint for UIA-heavy control queries.
- `control set-text` and handle-backed `control send-keys` now use shared direct-to-control message routing instead of relying on foreground focus.
- UIA control actions now reuse the same shared fallback-root search strategy as UIA discovery, which reduces “listed but not actionable” mismatches when modern apps expose controls under Chromium-style child roots.
- zero-handle UIA text and key fallback paths are now shared too, but they are intentionally opt-in because they rely on focused foreground input for modern apps.
- when zero-handle UIA text fallback is enabled, the shared library now prefers a focused replace-and-paste flow with verification before it falls back to raw typed input, which is notably more reliable for Chromium-style edit fields.
- `window type` sends text to the target window, either by simulated typing or clipboard paste.
- `window type --foreground-input` requires real foreground keyboard delivery and fails instead of silently falling back to background window messaging, which is a better fit for remote-session hosts such as RDP, Hyper-V, and Remote Desktop Manager.
- `window type --physical-keys` adds a layout-aware physical-key typing mode for foreground targets, which is often closer to how password managers "type" into hosted remote sessions.
- `window type --hosted-session` is a convenience profile for RDP, Hyper-V, and Remote Desktop Manager style targets. It enables a US-style foreground scancode path with slower defaults that are safer for hosted editors.
- `window type --script` preserves multiline formatting, chunks long lines into smaller typed segments, and can be combined with either the default delivery path or the stricter foreground typing modes.
- mutating `window` commands now support `--verify`, which re-queries the mutated window after the action and reports an observed postcondition instead of only the request outcome.
- `--verify-tolerance-px` tunes geometry verification for commands like `window move`; specifying it also implies `--verify`.
- the verification block is action-aware for `window move`, `window focus`, and `window minimize`, and falls back to honest presence-only observation for other window mutations such as typing and pointer input.
- hosted-session live diagnostics now write repo-local artifacts under `Artifacts\HostedSessionTyping`, including a raw JSON snapshot and a companion `*.summary.txt` file with the likely focus-culprit category and retry summary.
- `diagnostic hosted-session` reads the newest hosted-session artifact (or a specific one) and can return either the compact summary text or a structured record.
- hosted-session diagnostic artifacts now trim older entries automatically, keeping the newest artifact sets so the folder stays readable during repeated harness runs.
- `window keys` sends key chords or single keys to the target window after activating it, which is the safer shared follow-up path for Enter, Escape, and similar actions when modern controls stop being structurally reusable after text entry.
- mutating `window` and `control` commands can now return shared verification metadata: `success`, `elapsedMilliseconds`, `safetyMode`, optional target name/kind, best-effort before/after screenshots, artifact warnings, and for verified window mutations an explicit `verification` block with observed counts, summary text, and notes.
- those mutating commands now also accept `--capture-before`, `--capture-after`, and `--artifact-directory` so CLI, MCP, and agent workflows can ask for evidence without changing the core action logic.
- `workflow prepare-coding` can optionally apply a named layout and then focus a likely editor or terminal window.
- `workflow prepare-screen-sharing` can optionally apply a named layout, minimize common distractions, and then focus a likely sharing window.
- `workflow clean-up-distractions` exposes the same shared distraction-minimizing logic as a standalone structured step.
- workflow results can include `resolvedWindow` for the explicit target window when the workflow can resolve it, but callers should still treat focus and target resolution as best-effort and rely on `Notes` when Windows blocks the normal path.
- `layout assert` now verifies that the current desktop satisfies a saved named layout within configurable geometry tolerances and optional state matching, which makes saved layouts reusable as assertions instead of restore-only state.
- `window click`, `window drag`, and `window scroll` provide shared window-relative fallbacks for modern apps when structural control discovery is unavailable.
- `window wait-visual-change` is the matching non-mutating verification primitive for those fallback actions, so agents can wait for real visual change instead of guessing from timing alone.
- mutating `window` and `control` commands can now also ask for built-in visual-change verification, which waits for a real pixel delta after the action and returns the observed change metrics in the same structured result.
- `window` commands support exact handle targeting and active-window targeting for safer selection when multiple windows match.
- `window geometry` returns both outer-window and client-area bounds, which makes screenshot-assisted targeting much easier.
- `window click`, `window drag`, and `window scroll` now also support normalized ratios from `0` to `1` for less brittle targeting.
- `target save` lets you persist a reusable client-area or window-relative point once and reuse it from `window click`, `window drag`, and `window scroll`.
- `target save` can now also persist a reusable target area via `width`/`height` or `widthRatio`/`heightRatio`, which makes screenshot-assisted visual targeting much more reusable.
- `target resolve` shows the exact screen-space point a named target maps to for a live window.
- `screenshot target` and `screenshot window --target <name>` can now capture a resolved named target area directly.
- `control-target save` lets you persist a reusable control selector and capability profile once, then resolve it later against live windows.
- `control-target resolve` shows which live control a saved target matches, including its current capabilities and parent window.
- `control click`, `control set-text`, and `control send-keys` can now reuse a saved control target via `--target`.
- `control list`, `control exists`, and `control wait` can also reuse a saved control target via `--target`, which makes repeated modern-app inspection much less repetitive.
- the shared UIA layer now remembers a preferred root inside the current process after a successful modern-app lookup, and `control diagnose` exposes whether that preferred root was reused.
- the shared UIA layer now also keeps a very short-lived in-process cache of enumerated root controls, which helps repeated modern-app control reads and diagnostics in long-lived sessions.
- repeated UIA actions in the same long-lived process now also try a cached exact-match lookup before they fall back to a broader root walk.
- the shared control wait path now prefers already-seen matching window handles inside the same process before it falls back to broad rediscovery, which is safer for stable modern-app windows.
- `screenshot window` now prefers real window rendering before falling back to screen pixels, which improves captures for covered windows.
- `window type` still falls back to direct message-based delivery by default when Windows refuses to foreground the target window, which avoids leaking `SendInput` text into whatever app currently owns focus.
- `window type --foreground-input` disables that fallback and skips direct `WM_SETTEXT` verification, so it behaves more like deliberate keyboard typing than background control mutation.
- `window type --physical-keys` builds on the strict foreground path and prefers real keyboard-layout key combinations before it falls back to Unicode packets for characters that have no physical-key mapping.
- `window type --hosted-session` currently wraps a foreground US-style scancode path with slower pacing defaults. It requires the hosted editor surface to already own focus before typing starts, and it now aborts immediately if foreground ownership changes while typing.
- `window type --script --foreground-input` is the preferred shared path when you need to type a multiline script into an RDP, Hyper-V, or Remote Desktop Manager hosted editor without relying on clipboard paste.
- when the hosted-session harness goes inconclusive, inspect the matching `Artifacts\HostedSessionTyping\*.summary.txt` companion first. It now calls out whether the interruption looked like a repeated browser/Electron focus steal, mixed contention, or no retained external culprit.
- `process start` now prefers windows from the launched process and then newer post-launch window handles for the target app, which is safer than binding to any older matching window.
- `process start --require-window` is now a useful shared primitive for unattended workflows that need a validated target window instead of a best-effort launcher result.
- `mcp serve` hosts a stdio MCP server.
- `mcp serve` now defaults to read-only inspection so agents can connect safely before any mutation is enabled.
- `mcp serve --allow-mutations` enables mutating MCP tools for an intentional session.
- `mcp serve --allow-system-settings` is an additional gate for audio, power/session, personalization, taskbar, profile application, radio, and airplane-mode mutations.
- `mcp serve --allow-experimental` exposes the undocumented global airplane-mode read/write tools; supported per-radio inventory does not require it.
- `mcp serve --allow-process <pattern>` and `--deny-process <pattern>` constrain live desktop mutations to specific process patterns.
- `mcp serve --allow-foreground-input` is a second explicit opt-in for zero-handle UIA text/key fallback that may need focused foreground input.
- `mcp serve --dry-run` previews mutating tool calls without changing desktop or saved state.
- when process filters are active, broad layout/snapshot/workflow mutations that cannot be scoped to one process are intentionally blocked.

## Why this shape

- `window`, `monitor`, `layout`, and `snapshot` scale better than flat verbs.
- `process` and `screenshot` add the first inspect-launch-wait loop needed for desktop automation.
- `process start-and-wait` turns that inspect-launch-wait loop into one shared structured result instead of leaving the correlation logic to every caller.
- `control` and `window type` add the first direct interaction layer for classic desktop controls.
- `window keys` rounds out the shared whole-window input path for accelerators and commit keys without forcing agents back into ad-hoc foreground hacks.
- `window click`, `window drag`, and `window scroll` give CLI, MCP, and PowerShell the same coordinate-based fallback path when UIA-heavy apps stay opaque.
- `target` turns screenshot-assisted coordinate fallback into reusable state instead of one-off manual ratios.
- `visual` turns screenshot-assisted verification into reusable state instead of making every workflow keep one-off reference images and custom compare logic.
- the same saved visual baseline can now double as a reusable template anchor, so agents can relocate a previously seen button or panel before clicking it.
- that same anchor can now drive `window click` directly, which is a better fit for generic operator loops than forcing a separate resolve step in every caller.
- the same anchor family now also drives drag and scroll, which means a previously saved visual region can remain useful even when the next interaction is not a simple click.
- area-capable `target` definitions now let the shared core reuse visual regions, not just click points.
- `control-target` turns modern-app control discovery into reusable state instead of repeating long UIA selector sets each time.
- `workflow` packages a few multi-step desktop routines into shared structured results instead of leaving them as prompts or one-off agent logic.
- `layout assert` turns named layouts into reusable verification assets, not just restore assets.
- when a saved control target points at a modern Chromium-style app, the first resolution can still take a couple of seconds because shared UIA discovery is the expensive part of the workflow.
- those fallbacks now also support client-area coordinates, which are usually a better fit for browser and editor content than raw outer-window coordinates.
- screenshot JSON now includes window geometry metadata for window captures, so agents can map screenshots to client-area coordinates without extra probing.
- the CLI mirrors existing concepts already present in the library and PowerShell module.
- the CLI and MCP server reuse the same desktop operations and storage conventions.
- window selection, control geometry, and text-entry reliability now live in the shared C# library so CLI, MCP, and PowerShell stay aligned.

## Current Limits

- Child-window targeting is still the simplest path for classic Win32 controls.
- UIA discovery and action fallback now work through the shared library, but selector validation is still wise before unattended runs.
- `control diagnose` is the fastest way to understand why a modern app did or did not expose controls through the shared library, because it now shows per-root UIA probe details instead of only a single aggregate count.
- preferred UIA root reuse only helps inside a long-lived process like MCP or an in-process wait loop. Separate one-shot CLI invocations still start fresh.
- the short-lived UIA control cache is also process-local, so it mainly helps MCP, in-process waits, and repeated diagnostics inside the same host session.
- For opaque modern apps, the most reliable fallback flow is now: `screenshot window --json`, inspect `Geometry`, then use ratio-based `window click`, `window drag`, or `window scroll` with `--client-area`.
- For opaque modern apps, pair that fallback with `window wait-visual-change` for immediate feedback and `visual save` / `visual assert` when the workflow needs a reusable “still looks right” checkpoint across runs.

## Screenshot-Assisted Target Flow

When a modern app exposes unstable structure, prefer this repeatable flow:

```text
desktopmanager screenshot window --process msedge --json
desktopmanager target save edge-editor-pane --x-ratio 0.1 --y-ratio 0.15 --width-ratio 0.8 --height-ratio 0.7 --client-area
desktopmanager target resolve edge-editor-pane --process msedge --json
desktopmanager screenshot target edge-editor-pane --process msedge --json
desktopmanager window click --process msedge --target edge-editor-pane
desktopmanager window wait-visual-change --process msedge --target edge-editor-pane --timeout-ms 5000 --json
desktopmanager visual save edge-editor-clean --process msedge --target edge-editor-pane --json
desktopmanager visual resolve edge-editor-clean --process msedge --client-area --max-average-difference 10 --json
desktopmanager visual read-text --process msedge --client-area --json
desktopmanager visual resolve-text "Sign in" --process msedge --client-area --contains --json
desktopmanager window click --process msedge --ocr-text "Sign in" --ocr-contains --client-area --json
desktopmanager window drag --process msedge --start-ocr-text "Source" --end-ocr-text "Drop here" --ocr-contains --client-area --json
desktopmanager window scroll --process msedge --ocr-text "Timeline" --delta -120 --ocr-contains --client-area --json
desktopmanager window click --process msedge --visual-baseline edge-editor-clean --client-area --baseline-max-average-difference 10 --json
desktopmanager window scroll --process msedge --visual-baseline edge-editor-clean --delta -120 --client-area --baseline-max-average-difference 10 --json
desktopmanager visual assert edge-editor-clean --process msedge --target edge-editor-pane --max-changed-ratio 0.01 --json
```

For reusable drags or scrolling, save more than one target and then reuse them from `window drag` or `window scroll` instead of repeating raw coordinates.

When you want mutation evidence too, add artifact flags to the action step:

```text
desktopmanager control set-text --window-process msedge --target edge-address --text "https://evotec.xyz" --allow-foreground-input --capture-before --capture-after --json
desktopmanager window click --process msedge --target edge-editor-center --capture-before --capture-after --artifact-directory .\artifacts --json
```

For hosted-session diagnostics, prefer the summary first and then fall back to the full record only when needed:

```text
desktopmanager diagnostic hosted-session --summary-only
desktopmanager diagnostic hosted-session --repository-root C:\Support\GitHub\DesktopManager
desktopmanager diagnostic hosted-session --artifact C:\Support\GitHub\DesktopManager\Artifacts\HostedSessionTyping\sample.json --json
```
