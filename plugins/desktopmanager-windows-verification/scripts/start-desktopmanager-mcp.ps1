[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-PluginRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ScriptPath
    )

    [System.IO.Path]::GetFullPath((Join-Path $ScriptPath '..'))
}

function Resolve-RepoRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string] $PluginRoot
    )

    if (-not [string]::IsNullOrWhiteSpace($env:DESKTOPMANAGER_REPO_ROOT)) {
        return [System.IO.Path]::GetFullPath($env:DESKTOPMANAGER_REPO_ROOT)
    }

    [System.IO.Path]::GetFullPath((Join-Path $PluginRoot '..\..'))
}

function Add-StringIfPresent {
    param(
        [Parameter(Mandatory = $true)]
        [System.Collections.Generic.List[string]] $List,
        [string] $Value
    )

    if (-not [string]::IsNullOrWhiteSpace($Value)) {
        $List.Add($Value)
    }
}

function Add-RepeatedPatterns {
    param(
        [Parameter(Mandatory = $true)]
        [System.Collections.Generic.List[string]] $Arguments,
        [Parameter(Mandatory = $true)]
        [string] $SwitchName,
        [string] $RawValue
    )

    if ([string]::IsNullOrWhiteSpace($RawValue)) {
        return
    }

    foreach ($pattern in ($RawValue -split '[,;]')) {
        $trimmedPattern = $pattern.Trim()
        if (-not [string]::IsNullOrWhiteSpace($trimmedPattern)) {
            $Arguments.Add($SwitchName)
            $Arguments.Add($trimmedPattern)
        }
    }
}

function Find-DesktopManagerCliHost {
    param(
        [Parameter(Mandatory = $true)]
        [string] $RepoRoot
    )

    $candidates = [System.Collections.Generic.List[object]]::new()

    if (-not [string]::IsNullOrWhiteSpace($env:DESKTOPMANAGER_MCP_EXE) -and (Test-Path -LiteralPath $env:DESKTOPMANAGER_MCP_EXE)) {
        $candidates.Add([pscustomobject]@{
                Kind = 'exe'
                Path = [System.IO.Path]::GetFullPath($env:DESKTOPMANAGER_MCP_EXE)
            })
    }

    if (-not [string]::IsNullOrWhiteSpace($env:DESKTOPMANAGER_MCP_DLL) -and (Test-Path -LiteralPath $env:DESKTOPMANAGER_MCP_DLL)) {
        $candidates.Add([pscustomobject]@{
                Kind = 'dll'
                Path = [System.IO.Path]::GetFullPath($env:DESKTOPMANAGER_MCP_DLL)
            })
    }

    foreach ($relativePath in @(
            'Sources\DesktopManager.Cli\bin\Debug\net10.0-windows10.0.19041.0\DesktopManager.Cli.exe',
            'Sources\DesktopManager.Cli\bin\Debug\net8.0-windows10.0.19041.0\DesktopManager.Cli.exe',
            'Sources\DesktopManager.Cli\bin\Release\net10.0-windows10.0.19041.0\DesktopManager.Cli.exe',
            'Sources\DesktopManager.Cli\bin\Release\net8.0-windows10.0.19041.0\DesktopManager.Cli.exe',
            'Sources\DesktopManager.Cli\bin\Debug\net10.0-windows10.0.19041.0\DesktopManager.Cli.dll',
            'Sources\DesktopManager.Cli\bin\Debug\net8.0-windows10.0.19041.0\DesktopManager.Cli.dll',
            'Sources\DesktopManager.Cli\bin\Release\net10.0-windows10.0.19041.0\DesktopManager.Cli.dll',
            'Sources\DesktopManager.Cli\bin\Release\net8.0-windows10.0.19041.0\DesktopManager.Cli.dll',
            'Sources\DesktopManager.Cli\bin\Debug\net10.0-windows\DesktopManager.Cli.exe',
            'Sources\DesktopManager.Cli\bin\Debug\net8.0-windows\DesktopManager.Cli.exe',
            'Sources\DesktopManager.Cli\bin\Release\net10.0-windows\DesktopManager.Cli.exe',
            'Sources\DesktopManager.Cli\bin\Release\net8.0-windows\DesktopManager.Cli.exe',
            'Sources\DesktopManager.Cli\bin\Debug\net10.0-windows\DesktopManager.Cli.dll',
            'Sources\DesktopManager.Cli\bin\Debug\net8.0-windows\DesktopManager.Cli.dll',
            'Sources\DesktopManager.Cli\bin\Release\net10.0-windows\DesktopManager.Cli.dll',
            'Sources\DesktopManager.Cli\bin\Release\net8.0-windows\DesktopManager.Cli.dll'
        )) {
        $candidatePath = Join-Path $RepoRoot $relativePath
        if (Test-Path -LiteralPath $candidatePath) {
            $candidates.Add([pscustomobject]@{
                    Kind = [System.IO.Path]::GetExtension($candidatePath).TrimStart('.').ToLowerInvariant()
                    Path = [System.IO.Path]::GetFullPath($candidatePath)
                })
        }
    }

    $publishRoot = Join-Path $RepoRoot 'Artefacts\PowerForge\DesktopManager'
    if (Test-Path -LiteralPath $publishRoot) {
        $publishedHost = Get-ChildItem -LiteralPath $publishRoot -Recurse -File -ErrorAction SilentlyContinue |
            Where-Object {
                $_.Name -in @('desktopmanager.exe', 'DesktopManager.Cli.exe', 'DesktopManager.Cli.dll')
            } |
            Sort-Object LastWriteTimeUtc -Descending |
            Select-Object -First 1

        if ($null -ne $publishedHost) {
            $candidates.Add([pscustomobject]@{
                    Kind = [System.IO.Path]::GetExtension($publishedHost.FullName).TrimStart('.').ToLowerInvariant()
                    Path = $publishedHost.FullName
                })
        }
    }

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate.Path) {
            return $candidate
        }
    }

    return $null
}

