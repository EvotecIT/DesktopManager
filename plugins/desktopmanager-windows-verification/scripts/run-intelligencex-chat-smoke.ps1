[CmdletBinding()]
param(
    [string] $IntelligenceXRepoRoot,
    [string] $ArtifactRoot,
    [switch] $NoLaunch,
    [switch] $AutomationMode,
    [switch] $ProbeAutomationContract,
    [switch] $ProbeForegroundTyping,
    [string] $ProbeText = 'Reply with only the single word PONG.'
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

function Resolve-DesktopManagerCliPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $DesktopManagerRepoRoot
    )

    $candidates = @(
        (Join-Path $DesktopManagerRepoRoot 'Sources\DesktopManager.Cli\bin\Debug\net10.0-windows10.0.19041.0\DesktopManager.Cli.exe'),
        (Join-Path $DesktopManagerRepoRoot 'Sources\DesktopManager.Cli\bin\Debug\net8.0-windows10.0.19041.0\DesktopManager.Cli.exe'),
        (Join-Path $DesktopManagerRepoRoot 'Sources\DesktopManager.Cli\bin\Debug\net10.0-windows\DesktopManager.Cli.exe'),
        (Join-Path $DesktopManagerRepoRoot 'Sources\DesktopManager.Cli\bin\Debug\net8.0-windows\DesktopManager.Cli.exe')
    ) |
        Where-Object { Test-Path -LiteralPath $_ } |
        ForEach-Object { Get-Item -LiteralPath $_ } |
        Sort-Object LastWriteTimeUtc -Descending

    if ($candidates.Count -gt 0) {
        return $candidates[0].FullName
    }

    throw "DesktopManager.Cli.exe was not found under '$DesktopManagerRepoRoot'. Build DesktopManager.Cli first."
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

function Invoke-DesktopManagerJson {
    param(
        [Parameter(Mandatory = $true)]
        [string] $CliPath,
        [Parameter(Mandatory = $true)]
        [string[]] $Arguments
    )

    $output = & $CliPath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "DesktopManager command failed: $($Arguments -join ' ')"
    }

    if ([string]::IsNullOrWhiteSpace(($output | Out-String))) {
        return $null
    }

    $convertFromJsonSupportsDepth = $null -ne (Get-Command ConvertFrom-Json).Parameters['Depth']
    if ($convertFromJsonSupportsDepth) {
        return ($output | ConvertFrom-Json -Depth 100)
    }

    return ($output | ConvertFrom-Json)
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
                # Ignore cleanup failures on already-closed pipe handles.
            }
        }
        if ($null -ne $writer) {
            try {
                $writer.Dispose()
            } catch {
                # Ignore cleanup failures on already-closed pipe handles.
            }
        }
        try {
            $pipe.Dispose()
        } catch {
            # Ignore cleanup failures on already-closed pipe handles.
        }
    }
}

function Save-Json {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,
        [Parameter(Mandatory = $false)]
        $Value
    )

    if ($null -eq $Value) {
        'null' | Set-Content -LiteralPath $Path -Encoding UTF8
        return
    }

    $Value | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $Path -Encoding UTF8
}

function Test-ObjectProperty {
    param(
        [Parameter(Mandatory = $false)]
        $InputObject,
        [Parameter(Mandatory = $true)]
        [string] $Name
    )

    return $null -ne $InputObject -and $null -ne $InputObject.PSObject.Properties[$Name]
}

$desktopManagerRepoRoot = Resolve-DesktopManagerRepoRoot -ScriptPath $PSScriptRoot
$desktopManagerCli = Resolve-DesktopManagerCliPath -DesktopManagerRepoRoot $desktopManagerRepoRoot
$resolvedIntelligenceXRepoRoot = Resolve-IntelligenceXRepoRoot -ExplicitPath $IntelligenceXRepoRoot -DesktopManagerRepoRoot $desktopManagerRepoRoot
$chatAppPath = Resolve-ChatAppPath -RepoRoot $resolvedIntelligenceXRepoRoot

if ([string]::IsNullOrWhiteSpace($ArtifactRoot)) {
    $timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $ArtifactRoot = Join-Path $desktopManagerRepoRoot "Artifacts\IntelligenceXRealApp\$timestamp"
}

$resolvedArtifactRoot = [System.IO.Path]::GetFullPath($ArtifactRoot)
New-Item -ItemType Directory -Force -Path $resolvedArtifactRoot | Out-Null

