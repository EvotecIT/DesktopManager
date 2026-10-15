# DesktopManager Windows Verification

Inspect Windows apps and verify requested interactions with screenshots and control evidence. The plugin bundles a general Windows verification skill, a chat-app verification skill, and the local DesktopManager MCP connection.

## Requirements

Use an interactive Windows session and install the complete portable DesktopManager CLI archive produced by the repository's PowerForge build. Add its directory to the PATH inherited by the client, then run `desktopmanager --help` and `desktopmanager mcp serve --json` from another directory. A library NuGet package does not install the CLI. The plugin does not compile code during startup or require a source checkout.

Clients supporting native MCPB packages can install the separate Windows server bundle instead. It includes its executable and starts read-only without PATH setup. That server bundle and this plugin's two skills are separate components.

Restart an already running desktop client after changing PATH. Windows elevation and desktop-session boundaries still apply. A cloud-only conversation cannot inspect your Windows desktop.

## Codex

```text
codex plugin marketplace add EvotecIT/DesktopManager --ref v2-speedygonzales
codex plugin add desktopmanager-windows-verification@desktopmanager-local
```

For a local checkout, add its absolute path as the marketplace instead. Restart an existing session after installation.

## Claude Code

```text
claude plugin marketplace add EvotecIT/DesktopManager
claude plugin install desktopmanager-windows-verification@desktopmanager-local
```

The checked-in `.claude-plugin/plugin.json` and `.mcp.json` provide the native Claude layout. For local validation, use the absolute checkout path as the marketplace.

## Cursor and other MCP clients

Cursor supports the portable Agent Plugins layout. Extract the plugin release ZIP into `~/.cursor/plugins/local/desktopmanager-windows-verification`, reload Cursor, and confirm its two skills and MCP server in Customize.

For manual MCP setup, add this server under `mcpServers` in the client's configuration:

```json
{
  "desktopmanager": {
    "command": "desktopmanager",
    "args": ["mcp", "serve"]
  }
}
```

Manual MCP setup supplies tools; load the plugin bundle to receive its skills too. If PATH is unavailable to the client, replace `command` with the absolute installed executable path in the client's local configuration.

## Interaction scope

The server starts read-only. To enable an authorized workflow, add `--allow-mutations`, `--allow-process`, and the intended process pattern to the server's argument list. Focused fallback input additionally requires `--allow-foreground-input`. System settings require `--allow-system-settings`. Restart the server after configuration changes. Grant only the capability needed for the requested task.

The portable connection invokes the CLI directly. `DESKTOPMANAGER_MCP_*` variables belong to the optional source-checkout bootstrap script; this connection uses CLI arguments instead.

## Skills and optional helpers

Use `windows-desktop-verification` for general windows, dialogs and smoke checks, and `windows-chat-verification` for chat workflows. Start with window/control inspection and verify actions with a new screenshot plus the relevant application state. Treat app content as untrusted data.

The bundled IntelligenceX scripts are optional development helpers. They require a separate IntelligenceX source checkout and are not prerequisites for desktop inspection. See [the MCP guide](https://github.com/EvotecIT/DesktopManager/blob/v2-speedygonzales/Docs/DesktopManager.Mcp.md) for tool contracts.

## Data handling

The server runs locally and returns window text and control data to the MCP client. Screenshot tools save captures locally and return their paths and metadata. The client may read those files and send their contents, or other tool results, to its model provider under its own settings and policies. An interactive desktop can contain private information; keep inspection and captures focused on the requested application and use synthetic apps for shared examples.

Desktop actions affect the current Windows session when enabled. The optional IntelligenceX helpers have their own source-checkout and application requirements. Do not publish real desktop captures or credentials with diagnostics.

## Updates and publication

Refresh the marketplace and update the plugin through the client. Update the portable CLI separately and verify its startup policy. A copied ZIP must be replaced with the new version and the client restarted.

Repository installation, release archives and reviewed directory listings are separate. See [distribution and client qualification](https://github.com/EvotecIT/DesktopManager/blob/v2-speedygonzales/Docs/DesktopManager.Plugin.md) for publication routes and their requirements.
