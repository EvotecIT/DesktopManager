# DesktopManager

Windows desktop automation in C#: monitors, wallpapers, windows, controls,
input, devices, sessions and layouts. Ships as a NuGet library, a binary
PowerShell module, the `desktopmanager` CLI with an MCP server, and a WinUI
tray app with an MSI installer.

## Layout

- `Sources/DesktopManager.sln`: every project lives under `Sources/`.
  - `DesktopManager/`: the library. `Shared/` and `WindowTextHelper32/` support it.
  - `DesktopManager.PowerShell/`: binary cmdlets (`Cmdlet<Verb><Noun>.cs`).
  - `DesktopManager.Cli/`: CLI and MCP server (`desktopmanager mcp serve`).
  - `DesktopManager.App/`, `DesktopManager.App.Core/`, `DesktopManager.HotkeyHost/`:
    the WinUI tray app.
  - Tests: `DesktopManager.Tests/` (MSTest, Windows), `DesktopManager.Portable.Tests/`
    (cross-platform), `DesktopManager.AotSmoke/`. `DesktopManager.TestApp/` and
    `DesktopManager.WinUiHarness/` are UI test targets.
- Module root: `DesktopManager.psd1`, `DesktopManager.psm1`, `DesktopManager.Tests.ps1`,
  Pester tests in `Tests/`, examples in `Examples/`.
- `Build/`: PowerForge build scripts and `project.build.json`;
  `powerforge.dotnetpublish.json` (root) drives CLI, app and MSI publishing.
- `Docs/`: cmdlet reference (generated) plus hand-written guides
  (`DesktopManager.*.md`, `Build.Runbook.md`).
- `plugins/desktopmanager-windows-verification/`: Claude and Codex agent plugin
  bundling the MCP server and verification skills.
- `Website/content/`: curated docs and examples for the project site.

## Build and test

```powershell
dotnet build Sources/DesktopManager.sln -c Release
dotnet test Sources/DesktopManager.Tests/DesktopManager.Tests.csproj -c Release --no-build
dotnet test Sources/DesktopManager.Portable.Tests/DesktopManager.Portable.Tests.csproj -c Release
./DesktopManager.Tests.ps1                                            # Pester, after building
./Build/Build-Project.ps1 -Plan                                       # preview package and publish work
./Build/Build-Module.ps1 -ConfigurationGateMode Build -SignModule:$false -SkipInstall
./Build/Build-Project.ps1 -BuildModule:$false                         # CLI, app and installer outputs only
./Build/Build-DesktopManagerApp.ps1                                   # tray app, no installer
./Build/Build-DesktopManagerApp-MSI.ps1                               # tray app MSI
```

- Targets: library `net472;netstandard2.0;net8.0-windows10.0.19041.0;net10.0-windows10.0.19041.0`;
  cmdlets and tests `net472` plus both Windows TFMs; CLI `net8.0`/`net10.0`
  Windows TFMs; app `net10.0-windows10.0.19041.0`. The TFM properties live in
  `Directory.Build.props`, which also turns warnings into errors.
- `Build/Build-Project.ps1` is the main entry point. Narrow it with `-Target`,
  `-Runtimes win-x64`, `-Styles` and `-SkipInstallers`. See `Docs/Build.Runbook.md`.
- Release only: `-ConfigurationGateMode Publish` on either build script signs and
  publishes.

## Generated files

- Cmdlet pages in `Docs/` (`<Verb>-<Noun>.md` and `Docs/Readme.md`) come from the
  cmdlet XML docs. Regenerate with
  `./Build/Build-Module.ps1 -ConfigurationGateMode Documentation -SignModule:$false`.
- Refresh `DesktopManager.psd1` with
  `./Build/Build-Module.ps1 -ConfigurationGateMode Manifest -SignModule:$false`
  rather than editing it.

## Conventions

- C#: K&R braces, braces on every block, file-scoped namespaces; follow `.editorconfig`.
  Prefer smaller files and `partial` classes over large ones.
- Cmdlets use XML docs that PowerForge turns into help: `<summary>` for the
  synopsis, `<para type="description">` on parameters, and `<example>` blocks
  with `<summary>`, `<prefix>PS&gt; </prefix>`, `<code>` and `<para>`.
- PowerShell scripts and tests: support Windows PowerShell 5.1 and PowerShell 7,
  K&R/OTBS braces, full parameter names, no `+=` array building.
- Commit and PR titles use Conventional Commits with Gitmoji:
  `fix(threading): 🐛 Bound STA startup and worker waits`.

## Gotchas

- UI tests change the real desktop and are skipped by default. Opt in with
  `DESKTOPMANAGER_RUN_UI_TESTS=true`; desktop-changing, owned-window, system,
  foreground and external-app tests need their own
  `DESKTOPMANAGER_RUN_{DESTRUCTIVE,OWNED_WINDOW,SYSTEM,FOREGROUND,EXTERNAL}_UI_TESTS=true`.
  Don't enable them on a desktop someone is using.
- Most of the library is Windows-only. Only `DesktopManager.Portable.Tests`
  runs on Linux and macOS.
- Website examples must be safe to read before running; don't publish
  screenshots or machine-specific monitor layouts there.

## Repository skills

In `.agents/skills/`:

- `desktopmanager-build`: build, package and verify NuGet, module, CLI/MCP, app
  and MSI outputs through the PowerForge entry points.
- `desktopmanager-operator`: operate and inspect the Windows desktop through
  DesktopManager, MCP first and CLI second.