$launchResult = $null
if (-not $NoLaunch) {
    $runningProcess = Get-Process -Name 'IntelligenceX.Chat.App' -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $runningProcess) {
        $previousAutomationMode = $env:IXCHAT_AUTOMATION_MODE
        try {
            if ($AutomationMode) {
                $env:IXCHAT_AUTOMATION_MODE = '1'
            }

            $launched = Start-Process -FilePath $chatAppPath -WorkingDirectory (Split-Path -Parent $chatAppPath) -PassThru
        } finally {
            if ($null -eq $previousAutomationMode) {
                Remove-Item Env:IXCHAT_AUTOMATION_MODE -ErrorAction SilentlyContinue
            } else {
                $env:IXCHAT_AUTOMATION_MODE = $previousAutomationMode
            }
        }
        Start-Sleep -Seconds 3
        $launchResult = [pscustomobject]@{
            ProcessId = $launched.Id
            Path = $chatAppPath
            Launched = $true
            AutomationMode = $AutomationMode.IsPresent
        }
    } else {
        $launchResult = [pscustomobject]@{
            ProcessId = $runningProcess.Id
            Path = $runningProcess.Path
            Launched = $false
            AutomationMode = $false
        }
    }
}

$automationPipeName = $null
$automationStatusBefore = $null
$automationConversationListBefore = $null
$automationNewConversationResult = $null
$automationSendPromptResult = $null
$automationWaitForIdleResult = $null
$automationStatusAfter = $null
$automationConversationListAfter = $null
$automationTranscriptTail = $null
$automationContractError = $null

$waitResult = Invoke-DesktopManagerJson -CliPath $desktopManagerCli -Arguments @(
    'window', 'wait',
    '--process', 'IntelligenceX.Chat.App',
    '--include-empty',
    '--timeout-ms', '30000',
    '--interval-ms', '500',
    '--json'
)
Save-Json -Path (Join-Path $resolvedArtifactRoot 'window-wait.json') -Value $waitResult

$windowList = Invoke-DesktopManagerJson -CliPath $desktopManagerCli -Arguments @(
    'window', 'list',
    '--process', 'IntelligenceX.Chat.App',
    '--include-empty',
    '--json'
)
Save-Json -Path (Join-Path $resolvedArtifactRoot 'window-list.json') -Value $windowList

if ($AutomationMode) {
    $automationProcessId = $null
    if ($null -ne $launchResult -and $null -ne $launchResult.ProcessId) {
        $automationProcessId = [int] $launchResult.ProcessId
    } elseif ($null -ne $waitResult -and @($waitResult.Windows).Count -gt 0) {
        $automationProcessId = [int] @($waitResult.Windows)[0].ProcessId
    } elseif (@($windowList).Count -gt 0) {
        $automationProcessId = [int] @($windowList)[0].ProcessId
    }

    if ($null -ne $automationProcessId) {
        $automationPipeName = Resolve-IntelligenceXAutomationPipeName -ProcessId $automationProcessId
        try {
            $automationStatusBefore = Invoke-IntelligenceXAutomationJson -PipeName $automationPipeName -Request @{
                command = 'status'
            }
            Save-Json -Path (Join-Path $resolvedArtifactRoot 'automation-status-before.json') -Value $automationStatusBefore

            $automationConversationListBefore = Invoke-IntelligenceXAutomationJson -PipeName $automationPipeName -Request @{
                command = 'list_conversations'
            }
            Save-Json -Path (Join-Path $resolvedArtifactRoot 'automation-conversations-before.json') -Value $automationConversationListBefore

            if ($ProbeAutomationContract) {
                $automationNewConversationResult = Invoke-IntelligenceXAutomationJson -PipeName $automationPipeName -Request @{
                    command = 'new_conversation'
                }
                Save-Json -Path (Join-Path $resolvedArtifactRoot 'automation-new-conversation.json') -Value $automationNewConversationResult

                $automationSendPromptResult = Invoke-IntelligenceXAutomationJson -PipeName $automationPipeName -Request @{
                    command = 'send_prompt'
                    text = $ProbeText
                } -ConnectTimeoutMs 120000
                Save-Json -Path (Join-Path $resolvedArtifactRoot 'automation-send-prompt.json') -Value $automationSendPromptResult

                $automationWaitForIdleResult = Invoke-IntelligenceXAutomationJson -PipeName $automationPipeName -Request @{
                    command = 'wait_for_idle'
                    timeoutMs = 120000
                    intervalMs = 500
                } -ConnectTimeoutMs 125000
                Save-Json -Path (Join-Path $resolvedArtifactRoot 'automation-wait-for-idle.json') -Value $automationWaitForIdleResult

                $automationStatusAfter = Invoke-IntelligenceXAutomationJson -PipeName $automationPipeName -Request @{
                    command = 'status'
                }
                Save-Json -Path (Join-Path $resolvedArtifactRoot 'automation-status-after.json') -Value $automationStatusAfter

                $automationConversationListAfter = Invoke-IntelligenceXAutomationJson -PipeName $automationPipeName -Request @{
                    command = 'list_conversations'
                }
                Save-Json -Path (Join-Path $resolvedArtifactRoot 'automation-conversations-after.json') -Value $automationConversationListAfter

                $automationTranscriptTail = Invoke-IntelligenceXAutomationJson -PipeName $automationPipeName -Request @{
                    command = 'get_transcript_tail'
                    count = 6
                }
                Save-Json -Path (Join-Path $resolvedArtifactRoot 'automation-transcript-tail.json') -Value $automationTranscriptTail
            }
        } catch {
            $automationContractError = $_.Exception.Message
        }
    }
}

