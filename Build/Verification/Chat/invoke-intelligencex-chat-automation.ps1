[CmdletBinding()]
param(
    [ValidateSet('ping', 'status', 'new_conversation', 'send_prompt', 'wait_for_idle', 'get_transcript_tail', 'list_conversations', 'switch_conversation')]
    [string] $Command = 'status',
    [string] $IntelligenceXRepoRoot,
    [string] $Text,
    [string] $ConversationId,
    [int] $Count = 8,
    [int] $TimeoutMs = 120000,
    [int] $IntervalMs = 500,
    [switch] $LaunchIfNeeded,
    [string] $OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-DesktopManagerRepoRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ScriptPath
    )

    [System.IO.Path]::GetFullPath((Join-Path $ScriptPath '..\..\..'))
}

function Resolve-IntelligenceXRepoRoot {
    param(
        [string] $ExplicitPath,
        [Parameter(Mandatory = $true)]
        [string] $DesktopManagerRepoRoot
    )

    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath)) {
        return [System.IO.Path]::GetFullPath($ExplicitPath)
    }

    if (-not [string]::IsNullOrWhiteSpace($env:INTELLIGENCEX_REPO_ROOT)) {
        return [System.IO.Path]::GetFullPath($env:INTELLIGENCEX_REPO_ROOT)
    }

    $githubRoot = [System.IO.Path]::GetFullPath((Join-Path $DesktopManagerRepoRoot '..'))
    [System.IO.Path]::GetFullPath((Join-Path $githubRoot 'IntelligenceX'))
}

function Resolve-ChatAppPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $RepoRoot
    )

    $candidates = @(
        (Join-Path $RepoRoot 'IntelligenceX.Chat\IntelligenceX.Chat.App\bin\Release\net10.0-windows10.0.26100.0\win-x64\IntelligenceX.Chat.App.exe'),
        (Join-Path $RepoRoot 'IntelligenceX.Chat\IntelligenceX.Chat.App\bin\Release\net8.0-windows10.0.26100.0\win-x64\IntelligenceX.Chat.App.exe')
    )

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    throw "A built IntelligenceX.Chat.App.exe was not found under '$RepoRoot'. Build the chat app first."
}

function Resolve-IntelligenceXAutomationPipeName {
    param(
        [Parameter(Mandatory = $true)]
        [int] $ProcessId
    )

    'intelligencex.chat.app.automation.{0}' -f $ProcessId
}

function Invoke-IntelligenceXAutomationJson {
    param(
        [Parameter(Mandatory = $true)]
        [string] $PipeName,
        [Parameter(Mandatory = $true)]
        [hashtable] $Request,
        [int] $ConnectTimeoutMs = 5000
    )

    $reader = $null
    $writer = $null
    $pipe = New-Object System.IO.Pipes.NamedPipeClientStream('.', $PipeName, [System.IO.Pipes.PipeDirection]::InOut, [System.IO.Pipes.PipeOptions]::None)
    try {
        $pipe.Connect($ConnectTimeoutMs)
        $writer = New-Object System.IO.StreamWriter($pipe)
        $writer.AutoFlush = $true
        $writer.NewLine = "`n"
        $reader = New-Object System.IO.StreamReader($pipe)

        $requestJson = $Request | ConvertTo-Json -Depth 20 -Compress
        $writer.WriteLine($requestJson)
        $responseLine = $reader.ReadLine()
        if ([string]::IsNullOrWhiteSpace($responseLine)) {
            return $null
        }

        $convertFromJsonSupportsDepth = $null -ne (Get-Command ConvertFrom-Json).Parameters['Depth']
        if ($convertFromJsonSupportsDepth) {
            return ($responseLine | ConvertFrom-Json -Depth 100)
        }

        return ($responseLine | ConvertFrom-Json)
    } finally {
        if ($null -ne $reader) {
            try {
                $reader.Dispose()
            } catch {
            }
        }
        if ($null -ne $writer) {
            try {
                $writer.Dispose()
            } catch {
            }
        }
        try {
            $pipe.Dispose()
        } catch {
        }
    }
}

function Try-Invoke-IntelligenceXAutomationJson {
    param(
        [Parameter(Mandatory = $true)]
        [string] $PipeName,
        [Parameter(Mandatory = $true)]
        [hashtable] $Request,
        [int] $ConnectTimeoutMs = 1500
    )

    try {
        $response = Invoke-IntelligenceXAutomationJson -PipeName $PipeName -Request $Request -ConnectTimeoutMs $ConnectTimeoutMs
        return [pscustomobject]@{
            Success = $true
            Response = $response
        }
    } catch {
        return [pscustomobject]@{
            Success = $false
            Error = $_.Exception.Message
        }
    }
}

