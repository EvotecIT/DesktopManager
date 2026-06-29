---
name: desktopmanager-build
description: Build, package, and verify DesktopManager NuGet, PowerShell module, and CLI/MCP artefacts through the repo-standard PowerForge entrypoints. Use when changing Build/Build-Project.ps1, Build/Build-Module.ps1, Build/project.build.json, powerforge.dotnetpublish.json, module packaging, CLI publish outputs, release assets, or build/operator documentation.
---

# DesktopManager Build

Use this skill when the task touches how DesktopManager is built, packaged, or published.

## Golden Path

1. Use the repo entrypoint first:
   `.\Build\Build-Project.ps1`
2. Prefer plan mode before changing packaging behavior:
   `.\Build\Build-Project.ps1 -Plan`
3. For PowerShell module-only work, use:
   `.\Build\Build-Module.ps1 -SkipInstall`
4. For CLI/app publish verification, use:
   `.\Build\Build-Project.ps1 -Build:$false -BuildModule:$false`
5. For the daily WinUI tray app without installers, use:
   `.\Build\Build-DesktopManagerApp.ps1`
6. For the generated DesktopManager app MSI, use:
   `.\Build\Build-DesktopManagerApp-MSI.ps1`
   The MSI wrapper publishes `DesktopManager.App` as `PortableCompat`.
7. When checking a specific runtime publish, pass:
   `-Runtimes win-x64`

## Decision Rules

- Treat `Build/Build-Project.ps1` as the primary repo build and release entrypoint.
- Prefer changing `Build/project.build.json` and `powerforge.dotnetpublish.json` over hardcoding publish behavior in ad-hoc scripts.
- Prefer `Build/Build-Module.ps1` for PowerShell packaging behavior instead of editing checked-in artefacts by hand.
- Prefer the app/MSI wrapper scripts for daily desktop app packaging instead of invoking generated WiX files directly.
- Verify the resulting behaviour from the real surfaces:
  NuGet/library, PowerShell import, CLI help, `desktopmanager mcp serve`, `DesktopManager.App.exe`, and MSI plan/build output.
- Keep docs aligned with the actual build flow:
  `Docs/Build.Runbook.md`, `README.MD`, and repo-local skills.
- When packaging or publish output changes, check the expected output locations before concluding:
  `Artefacts/ProjectBuild`, `Artefacts/Unpacked`, `Artefacts/Packed`, `Artefacts/PowerForge/DesktopManager`, `Artefacts/PowerForge/DesktopManager.App`, and `Artefacts/PowerForge/Msi/DesktopManager.App`.

## Reference Files

- `Build/Build-Project.ps1`
- `Build/Build-Module.ps1`
- `Build/Build-DesktopManagerApp.ps1`
- `Build/Build-DesktopManagerApp-MSI.ps1`
- `Build/project.build.json`
- `powerforge.dotnetpublish.json`
- `Docs/Build.Runbook.md`
- `README.MD`