$windowScreenshot = Invoke-DesktopManagerJson -CliPath $desktopManagerCli -Arguments @(
    'screenshot', 'window',
    '--process', 'IntelligenceX.Chat.App',
    '--output', (Join-Path $resolvedArtifactRoot 'chat-window.png'),
    '--json'
)
Save-Json -Path (Join-Path $resolvedArtifactRoot 'window-screenshot.json') -Value $windowScreenshot

$controlList = Invoke-DesktopManagerJson -CliPath $desktopManagerCli -Arguments @(
    'control', 'list',
    '--window-process', 'IntelligenceX.Chat.App',
    '--uia',
    '--include-uia',
    '--all',
    '--json'
)
Save-Json -Path (Join-Path $resolvedArtifactRoot 'control-list.json') -Value $controlList

$controlDiagnostics = Invoke-DesktopManagerJson -CliPath $desktopManagerCli -Arguments @(
    'control', 'diagnose',
    '--window-process', 'IntelligenceX.Chat.App',
    '--uia',
    '--ensure-foreground',
    '--sample-limit', '25',
    '--action-probe',
    '--json'
)
Save-Json -Path (Join-Path $resolvedArtifactRoot 'control-diagnose.json') -Value $controlDiagnostics

$editProbe = Invoke-DesktopManagerJson -CliPath $desktopManagerCli -Arguments @(
    'control', 'list',
    '--window-process', 'IntelligenceX.Chat.App',
    '--uia',
    '--ensure-foreground',
    '--control-type', 'Edit',
    '--all',
    '--json'
)
Save-Json -Path (Join-Path $resolvedArtifactRoot 'control-probe-edit.json') -Value $editProbe

$promptAutomationProbe = Invoke-DesktopManagerJson -CliPath $desktopManagerCli -Arguments @(
    'control', 'list',
    '--window-process', 'IntelligenceX.Chat.App',
    '--uia',
    '--ensure-foreground',
    '--automation-id', 'prompt',
    '--all',
    '--json'
)
Save-Json -Path (Join-Path $resolvedArtifactRoot 'control-probe-automation-id-prompt.json') -Value $promptAutomationProbe

$promptTextProbe = Invoke-DesktopManagerJson -CliPath $desktopManagerCli -Arguments @(
    'control', 'list',
    '--window-process', 'IntelligenceX.Chat.App',
    '--uia',
    '--ensure-foreground',
    '--text-pattern', 'Ask IntelligenceX',
    '--all',
    '--json'
)
Save-Json -Path (Join-Path $resolvedArtifactRoot 'control-probe-placeholder.json') -Value $promptTextProbe