function Find-IntelligenceXAutomationProcess {
    $processes = Get-Process -Name 'IntelligenceX.Chat.App' -ErrorAction SilentlyContinue |
        Sort-Object StartTime -Descending

    foreach ($process in $processes) {
        $pipeName = Resolve-IntelligenceXAutomationPipeName -ProcessId $process.Id
        $probe = Try-Invoke-IntelligenceXAutomationJson -PipeName $pipeName -Request @{ command = 'ping' }
        if ($probe.Success -and $null -ne $probe.Response -and $probe.Response.Ok) {
            return [pscustomobject]@{
                ProcessId = $process.Id
                PipeName = $pipeName
                Probe = $probe.Response
                Launched = $false
                Path = $process.Path
            }
        }
    }

    return $null
}

function Start-IntelligenceXAutomationProcess {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ChatAppPath
    )

    $previousAutomationMode = $env:IXCHAT_AUTOMATION_MODE
    try {
        $env:IXCHAT_AUTOMATION_MODE = '1'
        $process = Start-Process -FilePath $ChatAppPath -WorkingDirectory (Split-Path -Parent $ChatAppPath) -PassThru
    } finally {
        if ($null -eq $previousAutomationMode) {
            Remove-Item Env:IXCHAT_AUTOMATION_MODE -ErrorAction SilentlyContinue
        } else {
            $env:IXCHAT_AUTOMATION_MODE = $previousAutomationMode
        }
    }

    $deadlineUtc = (Get-Date).ToUniversalTime().AddSeconds(30)
    while ((Get-Date).ToUniversalTime() -lt $deadlineUtc) {
        Start-Sleep -Milliseconds 500
        $pipeName = Resolve-IntelligenceXAutomationPipeName -ProcessId $process.Id
        $probe = Try-Invoke-IntelligenceXAutomationJson -PipeName $pipeName -Request @{ command = 'ping' }
        if ($probe.Success -and $null -ne $probe.Response -and $probe.Response.Ok) {
            return [pscustomobject]@{
                ProcessId = $process.Id
                PipeName = $pipeName
                Probe = $probe.Response
                Launched = $true
                Path = $ChatAppPath
            }
        }
    }

    throw "IntelligenceX.Chat automation mode did not expose a reachable pipe within 30 seconds."
}

function Build-IntelligenceXAutomationRequest {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ResolvedCommand
    )

    $request = @{
        command = $ResolvedCommand
    }

    if (-not [string]::IsNullOrWhiteSpace($ConversationId)) {
        $request.conversationId = $ConversationId
    }

    switch ($ResolvedCommand) {
        'send_prompt' {
            if ([string]::IsNullOrWhiteSpace($Text)) {
                throw "The send_prompt command requires -Text."
            }
            $request.text = $Text
        }
        'wait_for_idle' {
            $request.timeoutMs = $TimeoutMs
            $request.intervalMs = $IntervalMs
        }
        'get_transcript_tail' {
            $request.count = $Count
        }
        'new_conversation' {
            if (-not [string]::IsNullOrWhiteSpace($ConversationId)) {
                $request.conversationId = $ConversationId
            }
        }
    }

    return $request
}

$desktopManagerRepoRoot = Resolve-DesktopManagerRepoRoot -ScriptPath $PSScriptRoot
$resolvedIntelligenceXRepoRoot = Resolve-IntelligenceXRepoRoot -ExplicitPath $IntelligenceXRepoRoot -DesktopManagerRepoRoot $desktopManagerRepoRoot
$chatAppPath = Resolve-ChatAppPath -RepoRoot $resolvedIntelligenceXRepoRoot

$automationProcess = Find-IntelligenceXAutomationProcess
if ($null -eq $automationProcess) {
    if (-not $LaunchIfNeeded) {
        throw "No reachable IntelligenceX.Chat automation pipe was found. Start the app in automation mode or rerun with -LaunchIfNeeded."
    }

    $automationProcess = Start-IntelligenceXAutomationProcess -ChatAppPath $chatAppPath
}

$request = Build-IntelligenceXAutomationRequest -ResolvedCommand $Command
$connectTimeoutMs = if ($Command -eq 'wait_for_idle') { [Math]::Max($TimeoutMs + 5000, 10000) } else { 120000 }
$response = Invoke-IntelligenceXAutomationJson -PipeName $automationProcess.PipeName -Request $request -ConnectTimeoutMs $connectTimeoutMs

$result = [pscustomobject]@{
    RepoRoot = $resolvedIntelligenceXRepoRoot
    ChatAppPath = $chatAppPath
    ProcessId = $automationProcess.ProcessId
    PipeName = $automationProcess.PipeName
    Launched = $automationProcess.Launched
    Command = $Command
    Request = $request
    Response = $response
}

if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    $resolvedOutputPath = [System.IO.Path]::GetFullPath($OutputPath)
    $outputDirectory = Split-Path -Parent $resolvedOutputPath
    if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
        New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
    }
    $result | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $resolvedOutputPath -Encoding UTF8
}

$result | ConvertTo-Json -Depth 100
