# DesktopManager agent plugin distribution

The [Windows verification plugin](../plugins/desktopmanager-windows-verification/README.md) bundles two user skills and the local DesktopManager MCP connection. Contributor skills in `.agents/skills` guide work on DesktopManager source; they are not distributed as user workflows.

`plugins/desktopmanager-windows-verification/plugin.json` and `mcp.json` are canonical. PowerForge generates the Codex and Claude compatibility files. The repository supplies both `.agents/plugins/marketplace.json` and `.claude-plugin/marketplace.json` for direct installation.

## Runtime and access

Install the complete portable Windows CLI archive and add its directory to the client's PATH. The plugin starts `desktopmanager mcp serve`, with read-only defaults. It does not build source or require an IntelligenceX checkout. The Windows/WPF target is not supported by .NET's `PackAsTool`; the library's NuGet package is not an MCP executable installer.

Mutation flags belong in the MCP server's arguments. Use `--allow-mutations` with a narrow `--allow-process` pattern when the task needs interaction. Focused fallback input requires `--allow-foreground-input`; system settings additionally require `--allow-system-settings`. See the [MCP contract](DesktopManager.Mcp.md).

The optional IntelligenceX helper scripts support development verification with a separate source checkout and an app-owned automation contract. They are not part of the general application's startup path. Keep screenshots and control evidence alongside any app-owned result; neither an input acknowledgment nor a pipe response alone establishes visible behavior.

## Build and qualify

Use the existing PowerForge entrypoint to produce the CLI:

```powershell
./Build/Build-Project.ps1 -BuildModule:$false -Target DesktopManager.Cli.net10 -Runtimes win-x64 -SkipInstallers
```

The CLI target has no signing profile. Treat its validation output as unsigned. Apply any required release-signing policy before public delivery; ordinary validation does not authorize access to a hardware-backed key.

Use a source build of PowerForge containing `agent-plugin`, as described by its CLI documentation. From this checkout, invoke that CLI with:

```text
agent-plugin sync --source plugins/desktopmanager-windows-verification
agent-plugin validate --source plugins/desktopmanager-windows-verification
agent-plugin pack --source plugins/desktopmanager-windows-verification --out Artefacts/AgentPlugins
```

These are PowerForge subcommands, not standalone executables. Increment the plugin version for every published bundle change. Keep the plugin name and marketplace selector stable. Preserve existing versioned ZIPs and verify the sidecar independently.

Qualify the copied CLI and plugin outside the source tree: initialize MCP, list its tools, check read-only defaults and mutation rejection, inspect a synthetic test application, capture its window, and verify an explicitly enabled interaction. Install from fresh Codex and Claude profiles. Reload the portable plugin in Cursor and confirm both skills and its MCP server. Verify the update path for an existing installation. An unavailable client or unexercised interaction remains a qualification gap.

The Agent Plugin Package workflow packages the plugin and portable CLI as CI artifacts. This does not publish a product release. Public delivery needs both archives and their hashes available at a stable versioned release URL, followed by a fresh install from those public files.

## Directory submissions

- **OpenAI:** [desktop-only local MCP submissions](https://developers.openai.com/plugins/deploy/desktop-only) require an approved partner and an allowlisted publishing organization. Complete identity verification, confirm the runtime packaging requirements, upload the qualified ZIP, resolve findings, and submit for review. Publish after approval and verify the exact-name listing. The current plugin expects an installed CLI; confirm whether review requires a bundle containing the server and dependencies before submitting.
- **Claude:** the repository marketplace supports direct Claude Code installation. Wider discovery uses the [Anthropic directory portal](https://claude.ai/directory/manage), following [the publication guide](https://code.claude.com/docs/en/plugins/publish). Check Windows/local-execution component support for each Claude surface. The directory portal and `claude-plugins-official` have different admission routes.
- **Cursor:** test the portable bundle under `~/.cursor/plugins/local/desktopmanager-windows-verification`, then follow [Cursor's plugin documentation](https://cursor.com/docs/plugins) and [marketplace submission](https://cursor.com/marketplace/publish). A local copy and a reviewed marketplace listing are separate.
- **MCP Registry:** publish metadata only for a supported, independently installable server package. DesktopManager's ordinary library package cannot be declared as a runnable `dnx` server. Qualify an accepted native package format before adding a Registry entry; do not advertise the library as an executable tool.

Prepare concise metadata, publisher-owned privacy information, support contact, supported Windows versions, runtime/interactive-session requirements, synthetic review scenarios, checksums and an immutable source revision. Do not use GitHub's privacy policy as the product's policy or include real desktop captures with private information. Keep submission identifiers, feedback, approved versions and verified listing URLs in the release handoff.