$pluginRoot = Resolve-PluginRoot -ScriptPath $PSScriptRoot
$repoRoot = Resolve-RepoRoot -PluginRoot $pluginRoot
$resolvedHost = Find-DesktopManagerCliHost -RepoRoot $repoRoot

if ($null -eq $resolvedHost) {
    [Console]::Error.WriteLine("DesktopManager MCP bootstrap could not find a built CLI host. Build DesktopManager.Cli first or set DESKTOPMANAGER_MCP_EXE / DESKTOPMANAGER_MCP_DLL.")
    exit 1
}

$mcpArguments = [System.Collections.Generic.List[string]]::new()
$mcpArguments.Add('mcp')
$mcpArguments.Add('serve')

if ($env:DESKTOPMANAGER_MCP_ALLOW_MUTATIONS -in @('1', 'true', 'TRUE', 'yes', 'YES')) {
    $mcpArguments.Add('--allow-mutations')
}

if ($env:DESKTOPMANAGER_MCP_DRY_RUN -in @('1', 'true', 'TRUE', 'yes', 'YES')) {
    $mcpArguments.Add('--dry-run')
}

if ($env:DESKTOPMANAGER_MCP_ALLOW_FOREGROUND_INPUT -in @('1', 'true', 'TRUE', 'yes', 'YES')) {
    $mcpArguments.Add('--allow-foreground-input')
}

Add-RepeatedPatterns -Arguments $mcpArguments -SwitchName '--allow-process' -RawValue $env:DESKTOPMANAGER_MCP_ALLOW_PROCESS
Add-RepeatedPatterns -Arguments $mcpArguments -SwitchName '--deny-process' -RawValue $env:DESKTOPMANAGER_MCP_DENY_PROCESS

if ($env:DESKTOPMANAGER_MCP_DIAGNOSTIC -in @('1', 'true', 'TRUE', 'yes', 'YES')) {
    $diagnostic = [ordered]@{
        pluginRoot = $pluginRoot
        repoRoot = $repoRoot
        host = $resolvedHost
        arguments = @($mcpArguments)
    }

    $diagnostic | ConvertTo-Json -Depth 6
    exit 0
}

$argumentArray = @($mcpArguments)

if ($resolvedHost.Kind -eq 'exe') {
    & $resolvedHost.Path @argumentArray
    exit $LASTEXITCODE
}

if ($resolvedHost.Kind -eq 'dll') {
    & dotnet exec $resolvedHost.Path @argumentArray
    exit $LASTEXITCODE
}

[Console]::Error.WriteLine("DesktopManager MCP bootstrap found an unsupported host kind '$($resolvedHost.Kind)' at '$($resolvedHost.Path)'.")
exit 1
