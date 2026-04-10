# DesktopManager Automation Basics

This document tracks the baseline automation capabilities that need to feel solid before we trust larger app-verification workflows.

The goal is simple:

1. find the window
2. find the meaningful controls inside it
3. act on those controls directly
4. verify the state change structurally
5. capture honest visual evidence of the same result

## Why This Matters

Higher-level app verification only feels reliable when the low-level primitives are reliable first.

That means DesktopManager should be able to handle AutoIt-style fundamentals for purpose-built test surfaces:

- wait for a specific window
- enumerate text boxes, buttons, checkboxes, combo boxes, and similar controls
- click a checkbox and verify the checked state changed
- focus a text box and write directly into it without coordinate guessing
- invoke a button and verify the outcome
- capture the same live window visually without returning a black artifact

## Surface Ladder

We should certify DesktopManager progressively, from easy to hard:

### 1. Classic Controls

These are the baseline certification surfaces and should become boringly reliable.

- WinForms `TextBox`
- WinForms `CheckBox`
- WinForms `ComboBox`
- WinForms `Button`
- drag-and-drop targets

Current direction:

- use `DesktopManager.TestApp` as the first certification harness
- store control bounds and state in the status snapshot
- cover discovery, click, set-text, send-keys, and verification end to end

Current certified basics:

- direct text mutation for classic edit controls
- direct checkbox toggle with structural state verification
- direct combo-box selection with structural selected-item verification
- direct button invoke against the live certification harness
- live discovery of a dedicated WinUI-backed harness editor and apply button through UI Automation
- verified WinUI-backed editor mutation through DesktopManager with status-backed confirmation
- verified WinUI-backed checkbox toggle through DesktopManager with UIA-backed state confirmation
- verified WinUI-backed picker selection through DesktopManager with status-backed confirmation
- verified WinUI-backed button invoke through DesktopManager with status-backed confirmation
- verified selector-based CLI and MCP button invoke on the WinUI harness without requiring a raw control handle
- verified selector-based CLI and MCP checkbox mutation on the WinUI harness without requiring a raw control handle
- verified selector-based CLI and MCP picker selection on the WinUI harness without requiring a raw control handle
- live discovery of the hosted WPF command bar as an editable UI Automation control
- live discovery of the hosted WebView2 shell as a WPF host plus browser-backed child root
- live discovery of inner WebView2 Chrome controls such as `prompt` and `send`
- verified inner WebView2 prompt mutation through DesktopManager with post-action value confirmation
- verified inner WebView2 button invoke through DesktopManager with DOM-backed status confirmation
- WinUI-hosted editor capture parity against live desktop pixels
- WPF-hosted command bar capture parity against live desktop pixels
- WebView2-hosted browser surface capture parity against live desktop pixels
- built-in visual-change verification on mutating window and control actions so public DesktopManager results can prove visible UI reaction, not only structural state
- reusable visual baselines for saved “still looks right” checkpoints across runs, not just one-shot before/after evidence
- reusable visual-baseline resolution so a previously saved visual region can be found again as a generic anchor inside the current window
- direct click reuse of saved visual baselines, so a previously certified region can drive an action without dropping back to manual coordinates
- direct drag and scroll reuse of saved visual baselines, so the same saved region can support richer pointer flows without fresh coordinate work
- anchor-driven actions now publish the resolved regions and action points they used, so “what was seen” and “what was acted on” are easier to line up
- Windows OCR-backed text extraction over live window, client-area, and target captures
- OCR-backed text-anchor resolution, so visible labels can become generic action coordinates without app-specific adapters
- direct click reuse of OCR-resolved visible text, so a visible label can drive a live action without a saved target or saved baseline
- direct drag and scroll reuse of OCR-resolved visible text, so the same text-anchor path can support richer pointer flows on visually obvious surfaces
- built-in visual-change verification on OCR-driven click, drag, and scroll flows, so a single result can prove both what was targeted and that the pixels actually changed

### 2. WPF Controls

These prove the UIA-oriented path beyond classic child-window handles.

- WPF `TextBox`
- command-style focused input
- explicit foreground-input fallback when required

Current direction:

- keep the existing command bar surface
- prove both blocked and allowed foreground-input policies

### 3. WinUI / WebView / Chromium Surfaces

These are the modern hard case and should be treated as a separate certification stage, not as the first proving ground.

- WinUI-hosted surfaces
- WebView2 content
- Chromium-style editors and buttons

Current observed gaps:

- meaningful inner controls may not be exposed through UIA on arbitrary third-party modern apps
- control enumeration may stop at shell or container panes when the app does not publish stable automation surfaces
- browser-shell discovery now proves inner DOM edit and button actionability on the repo-owned WebView2 harness
- direct window rendering can return black artifacts even when the window is visibly present on screen
- foreground typing can report success without visibly changing the intended editor

Current mitigation:

- suspiciously black `PrintWindow` captures now fall back to live desktop pixels for the same window bounds
- UI Automation direct-value writes now verify the resolved post-action value before reporting success
- the dedicated MAUI/WinUI harness now publishes structural state for editor, checkbox, picker, and apply-button verification
- the WebView2 test harness now publishes DOM prompt/status state back to DesktopManager for structural verification
- repo-owned discovery and action coverage now certifies a dedicated WinUI-backed harness for direct edit and button automation
- repo-owned public-path coverage now certifies selector-driven checkbox and picker mutations through the CLI/MCP surface
- repo-owned public-path coverage now certifies selector-driven modern button invoke through the CLI/MCP surface
- repo-owned parity coverage now certifies a WinUI-backed editor surface against desktop-crop evidence
- repo-owned discovery coverage now certifies the hosted WPF command bar as a concrete editable UI Automation target
- repo-owned discovery coverage now certifies a WebView2-backed host shell plus browser-root enumeration path
- repo-owned parity coverage now certifies the hosted WPF command bar surface against desktop-crop evidence
- repo-owned parity coverage now certifies a WebView2-backed hosted browser surface against desktop-crop evidence

## Certification Rules

DesktopManager should not consider a surface "ready" until it can prove all of the following on that surface:

- `window wait` resolves the live window
- `control list` or `control diagnose` finds the actionable target
- the action works without coordinate guessing when the structure is available
- the post-action state is verified structurally
- the artifact proves the same visible state change

If structure is weak, the fallback path should still be explicit and honest:

- say that the target is coordinate-driven
- say that foreground input is required
- say that verification is visual-only when structure is unavailable
- never treat a black screenshot as a successful evidence artifact

## Immediate Work

- keep `DesktopManager.TestApp` as the main classic-control certification harness and grow it only when a new primitive needs proof
- keep the separate WinUI harness small and boring so modern-surface regressions stay easy to isolate
- add a screenshot parity test that compares window-render capture with desktop-crop capture
- extend the same self-reporting certification style to more WinUI-hosted opaque surfaces
- make mutating actions return clearer "reported success vs verified success" distinctions for opaque surfaces
- keep extending the public mutate-and-visually-verify path so common click/type/key workflows do not need custom sleeps or app-specific wrappers
- keep adding reusable visual state primitives so agents can persist expected UI regions and assert them later without app-specific code
- keep turning those reusable visual regions into actionable anchors, so the next step after capture can be “locate this again” before we add OCR or model-driven clicks
