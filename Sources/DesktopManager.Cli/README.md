# DesktopManager CLI and MCP server

Inspect Windows applications, windows and controls, capture screenshots, and perform explicitly enabled desktop actions. The CLI runs in the current user's interactive Windows session.

Use the Windows portable CLI produced by the repository's [PowerForge build](../../Build/Build-Project.ps1). Extract the complete CLI archive to an application folder and add that folder to the PATH inherited by your MCP client. Verify the installation from another directory:

```powershell
desktopmanager --help
desktopmanager mcp serve --json
```

The second command reports the server's startup policy without opening the MCP transport. An interactive desktop is required for UI inspection and input. The tool does not supply a remote desktop session or bypass Windows elevation boundaries.

The Windows/WPF CLI is distributed as a portable executable; .NET's `PackAsTool` does not support that target. A library NuGet package does not install this executable.

Run `desktopmanager mcp serve` as the local STDIO command in an MCP client, or install the [Windows verification plugin](../../plugins/desktopmanager-windows-verification/README.md) for bundled skills. The server starts read-only. To enable an authorized interaction, restart it with `--allow-mutations` and a narrow `--allow-process` pattern. Focused fallback input additionally requires `--allow-foreground-input`; system settings also require `--allow-system-settings`.

See the [MCP guide](../../Docs/DesktopManager.Mcp.md) for available tools, process restrictions and desktop-session limitations.
