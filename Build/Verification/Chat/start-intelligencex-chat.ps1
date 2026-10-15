[CmdletBinding()]
param(
    [string] $IntelligenceXRepoRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-IntelligenceXRepoRoot {
    param(
        [string] $ExplicitPath,
        [Parameter(Mandatory = $true)]
        [string] $ScriptPath
    )

    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath)) {
        return [System.IO.Path]::GetFullPath($ExplicitPath)
    }

    if (-not [string]::IsNullOrWhiteSpace($env:INTELLIGENCEX_REPO_ROOT)) {
        return [System.IO.Path]::GetFullPath($env:INTELLIGENCEX_REPO_ROOT)
    }

    $desktopManagerRoot = [System.IO.Path]::GetFullPath((Join-Path $ScriptPath '..\..\..'))
    $githubRoot = [System.IO.Path]::GetFullPath((Join-Path $desktopManagerRoot '..'))
    [System.IO.Path]::GetFullPath((Join-Path $githubRoot 'IntelligenceX'))
}

$resolvedRepoRoot = Resolve-IntelligenceXRepoRoot -ExplicitPath $IntelligenceXRepoRoot -ScriptPath $PSScriptRoot
$runScriptPath = Join-Path $resolvedRepoRoot 'Build\Chat\Run-ChatApp.ps1'

if (-not (Test-Path -LiteralPath $runScriptPath)) {
    throw "IntelligenceX run script was not found at '$runScriptPath'. Pass -IntelligenceXRepoRoot or set INTELLIGENCEX_REPO_ROOT."
}

& pwsh -NoLogo -NoProfile -File $runScriptPath
exit $LASTEXITCODE
