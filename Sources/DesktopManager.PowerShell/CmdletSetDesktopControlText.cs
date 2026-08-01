using System.Management.Automation;
using System.Runtime.Versioning;

namespace DesktopManager.PowerShell;

/// <summary>Safely edits text on a specific window control or prior semantic observation.</summary>
/// <para type="synopsis">Uses provider-safe setters first and explicitly gated foreground input for selection or caret edits.</para>
/// <example>
///   <code>Set-DesktopControlText -Control $ctrl -Text "Hello world"</code>
/// </example>
[Cmdlet(VerbsCommon.Set, "DesktopControlText", SupportsShouldProcess = true)]
[SupportedOSPlatform("windows")]
public sealed class CmdletSetDesktopControlText : PSCmdlet {
    /// <summary>
    /// <para type="description">Control to update.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByControl")]
    public WindowControlInfo Control { get; set; } = null!;

    /// <summary>
    /// <para type="description">Prior generic observation identifying the live control. Its complete-text fingerprint is used as the default concurrency precondition.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByObservation")]
    public DesktopControlObservation Observation { get; set; } = null!;

    /// <summary>
    /// <para type="description">Text to apply to the control.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 1)]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Replace the complete document, replace the current selection, or insert at the current caret.</para>
    /// </summary>
    [Parameter]
    public DesktopTextEditMode Mode { get; set; } = DesktopTextEditMode.ReplaceDocument;

    /// <summary>
    /// <para type="description">Optional complete-content fingerprint that must still match before the edit is applied.</para>
    /// </summary>
    [Parameter]
    public string ExpectedFingerprint { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Optional selection/caret context fingerprint that must still match before a range edit.</para>
    /// </summary>
    [Parameter]
    public string ExpectedEditContextFingerprint { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Bring the parent window to the foreground before UI Automation text fallback.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter EnsureForeground { get; set; }

    /// <summary>
    /// <para type="description">Explicitly allow focused foreground input fallback for zero-handle UI Automation controls.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter AllowForegroundInput { get; set; }

    /// <summary>
    /// <para type="description">Return the structured verification result. Verification is enabled by default.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Verify { get; set; }

    /// <summary>
    /// <para type="description">Skip the default exact post-edit text verification.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter NoVerify { get; set; }

    /// <summary>
    /// <para type="description">Return a structured mutation result object for the targeted control.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter PassThru { get; set; }

    /// <inheritdoc />
    protected override void BeginProcessing() {
        string target = ParameterSetName == "ByObservation"
            ? Observation.Identity.SessionKey
            : Control.Text ?? Control.ClassName;
        if (ShouldProcess(target, $"{Mode} text")) {
            var automation = new DesktopAutomationService();
            var request = new DesktopTextEditRequest {
                Text = Text,
                Mode = Mode,
                ExpectedFingerprint = !string.IsNullOrWhiteSpace(ExpectedFingerprint)
                    ? ExpectedFingerprint
                    : ParameterSetName == "ByObservation" && Observation.Text.IsComplete
                        ? Observation.Text.ContentFingerprint
                        : null,
                ExpectedEditContextFingerprint = !string.IsNullOrWhiteSpace(ExpectedEditContextFingerprint)
                    ? ExpectedEditContextFingerprint
                    : ParameterSetName == "ByObservation"
                        ? Observation.Text.EditContextFingerprint
                        : null,
                EnsureForegroundWindow = EnsureForeground,
                AllowForegroundInputFallback = AllowForegroundInput,
                VerifyAfterEdit = !NoVerify.IsPresent
            };
            DesktopTextEditResult result = ParameterSetName == "ByObservation"
                ? automation.EditControlText(Observation, request)
                : automation.EditControlText(Control, request);
            if (!result.Success) {
                if (!Verify.IsPresent && !PassThru.IsPresent) {
                    throw new InvalidOperationException(result.FailureReason);
                }

                WriteWarning($"Failed to edit text on control '{target}': {result.FailureReason}");
            }

            if (Verify.IsPresent || PassThru.IsPresent) {
                WriteObject(result);
            }
        }
    }
}
