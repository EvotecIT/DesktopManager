namespace DesktopManager.PowerShell;

/// <summary>Restores a named workstation profile.</summary>
[Cmdlet(VerbsData.Restore, "DesktopWorkstationProfile", SupportsShouldProcess = true)]
[OutputType(typeof(WorkstationProfileApplyResult))]
public sealed class CmdletRestoreDesktopWorkstationProfile : PSCmdlet {
    /// <summary><para type="description">The stored profile name.</para></summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string Name;

    /// <summary><para type="description">Allows saved monitors to be absent.</para></summary>
    [Parameter]
    public SwitchParameter AllowMissingMonitor;

    /// <summary><para type="description">Skips display settings.</para></summary>
    [Parameter]
    public SwitchParameter SkipDisplay;

    /// <summary><para type="description">Skips audio settings.</para></summary>
    [Parameter]
    public SwitchParameter SkipAudio;

    /// <summary><para type="description">Skips personalization settings.</para></summary>
    [Parameter]
    public SwitchParameter SkipPersonalization;

    /// <summary><para type="description">Also restores machine-wide lock-screen and Spotlight policies.</para></summary>
    [Parameter]
    public SwitchParameter IncludeMachinePolicies;

    /// <summary><para type="description">Skips taskbar settings.</para></summary>
    [Parameter]
    public SwitchParameter SkipTaskbar;

    /// <summary><para type="description">Disables automatic rollback after failure.</para></summary>
    [Parameter]
    public SwitchParameter NoRollback;

    /// <summary>Restores the selected profile sections.</summary>
    protected override void BeginProcessing() {
        if (!ShouldProcess(Name, "Restore workstation profile")) {
            return;
        }

        WorkstationProfileApplyResult result = new WorkstationProfileService().ApplyProfile(Name, new WorkstationProfileApplyOptions {
            RequireAllMonitors = !AllowMissingMonitor,
            ApplyDisplays = !SkipDisplay,
            ApplyAudio = !SkipAudio,
            ApplyPersonalization = !SkipPersonalization,
            ApplyMachinePolicies = IncludeMachinePolicies,
            ApplyTaskbars = !SkipTaskbar,
            RollbackOnFailure = !NoRollback
        });
        if (!result.Succeeded) {
            ThrowTerminatingError(CreateApplyFailureError(Name, result));
        }

        WriteObject(result);
    }

    internal static ErrorRecord CreateApplyFailureError(string name, WorkstationProfileApplyResult result) {
        var exception = new InvalidOperationException(CreateApplyFailureMessage(name, result));
        return new ErrorRecord(
            exception,
            "WorkstationProfileApplyFailed",
            ErrorCategory.OperationStopped,
            name);
    }

    internal static string CreateApplyFailureMessage(string name, WorkstationProfileApplyResult result) {
        var details = new List<string> {
            string.IsNullOrWhiteSpace(result.Error) ? "The operation failed without an error message." : result.Error
        };
        if (result.RolledBack) {
            details.Add("Previous desktop state was restored.");
        }
        if (result.Warnings.Count > 0) {
            details.Add($"Warnings: {string.Join(" ", result.Warnings)}");
        }

        return $"Workstation profile '{name}' could not be restored. {string.Join(" ", details)}";
    }
}
