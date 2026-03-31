using System.Management.Automation;
using System.Runtime.Versioning;

namespace DesktopManager.PowerShell;

/// <summary>Sets text in a desktop window.</summary>
/// <para type="synopsis">Pastes or types text into a desktop window.</para>
/// <example>
///   <code>Set-DesktopWindowText -Name "Notepad" -Text "Hello"</code>
/// </example>
/// <example>
///   <code>Set-DesktopWindowText -Name "Notepad" -Text "Hello" -Type</code>
/// </example>
[Cmdlet(VerbsCommon.Set, "DesktopWindowText", DefaultParameterSetName = "Paste", SupportsShouldProcess = true)]
[SupportedOSPlatform("windows")]
public sealed class CmdletSetDesktopWindowText : PSCmdlet {
    /// <summary>
    /// <para type="description">Window title to match. Supports wildcards.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string Name { get; set; }

    /// <summary>
    /// <para type="description">Text to paste or type.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 1)]
    public string Text { get; set; }

    /// <summary>
    /// <para type="description">Use the clipboard paste method.</para>
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "Paste")]
    public SwitchParameter Paste { get; set; }

    /// <summary>
    /// <para type="description">Simulate typing the text.</para>
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "Type")]
    public SwitchParameter Type { get; set; }

    /// <summary>
    /// <para type="description">Delay in milliseconds between characters when typing.</para>
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "Type")]
    public int Delay { get; set; } = 0;

    /// <summary>
    /// <para type="description">Use WM_CHAR messages instead of SendInput when typing.</para>
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "Type")]
    public SwitchParameter UseMessage { get; set; }

    /// <summary>
    /// <para type="description">Require real foreground keyboard input and fail instead of falling back to background message typing.</para>
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "Type")]
    public SwitchParameter ForegroundInput { get; set; }

    /// <summary>
    /// <para type="description">Prefer layout-aware physical key presses over Unicode packets when typing in the foreground.</para>
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "Type")]
    public SwitchParameter PhysicalKeys { get; set; }

    /// <summary>
    /// <para type="description">Use a hosted-session typing profile with a fixed US-style foreground scancode path and slower pacing defaults. The target surface must already own focus, and typing stops if focus drifts.</para>
    /// <para type="description">When the repo-owned hosted-session harness is exercised, related diagnostics are written under Artifacts\HostedSessionTyping as a raw JSON snapshot plus a companion summary file.</para>
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "Type")]
    public SwitchParameter HostedSession { get; set; }

    /// <summary>
    /// <para type="description">Preserve multiline formatting and chunk long lines into smaller typed segments.</para>
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "Type")]
    public SwitchParameter Script { get; set; }

    /// <summary>
    /// <para type="description">Maximum number of characters to send in each script chunk.</para>
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "Type")]
    public int ScriptChunkSize { get; set; } = 120;

    /// <summary>
    /// <para type="description">Delay in milliseconds after each scripted line break.</para>
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "Type")]
    public int ScriptLineDelayMilliseconds { get; set; }

    /// <summary>
    /// <para type="description">Number of clipboard open retries.</para>
    /// </summary>
    [Parameter]
    public int ClipboardRetryCount { get; set; } = 5;

    /// <summary>
    /// <para type="description">Delay between clipboard retries in milliseconds.</para>
    /// </summary>
    [Parameter]
    public int ClipboardRetryDelayMilliseconds { get; set; } = 50;

    /// <summary>
    /// <para type="description">Number of activation retries.</para>
    /// </summary>
    [Parameter]
    public int ActivationRetryCount { get; set; } = 3;

    /// <summary>
    /// <para type="description">Delay between activation retries in milliseconds.</para>
    /// </summary>
    [Parameter]
    public int ActivationRetryDelayMilliseconds { get; set; } = 100;

    /// <summary>
    /// <para type="description">Number of input retries.</para>
    /// </summary>
    [Parameter]
    public int InputRetryCount { get; set; } = 2;

    /// <summary>
    /// <para type="description">Do not activate the window before sending input.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter NoActivate { get; set; }

    /// <summary>
    /// <para type="description">Restore focus to the previously active window.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter RestoreFocus { get; set; }

    /// <summary>
    /// <para type="description">Preserve and restore clipboard text.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter PreserveClipboard { get; set; }

    /// <summary>
    /// <para type="description">Enable safe mode (no activation, preserve clipboard).</para>
    /// </summary>
    [Parameter]
    public SwitchParameter SafeMode { get; set; }

    /// <summary>
    /// <para type="description">Re-query the target window after the mutation and report the observed postcondition.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Verify { get; set; }

    /// <summary>
    /// <para type="description">Return a structured mutation result object for each matching window.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter PassThru { get; set; }

    /// <inheritdoc />
    protected override void BeginProcessing() {
        var automation = new DesktopAutomationService();
        IReadOnlyList<WindowInfo> windows = automation.GetWindows(new WindowQueryOptions {
            TitlePattern = Name,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        });

        foreach (var window in windows) {
            string action = ParameterSetName == "Type" ? "Type text" : "Paste text";
            if (ShouldProcess(window.Title, action)) {
                var options = new WindowInputOptions {
                    ActivateWindow = !NoActivate,
                    RestoreFocus = RestoreFocus,
                    PreserveClipboard = PreserveClipboard,
                    ClipboardRetryCount = ClipboardRetryCount,
                    ClipboardRetryDelayMilliseconds = ClipboardRetryDelayMilliseconds,
                    ActivationRetryCount = ActivationRetryCount,
                    ActivationRetryDelayMilliseconds = ActivationRetryDelayMilliseconds,
                    InputRetryCount = InputRetryCount,
                    KeyDelayMilliseconds = Delay > 0 ? Delay : HostedSession ? 35 : 0,
                    UseSendInput = !UseMessage,
                    RequireForegroundWindowForTyping = ForegroundInput || PhysicalKeys || HostedSession,
                    UsePhysicalKeyboardLayout = PhysicalKeys,
                    UseHostedSessionScanCodes = HostedSession,
                    TypeTextAsScript = Script,
                    ScriptChunkLength = ScriptChunkSize,
                    ScriptLineDelayMilliseconds = ScriptLineDelayMilliseconds > 0 ? ScriptLineDelayMilliseconds : HostedSession && Script ? 120 : 0
                };

                if (SafeMode) {
                    options.ActivateWindow = false;
                    options.RestoreFocus = false;
                    options.PreserveClipboard = true;
                }

                if (ParameterSetName == "Type" && !options.ActivateWindow && options.UseSendInput && !options.RequireForegroundWindowForTyping && !options.UsePhysicalKeyboardLayout) {
                    options.UseSendInput = false;
                    WriteVerbose("NoActivate is set; using WM_CHAR to avoid typing into the foreground window.");
                }

                DesktopWindowMutationRecord result = null;
                if (ParameterSetName == "Type") {
                    try {
                        automation.TypeWindowText(window.Handle, Text, options);
                        if (Verify.IsPresent || PassThru.IsPresent) {
                            result = DesktopWindowMutationVerifier.Verify(
                                automation,
                                options.UseHostedSessionScanCodes ? "type-text-hosted-session" : options.UsePhysicalKeyboardLayout ? "type-text-physical-keys" : options.RequireForegroundWindowForTyping ? "type-text-foreground" : "type-text",
                                window,
                                tolerancePixels: 10,
                                requireForeground: options.RequireForegroundWindowForTyping && options.ActivateWindow);
                        }
                    } catch (Exception ex) {
                        if (!Verify.IsPresent && !PassThru.IsPresent) {
                            throw;
                        }

                        WriteWarning($"Failed to type text into '{window.Title}': {ex.Message}");
                        if (Verify.IsPresent || PassThru.IsPresent) {
                            result = DesktopWindowMutationVerifier.CreateFailureRecord("type-text", window, ex.Message, Verify.IsPresent, 10);
                        }
                    }
                } else {
                    try {
                        automation.PasteWindowText(window.Handle, Text, options);
                        if (Verify.IsPresent || PassThru.IsPresent) {
                            result = DesktopWindowMutationVerifier.Verify(
                                automation,
                                "paste-text",
                                window,
                                tolerancePixels: 10,
                                requireForeground: options.ActivateWindow);
                        }
                    } catch (Exception ex) {
                        if (!Verify.IsPresent && !PassThru.IsPresent) {
                            throw;
                        }

                        WriteWarning($"Failed to paste text into '{window.Title}': {ex.Message}");
                        if (Verify.IsPresent || PassThru.IsPresent) {
                            result = DesktopWindowMutationVerifier.CreateFailureRecord("paste-text", window, ex.Message, Verify.IsPresent, 10);
                        }
                    }
                }

                if (result != null) {
                    WriteObject(result);
                }
            }
        }
    }
}

