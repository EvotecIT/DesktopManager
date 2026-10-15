<#
.SYNOPSIS
Creates MCP Registry metadata bound to a DesktopManager MCPB release artifact.
.DESCRIPTION
Reads the bundled version and hashes the actual MCPB file. Writes metadata for
the matching versioned GitHub release without authenticating or publishing.
.PARAMETER PackagePath
The qualified DesktopManager MCPB archive to publish.
.PARAMETER OutputPath
Where to write the generated server.json outside the source tree.
.EXAMPLE
./Build/McpBundle/New-RegistryMetadata.ps1 -PackagePath ./Artefacts/PowerForge/Mcp/desktopmanager-mcp-win-x64.mcpb -OutputPath ./Artefacts/AgentPlugins/server.json
.OUTPUTS
The path of the generated MCP Registry metadata file.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $PackagePath,
    [Parameter(Mandatory = $true)]
    [string] $OutputPath
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $PackagePath).Path)
try {
    $reader = [IO.StreamReader]::new($archive.GetEntry('manifest.json').Open())
    try {
        $manifest = $reader.ReadToEnd() | ConvertFrom-Json
    } finally {
        $reader.Dispose()
    }
} finally {
    $archive.Dispose()
}

if ($manifest.name -ne 'desktopmanager-windows-verification' -or $manifest.version -notmatch '^\d+\.\d+\.\d+$') {
    throw 'Expected a versioned DesktopManager Windows Verification MCPB archive.'
}

$server = [ordered]@{
    '$schema' = 'https://static.modelcontextprotocol.io/schemas/2025-12-11/server.schema.json'
    name = 'io.github.EvotecIT/desktopmanager'
    title = 'DesktopManager Windows Verification'
    description = 'Inspect and verify Windows x64 desktop apps through a local, read-only-by-default MCP server.'
    version = $manifest.version
    repository = @{ url = 'https://github.com/EvotecIT/DesktopManager'; source = 'github' }
    packages = @(@{
        registryType = 'mcpb'
        identifier = "https://github.com/EvotecIT/DesktopManager/releases/download/desktopmanager-windows-verification-v$($manifest.version)/desktopmanager-mcp-win-x64.mcpb"
        fileSha256 = (Get-FileHash -LiteralPath $PackagePath -Algorithm SHA256).Hash.ToLowerInvariant()
        transport = @{ type = 'stdio' }
    })
}
$destination = [IO.Path]::GetFullPath($OutputPath)
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
[IO.File]::WriteAllText($destination, ($server | ConvertTo-Json -Depth 8) + "`n", [Text.UTF8Encoding]::new($false))
$destination