$typingProbeResult = $null
if ($ProbeForegroundTyping) {
    $null = Invoke-DesktopManagerJson -CliPath $desktopManagerCli -Arguments @(
        'window', 'click',
        '--process', 'IntelligenceX.Chat.App',
        '--x-ratio', '0.50',
        '--y-ratio', '0.97',
        '--client-area',
        '--activate',
        '--json'
    )

    $typingProbeResult = Invoke-DesktopManagerJson -CliPath $desktopManagerCli -Arguments @(
        'window', 'type',
        '--process', 'IntelligenceX.Chat.App',
        '--text', $ProbeText,
        '--foreground-input',
        '--capture-after',
        '--artifact-directory', $resolvedArtifactRoot,
        '--verify',
        '--json'
    )
    Save-Json -Path (Join-Path $resolvedArtifactRoot 'foreground-typing-probe.json') -Value $typingProbeResult
}

$promptSurfaceStructurallyActionable = @($editProbe).Count -gt 0 -or @($promptAutomationProbe).Count -gt 0 -or @($promptTextProbe).Count -gt 0
$summaryNotes = [System.Collections.Generic.List[string]]::new()
if (-not $promptSurfaceStructurallyActionable) {
    $summaryNotes.Add('DesktopManager did not resolve the visible chat composer through UIA selectors such as Edit, automation-id=prompt, or placeholder text.')
}
if ($ProbeForegroundTyping -and -not $promptSurfaceStructurallyActionable) {
    $summaryNotes.Add('Foreground typing verification is not structural for this app yet; use the saved after-screenshot as the visual source of truth.')
}
if ($AutomationMode -and -not [string]::IsNullOrWhiteSpace($automationPipeName) -and $null -ne $automationStatusBefore) {
    $summaryNotes.Add('Automation mode exposed a non-visual control pipe, so prompt/send/status checks can be validated without relying on UIA discovery.')
}
if ($null -ne $automationNewConversationResult -and $automationNewConversationResult.Ok) {
    $summaryNotes.Add('Automation contract created an isolated conversation before sending the smoke prompt, which keeps probe evidence away from the previously active chat state.')
}
if (-not [string]::IsNullOrWhiteSpace($automationContractError)) {
    $summaryNotes.Add('Automation contract probe failed: ' + $automationContractError)
}

$automationPromptObserved = $null
$automationAssistantMessage = $null
$automationStatusText = $null
$automationIdleReached = $null
$automationActiveConversationId = $null
$automationActiveConversationTitle = $null
$automationConversationCount = $null
$automationTranscriptTailCount = $null
$automationTranscriptTailLastRole = $null
$automationTranscriptTailLastText = $null
if ($null -ne $automationSendPromptResult) {
    $lastAutomationUserMessage = ''
    if ($null -ne $automationSendPromptResult.AfterSnapshot -and $null -ne $automationSendPromptResult.AfterSnapshot.LastUserMessage) {
        $lastAutomationUserMessage = [string] $automationSendPromptResult.AfterSnapshot.LastUserMessage
    } elseif ($null -ne $automationStatusAfter -and $null -ne $automationStatusAfter.Snapshot -and $null -ne $automationStatusAfter.Snapshot.LastUserMessage) {
        $lastAutomationUserMessage = [string] $automationStatusAfter.Snapshot.LastUserMessage
    }
    $automationPromptObserved = $lastAutomationUserMessage -eq $ProbeText
    if ($null -ne $automationSendPromptResult.AfterSnapshot -and $null -ne $automationSendPromptResult.AfterSnapshot.LastAssistantMessage) {
        $automationAssistantMessage = $automationSendPromptResult.AfterSnapshot.LastAssistantMessage
    } elseif ($null -ne $automationStatusAfter -and $null -ne $automationStatusAfter.Snapshot -and $null -ne $automationStatusAfter.Snapshot.LastAssistantMessage) {
        $automationAssistantMessage = $automationStatusAfter.Snapshot.LastAssistantMessage
    }
}
if ($null -ne $automationStatusAfter -and $null -ne $automationStatusAfter.Snapshot -and $null -ne $automationStatusAfter.Snapshot.StatusText) {
    $automationStatusText = $automationStatusAfter.Snapshot.StatusText
} elseif ($null -ne $automationStatusBefore -and $null -ne $automationStatusBefore.Snapshot -and $null -ne $automationStatusBefore.Snapshot.StatusText) {
    $automationStatusText = $automationStatusBefore.Snapshot.StatusText
}
if ($null -ne $automationWaitForIdleResult) {
    $automationIdleReached = $automationWaitForIdleResult.Ok
}
if ($null -ne $automationStatusAfter -and $null -ne $automationStatusAfter.Snapshot) {
    $automationActiveConversationId = $automationStatusAfter.Snapshot.ActiveConversationId
    $automationActiveConversationTitle = $automationStatusAfter.Snapshot.ActiveConversationTitle
} elseif ($null -ne $automationStatusBefore -and $null -ne $automationStatusBefore.Snapshot) {
    $automationActiveConversationId = $automationStatusBefore.Snapshot.ActiveConversationId
    $automationActiveConversationTitle = $automationStatusBefore.Snapshot.ActiveConversationTitle
}
if ((Test-ObjectProperty -InputObject $automationConversationListAfter -Name 'Conversations') -and $null -ne $automationConversationListAfter.Conversations) {
    $automationConversationCount = @($automationConversationListAfter.Conversations).Count
} elseif ((Test-ObjectProperty -InputObject $automationConversationListBefore -Name 'Conversations') -and $null -ne $automationConversationListBefore.Conversations) {
    $automationConversationCount = @($automationConversationListBefore.Conversations).Count
}
if ((Test-ObjectProperty -InputObject $automationTranscriptTail -Name 'Transcript') -and $null -ne $automationTranscriptTail.Transcript) {
    $automationTranscriptTailCount = @($automationTranscriptTail.Transcript).Count
    if ($automationTranscriptTailCount -gt 0) {
        $lastTranscriptItem = @($automationTranscriptTail.Transcript)[-1]
        $automationTranscriptTailLastRole = $lastTranscriptItem.Role
        $automationTranscriptTailLastText = $lastTranscriptItem.Text
    }
}

