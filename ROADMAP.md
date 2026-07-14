# DesktopManager Roadmap

This roadmap tracks the remaining product work that belongs in DesktopManager. Completed implementation history belongs in releases and pull requests, not in a growing checklist.

## Current priorities

- [ ] Define a stable remote-session contract for desktop, monitor, window, and control attachment. DesktopManager should own capture, coordinates, input, and evidence primitives; transport and controller UX should remain in their owning products.
- [ ] Add stream-friendly capture with frame metadata, throttling, and changed-frame delivery while keeping one-shot screenshots as the evidence fallback.
- [ ] Finish logical-to-physical coordinate mapping for mixed-DPI, multi-monitor, client-area, and preview-space operations.
- [ ] Expand capability policy and audit records for view, input, control text, control keys, session lifecycle, and foreground-stealing operations.
- [ ] Add contract-focused end-to-end scenarios proving that the observed target, performed action, and captured result agree across Win32, UI Automation, WebView2, and WinUI surfaces.
- [ ] Publish and consume the PowerForge package version that validates the complete CLI, app, MSI, NuGet, and PowerShell-module artifact path in CI.

## Product boundaries

- Keep reusable desktop behavior in the C# core. PowerShell, CLI, MCP, the tray app, and remote adapters should remain thin surfaces.
- Use supported Windows APIs. Per-monitor display-scale mutation is intentionally not exposed because Windows has no supported public setter for that user setting.
- Keep system-wide and foreground-input tests opt-in. Default validation must be safe to run on a developer workstation.
- Avoid consumer-local packaging logic. Build, package, installer, signing, and publication behavior belongs in PowerForge/PSPublishModule.

## Later opportunities

- Virtual desktop creation, switching, and window moves.
- GPU, adapter, color-profile, and EDID inventory.
- Remote Desktop and multi-session-aware routing.
- DWM thumbnails and preview effects where they provide a concrete operator workflow.