$summary = [pscustomobject]@{
    ArtifactRoot = $resolvedArtifactRoot
    DesktopManagerCli = $desktopManagerCli
    IntelligenceXRepoRoot = $resolvedIntelligenceXRepoRoot
    AutomationMode = $AutomationMode.IsPresent
    AutomationPipeName = $automationPipeName
    AutomationPipeReachable = $null -ne $automationStatusBefore
    AutomationContractProbeAttempted = $ProbeAutomationContract.IsPresent
    AutomationContractError = $automationContractError
    AutomationPromptAccepted = if ($null -eq $automationSendPromptResult) { $null } else { $automationSendPromptResult.Ok }
    AutomationIdleReached = $automationIdleReached
    AutomationPromptObserved = $automationPromptObserved
    AutomationAssistantMessage = $automationAssistantMessage
    AutomationStatusText = $automationStatusText
    AutomationActiveConversationId = $automationActiveConversationId
    AutomationActiveConversationTitle = $automationActiveConversationTitle
    AutomationConversationCount = $automationConversationCount
    AutomationTranscriptTailCount = $automationTranscriptTailCount
    AutomationTranscriptTailLastRole = $automationTranscriptTailLastRole
    AutomationTranscriptTailLastText = $automationTranscriptTailLastText
    WindowCount = @($waitResult.Windows).Count
    WindowTitle = if (@($waitResult.Windows).Count -gt 0) { @($waitResult.Windows)[0].Title } else { $null }
    WindowHandle = if (@($waitResult.Windows).Count -gt 0) { @($waitResult.Windows)[0].Handle } else { $null }
    ControlCount = @($controlList).Count
    DiagnosedEffectiveControlCount = if (@($controlDiagnostics).Count -gt 0) { @($controlDiagnostics)[0].EffectiveControlCount } else { $null }
    DiagnosedMatchedControlCount = if (@($controlDiagnostics).Count -gt 0) { @($controlDiagnostics)[0].MatchedControlCount } else { $null }
    EditProbeCount = @($editProbe).Count
    PromptAutomationIdProbeCount = @($promptAutomationProbe).Count
    PromptPlaceholderProbeCount = @($promptTextProbe).Count
    PromptSurfaceStructurallyActionable = $promptSurfaceStructurallyActionable
    TypingProbeVerified = if ($null -eq $typingProbeResult) { $null } else { $typingProbeResult.Verification.Verified }
    TypingProbeVerificationMode = if ($null -eq $typingProbeResult) { $null } else { $typingProbeResult.Verification.Mode }
    Notes = $summaryNotes
}

Save-Json -Path (Join-Path $resolvedArtifactRoot 'summary.json') -Value $summary
$summary | ConvertTo-Json -Depth 20
